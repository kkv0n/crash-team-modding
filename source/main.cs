using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Crash_Team_Mod
{
    internal static class main
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CTM()); //run app gui class that includes all the code
        }
    }
}
