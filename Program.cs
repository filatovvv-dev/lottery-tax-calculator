using System;
using System.Windows.Forms;

namespace LotteryTax;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();   // для .NET Framework: Application.EnableVisualStyles();
        Application.Run(new MainForm());          //                         Application.SetCompatibleTextRenderingDefault(false);
    }
}