using System.Configuration;
using System.Data;
using System.Windows;
using OCR.Models;
using OCR.Services;
using System;

namespace OCR
{
    public partial class App : Application
    {
        private SilentRestartService _silentRestartService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 初始化静默重启服务
                _silentRestartService = new SilentRestartService();
                
                // 清理过期的临时文件
                _silentRestartService.CleanupExpiredFiles();

                // 检查是否为静默重启
                bool isSilentRestart = _silentRestartService.IsSilentRestart();
                
                Console.WriteLine($"应用程序启动: 静默重启={isSilentRestart}");

                // Load application settings if necessary (already here)
                var settings = AppSettings.Load();

                // Create and show the main window
                MainWindow mainWindow = new MainWindow(_silentRestartService, isSilentRestart);
                this.MainWindow = mainWindow; // Set it as the main window

                // 如果是静默重启，尝试恢复窗口状态
                if (isSilentRestart)
                {
                    Console.WriteLine("尝试恢复窗口状态");
                    bool stateRestored = _silentRestartService.RestoreWindowState(mainWindow);
                    
                    if (!stateRestored)
                    {
                        Console.WriteLine("窗口状态恢复失败，使用默认状态");
                        mainWindow.Show(); // 使用默认状态显示
                    }
                    
                    // 通知静默重启完成
                    _silentRestartService.NotifyRestartComplete();
                    Console.WriteLine("静默重启流程完成");
                }
                else
                {
                    // 正常启动，显示窗口
                    mainWindow.Show();
                }

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

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 清理临时文件
                _silentRestartService?.CleanupTempFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用程序退出时清理失败: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}