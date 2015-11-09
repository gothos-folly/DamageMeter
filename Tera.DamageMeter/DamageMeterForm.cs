﻿// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tera.Data;
using Tera.Game;
using Tera.Game.Messages;
using Tera.PacketLog;
using Tera.Sniffing;

namespace Tera.DamageMeter
{
    public partial class DamageMeterForm : Form
    {
        private TeraSniffer _teraSniffer;
        private static readonly BasicTeraData _basicTeraData = new BasicTeraData();
        private ClassIcons _classIcons;
        private static TeraData _teraData;
        private readonly Dictionary<PlayerInfo, PlayerStatsControl> _controls = new Dictionary<PlayerInfo, PlayerStatsControl>();
        private MessageFactory _messageFactory;
        private EntityTracker _entityTracker;
        private DamageTracker _damageTracker;
        private Server _server;
        private PlayerTracker _playerTracker;
        private HotKeyManager _hotKeyManager;
        private Settings _settings;
        private GlobalHotKey _pasteStatsHotKey;
        private GlobalHotKey _resetHotKey;

        public DamageMeterForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Logger.Log("Form created");
            _settings = Settings.Load();
            Logger.Log("Settings loaded");
            _classIcons = new ClassIcons(_basicTeraData.ResourceDirectory + @"class-icons\", 36);

            _hotKeyManager = new HotKeyManager();
            _pasteStatsHotKey = new GlobalHotKey(_hotKeyManager);
            _pasteStatsHotKey.Pressed += PasteStatsMenuItem_Click;
            _resetHotKey = new GlobalHotKey(_hotKeyManager);
            _resetHotKey.Pressed += ResetButton_Click;

            _teraSniffer = new TeraSniffer(_basicTeraData.Servers);
            _teraSniffer.MessageReceived += message => InvokeAction(() => HandleMessageReceived(message));
            _teraSniffer.NewConnection += server => InvokeAction(() => HandleNewConnection(server));
            _teraSniffer.Warning += LogWarning;
            _teraSniffer.BufferSize = _settings.BufferSize;
            Logger.Log(string.Format("Setting buffer size to {0}B", FormatHelpers.Invariant.FormatValue(_settings.BufferSize) ?? "Default"));
            SettingsChanged();

            StartSniffing();
        }

        void LogWarning(string message)
        {
            Logger.Log(message);
        }

        private void StartSniffing()
        {
            if (_teraSniffer.Enabled)
                return;
            Logger.Log("Starting sniffing...");
            _teraSniffer.Enabled = true;
            foreach (string line in _teraSniffer.SnifferStatus())
            {
                Logger.Log(line);
            }
            Logger.Log("Sniffing started");
        }

        private void InvokeAction(Action action)
        {
            if (IsDisposed)
                return;
            if (!InvokeRequired)
                throw new InvalidOperationException("Expected InvokeRequired");
            Invoke(action);
        }


        public void Fetch()
        {
            UpdateStatus();
            if (_damageTracker != null)
            {
                Fetch(_damageTracker);
            }
        }

        public void Fetch(IEnumerable<PlayerInfo> playerStatsSequence)
        {
            var formatHelpers = FormatHelpers.Pretty;
            playerStatsSequence = playerStatsSequence.OrderByDescending(playerStats => playerStats.Dealt.Damage + playerStats.Dealt.Heal);

            TotalDamageLabel.Text = string.Format("Total damage: {0}", formatHelpers.FormatValue(_damageTracker.TotalDealt.Damage));
            TotalTimeLabel.Text = string.Format("Total time: {0}", formatHelpers.FormatTimeSpan(_damageTracker.Duration) ?? "-");
            TotalDpsLabel.Text = string.Format("Total DPS: {0}/s", formatHelpers.FormatValue(_damageTracker.Dps(_damageTracker.TotalDealt.Damage)) ?? "-");

            TotalDpsLabel.Left = FooterPanel.Width - TotalDpsLabel.Width;
            TotalTimeLabel.Left = FooterPanel.Width - TotalTimeLabel.Width;

            int pos = 0;
            var visiblePlayerStats = new HashSet<PlayerInfo>();
            foreach (var playerStats in playerStatsSequence)
            {
                if (pos > ListPanel.Height)
                    break;

                visiblePlayerStats.Add(playerStats);
                PlayerStatsControl playerStatsControl;
                _controls.TryGetValue(playerStats, out playerStatsControl);
                if (playerStatsControl == null)
                {
                    playerStatsControl = new PlayerStatsControl();
                    playerStatsControl.PlayerInfo = playerStats;
                    playerStatsControl.Height = 50;
                    _controls.Add(playerStats, playerStatsControl);
                    playerStatsControl.Parent = ListPanel;
                    playerStatsControl.ClassIcons = _classIcons;
                }
                playerStatsControl.Top = pos;
                playerStatsControl.Width = ListPanel.Width;
                pos += playerStatsControl.Height + 2;
                playerStatsControl.Invalidate();
            }

            var invisibleControls = _controls.Where(x => !visiblePlayerStats.Contains(x.Key)).ToList();
            foreach (var invisibleControl in invisibleControls)
            {
                invisibleControl.Value.Dispose();
                _controls.Remove(invisibleControl.Key);
            }
        }

