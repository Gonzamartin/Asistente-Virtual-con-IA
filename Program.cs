using System;
using System.Windows;

namespace LectorIAPDF
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Inicializa la aplicación de Windows
            Application app = new Application();
            
            // Instancia y muestra nuestra ventana hecha en C# puro
            MainWindow ventana = new MainWindow();
            
            // Arranca el bucle de la interfaz gráfica
            app.Run(ventana);
        }
    }
}
