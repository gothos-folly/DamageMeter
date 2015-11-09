// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Tera.DamageMeter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Logger.Clear();
            Logger.Log("Starting");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DamageMeterForm());
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Logger.Log(e.Exception.ToString());

            {
                if (!SystemInformation.UserInteractive)
                    return;

                DialogResult dialogResult;
                using (var threadExceptionDialog = new ThreadExceptionDialog(e.Exception))
                {
                    dialogResult = threadExceptionDialog.ShowDialog();
                }
                switch (dialogResult)
                {
                    case DialogResult.Abort:
                        Application.Exit();
                        Environment.Exit(0);
                        break;
                    case DialogResult.Yes:
                        var warningException = e.Exception as WarningException;
                        if (warningException == null)
                            break;
                        Help.ShowHelp(null, warningException.HelpUrl, warningException.HelpTopic);
                        break;
                }
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log(e.ExceptionObject.ToString());
        }
    }
}
