using System;

namespace LectorIAPDF
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new System.Windows.Application();
            var mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }
}