        public void SettingsChanged()
        {
            if (_settings.AlwaysOnTop != TopMost)
                TopMost = _settings.AlwaysOnTop;
            Opacity = _settings.Opacity;
            alwaysOnTopToolStripMenuItem.Checked = _settings.AlwaysOnTop;

            _pasteStatsHotKey.Key = HotKeyHelpers.FromString(_settings.HotKeys.PasteStats);
            _resetHotKey.Key = HotKeyHelpers.FromString(_settings.HotKeys.Reset);

            Fetch();
            _settings.Save();
        }

        public void UpdateStatus()
        {
            CaptureMenuItem.Checked = _teraSniffer.Enabled;
            PasteStatsMenuItem.Enabled = TeraWindow.IsTeraRunning();

            SetHotKeyEnabled(TeraWindow.IsTeraActive());
        }

        public void SetHotKeyEnabled(bool enabled)
        {
            foreach (var hotkey in _hotKeyManager.Hotkeys)
            {
                hotkey.Enabled = enabled;
            }
        }

        private void HandleNewConnection(Server server)
        {
            Text = string.Format("Damage Meter connected to {0}", server.Name);
            _server = server;
            _teraData = _basicTeraData.DataForRegion(server.Region);
            _entityTracker = new EntityTracker();
            _playerTracker = new PlayerTracker(_entityTracker);
            _damageTracker = new DamageTracker();
            _messageFactory = new MessageFactory(_teraData.OpCodeNamer);

            Logger.Log(Text);
        }

        private void HandleMessageReceived(Message obj)
        {
            var message = _messageFactory.Create(obj);
            _entityTracker.Update(message);

            var skillResultMessage = message as EachSkillResultServerMessage;
            if (skillResultMessage != null)
            {
                var skillResult = new SkillResult(skillResultMessage, _entityTracker, _playerTracker, _teraData.SkillDatabase);
                _damageTracker.Update(skillResult);
            }
        }

        private void DamageMeterForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _hotKeyManager.Dispose();
            _teraSniffer.Enabled = false;
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            if (_server == null)
                return;
            _damageTracker = new DamageTracker();
        }

        private void RefershTimer_Tick(object sender, EventArgs e)
        {
            Fetch();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            MainMenu.Show(MenuButton, 0, MenuButton.Height);
        }

        private void OpenPacketLogMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenPacketLogFileDialog.ShowDialog() != DialogResult.OK)
                return;

            _teraSniffer.Enabled = false;

            var log = new PacketLogFile(OpenPacketLogFileDialog.FileName);

            var server = new Server(string.Format("[{0}] Packet Log", log.Header.Region), log.Header.Region ?? "EU", null);
            HandleNewConnection(server);

            foreach (var message in log.Messages)
            {
                HandleMessageReceived(message);
            }
        }

        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
            SettingsChanged();
        }

        private void CaptureMenuItem_Click(object sender, EventArgs e)
        {
            _teraSniffer.Enabled = !_teraSniffer.Enabled;
            Fetch();
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.Settings = _settings;
                settingsForm.ShowDialog(this);
            }
            SettingsChanged();
        }

        private void PasteStatsMenuItem_Click(object sender, EventArgs e)
        {
            if (_damageTracker == null)
                return;

            var playerStatsSequence = _damageTracker.OrderByDescending(playerStats => playerStats.Dealt.Damage).TakeWhile(x => x.Dealt.Damage > 0);
            const int maxLength = 300;

            var sb = new StringBuilder();
            bool first = true;

            foreach (var playerInfo in playerStatsSequence)
            {
                var placeHolder = DamagePlaceHolders.FromPlayerInfo(playerInfo, FormatHelpers.Invariant);
                var playerText = first ? "" : " | ";

                playerText += placeHolder.Replace("{Name} {Damage} {DamagePercent}");

                if (sb.Length + playerText.Length > maxLength)
                    break;

                sb.Append(playerText);
                first = false;
            }

            var text = sb.ToString();
            TeraWindow.SendString(text);
        }

        private void showLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Logger.LogFile);
        }
    }
}
