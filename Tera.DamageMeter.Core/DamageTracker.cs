// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tera.Game;
using Tera.DamageMeter;
using Tera.Game.Messages;

namespace Tera.DamageMeter
{
    public class DamageTracker : IEnumerable<PlayerInfo>
    {
        public bool OnlyBosses { get; set; }
        public bool IgnoreOneshots { get; set; }
        readonly Dictionary<Player, PlayerInfo> _statsByUser = new Dictionary<Player, PlayerInfo>();
        public DateTime? FirstAttack { get; private set; }
        public DateTime? LastAttack { get; private set; }
        public TimeSpan? Duration { get { return LastAttack - FirstAttack; } }

        public SkillStats TotalDealt { get; private set; }
        public SkillStats TotalReceived { get; private set; }

        public void UpdateTotal()
        {

        }

        public DamageTracker(bool onlyboss,bool ignoreoneshots)
        {
            TotalDealt = new SkillStats();
            TotalReceived = new SkillStats();
            OnlyBosses = onlyboss;
            IgnoreOneshots = ignoreoneshots;
        }

        private PlayerInfo GetOrCreate(Player player)
        {
            PlayerInfo playerStats;
            if (!_statsByUser.TryGetValue(player, out playerStats))
            {
                playerStats = new PlayerInfo(player, this);
                _statsByUser.Add(player, playerStats);
            }

            return playerStats;
        }

        public void Update(SkillResult skillResult)
        {
            if (skillResult.SourcePlayer != null)
            {
                var playerStats = GetOrCreate(skillResult.SourcePlayer);
                var statsChange = StatsChange(skillResult);
                playerStats.Dealt.Add(statsChange);
                TotalDealt.Add(statsChange);
            }

            if (skillResult.TargetPlayer != null)
            {
                var playerStats = GetOrCreate(skillResult.TargetPlayer);
                var statsChange = StatsChange(skillResult);
                playerStats.Received.Add(statsChange);
                TotalReceived.Add(statsChange);
            }

            if (skillResult.SourcePlayer != null && (skillResult.Damage > 0) && (skillResult.Source.Id != skillResult.Target.Id))
            {
                LastAttack = skillResult.Time;

                if (FirstAttack == null)
                    FirstAttack = skillResult.Time;
            }
        }

        private SkillStats StatsChange(SkillResult message)
        {
            var result = new SkillStats();
            if (message.Amount == 0)
                return result;

            /// Fix counting self-damage, such as Death from above & Command: Self-destruct 
            if ((message.Source.RootOwner == message.Target.RootOwner) && (message.Damage > 0))
                return result;

            NpcEntity npctarget = message.Target as NpcEntity;
            if (npctarget != null) { 
                if (OnlyBosses)       /// not count bosses
                    if (!npctarget.Info.Boss)
                        return result;
                if (IgnoreOneshots)    /// ignore damage that is more than 10x times than mob's hp
                    if ((npctarget.Info.HP > 0) && (npctarget.Info.HP <= message.Damage/10))
                        return result;
            }

            result.Damage = message.Damage;
            result.Heal = message.Heal;
            result.Hits++;
            if (message.IsCritical)
                result.Crits++;

            return result;
        }

        public IEnumerator<PlayerInfo> GetEnumerator()
        {
            return _statsByUser.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public long? Dps(long damage)
        {
            return Dps(damage, Duration);
        }

        public static long? Dps(long damage, TimeSpan? duration)
        {
            var durationInSeconds = (duration ?? TimeSpan.Zero).TotalSeconds;
            if (durationInSeconds < 1)
                durationInSeconds = 1;
            var dps = damage / durationInSeconds;
            if (Math.Abs(dps) > long.MaxValue)
                return null;
            return (long)dps;
        }
    }
}
