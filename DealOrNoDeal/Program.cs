using System;
using System.Threading;
using System.Windows.Forms;
using CustomWFUI;
using CustomWFUI.Forms;

namespace DealOrNoDeal
{
    static class Program
    {
        private static Mutex instanceMutex;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Black/yellow, not CustomWFUI's default blue - has to happen
            // before any control is built, since a few of them only read
            // their accent color once at construction time.
            UIStyles.Colors.SetAccent(UIStyles.Colors.YellowLighter);

            GameStrings.Register();
            GameSettings.Load();
            GameHistory.Load();

            // Named per-app so it can't collide with any other app's
            // single-instance mutex on the same machine.
            instanceMutex = new Mutex(true, "DealOrNoDeal_SingleInstance", out bool createdNew);

            if (!createdNew)
            {
                CustomMessageBox.Show(
                    AppLocalization.Get("App.AlreadyRunning"),
                    "Deal or No Deal",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Info);

                instanceMutex.Dispose();
                return;
            }

            try
            {
                Application.Run(new DealOrNoDeal());
            }
            finally
            {
                instanceMutex.ReleaseMutex();
                instanceMutex.Dispose();
            }
        }
    }
}
