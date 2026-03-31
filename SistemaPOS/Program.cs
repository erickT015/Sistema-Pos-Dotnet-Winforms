using SistemaPOS.Forms;
using System;
using System.Windows.Forms;

namespace SistemaPOS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles(); // Añade esto por seguridad
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}