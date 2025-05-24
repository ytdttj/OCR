using System.Configuration;
using System.Data;
using System.Windows;
using OCR.Models;
using System;

namespace OCR
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Load application settings if necessary (already here)
                var settings = AppSettings.Load();

                // Create and show the main window
                MainWindow mainWindow = new MainWindow();
                this.MainWindow = mainWindow; // Set it as the main window
                mainWindow.Show(); // Explicitly show it

                // The logic for StartMinimized is handled in MainWindow's Loaded event,
                // so no need to duplicate it here.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1); // Exit if startup fails
            }
        }
    }
}