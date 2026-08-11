using System;
using System.Windows.Forms;

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

            GameStrings.Register();
            GameSettings.Load();

            Application.Run(new DealOrNoDeal());
        }
    }
}
