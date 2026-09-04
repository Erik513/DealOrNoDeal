using System;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI;
using ErikwnkWFUI.Forms;
using MessageBox = ErikwnkWFUI.Forms.MessageBox;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace DealOrNoDeal
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Black/yellow, not ErikwnkWFUI's default blue - has to happen
            // before any control is built, since a few of them only read
            // their accent color once at construction time.
            UIStyles.Colors.SetAccent(UIStyles.Colors.YellowLighter);

            GameStrings.Register();
            GameSettings.Load();
            GameHistory.Load();

            // Named per-app so it can't collide with any other app's
            // single-instance mutex on the same machine.
            using (var guard = new SingleInstanceGuard("DealOrNoDeal_SingleInstance"))
            {
                if (!guard.IsFirstInstance)
                {
                    MessageBox.Show(
                        AppLocalization.Get("App.AlreadyRunning"),
                        "Deal or No Deal",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Info);

                    return;
                }

                Application.Run(new DealOrNoDeal());
            }
        }
    }
}
