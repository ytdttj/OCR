using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OCR.Models;

namespace OCR.Services
{
    public class SilentRestartService
    {
        private readonly string _tempStateFile;
        private readonly string _restartLockFile;
        private readonly string _appPath;

        public SilentRestartService()
        {
            string tempDir = Path.GetTempPath();
            _tempStateFile = Path.Combine(tempDir, "ytocr_window_state.json");
            _restartLockFile = Path.Combine(tempDir, "ytocr_restart.lock");
            
            // 单文件发布兼容：使用AppContext.BaseDirectory获取应用程序路径
            // Assembly.Location在单文件发布中返回空字符串
            string? appLocation = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(appLocation))
            {
                // 单文件发布环境，使用进程路径
                _appPath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OCR.exe");
            }
            else
            {
                // 常规发布环境
                _appPath = appLocation;
            }
            
            Console.WriteLine($"SilentRestartService: 应用程序路径 = {_appPath}");
        }

        /// <summary>
        /// 检查是否为静默重启启动
        /// </summary>
        public bool IsSilentRestart()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.Equals("--silent-restart", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 保存当前窗口状态
        /// </summary>
        public void SaveWindowState(Window window)
        {
            try
            {
                var state = new Models.WindowState
                {
                    Left = window.Left,
                    Top = window.Top,
                    Width = window.Width,
                    Height = window.Height,
                    WinState = window.WindowState,
                    ShowInTaskbar = window.ShowInTaskbar,
                    IsVisible = window.IsVisible,
                    SavedAt = DateTime.Now
                };

                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_tempStateFile, json);
                
                Console.WriteLine($"窗口状态已保存到: {_tempStateFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存窗口状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复窗口状态
        /// </summary>
        public bool RestoreWindowState(Window window)
        {
            try
            {
                if (!File.Exists(_tempStateFile))
                {
                    Console.WriteLine("窗口状态文件不存在，使用默认状态");
                    return false;
                }

                string json = File.ReadAllText(_tempStateFile);
                var state = JsonSerializer.Deserialize<Models.WindowState>(json);

                if (state != null)
                {
                    // 检查状态文件是否过期（防止意外恢复旧状态）
                    if (DateTime.Now - state.SavedAt > TimeSpan.FromMinutes(5))
                    {
                        Console.WriteLine("窗口状态文件过期，使用默认状态");
                        CleanupTempFiles();
                        return false;
                    }

                    window.Left = state.Left;
                    window.Top = state.Top;
                    window.Width = state.Width;
                    window.Height = state.Height;
                    window.WindowState = state.WinState;
                    window.ShowInTaskbar = state.ShowInTaskbar;

                    // 可见性需要特殊处理
                    if (state.IsVisible)
                    {
                        window.Show();
                    }

                    Console.WriteLine("窗口状态已恢复");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"恢复窗口状态失败: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 执行静默重启
        /// </summary>
        public async Task<bool> PerformSilentRestart(Window window)
        {
            try
            {
                Console.WriteLine("开始静默重启流程");

                // 1. 检查是否已有重启进程在运行
                if (File.Exists(_restartLockFile))
                {
                    Console.WriteLine("检测到重启锁文件，跳过静默重启");
                    return false;
                }

                // 2. 创建重启锁文件
                File.WriteAllText(_restartLockFile, Process.GetCurrentProcess().Id.ToString());

                // 3. 保存当前窗口状态
                SaveWindowState(window);

                // 4. 启动新实例
                var startInfo = new ProcessStartInfo
                {
                    FileName = _appPath,
                    Arguments = "--silent-restart",
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var newProcess = Process.Start(startInfo);
                if (newProcess == null)
                {
                    Console.WriteLine("启动新实例失败");
                    CleanupTempFiles();
                    return false;
                }

                Console.WriteLine($"新实例已启动，PID: {newProcess.Id}");

                // 5. 等待新实例就绪（通过检查锁文件是否被删除）
                bool newInstanceReady = await WaitForNewInstanceReady();
                
                if (newInstanceReady)
                {
                    Console.WriteLine("新实例已就绪，准备退出当前实例");
                    return true;
                }
                else
                {
                    Console.WriteLine("新实例启动超时，取消静默重启");
                    CleanupTempFiles();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"静默重启失败: {ex.Message}");
                CleanupTempFiles();
                return false;
            }
        }

        /// <summary>
        /// 等待新实例就绪
        /// </summary>
        private async Task<bool> WaitForNewInstanceReady()
        {
            const int timeoutSeconds = 30;
            const int checkIntervalMs = 500;
            int totalWaitTime = 0;

            while (totalWaitTime < timeoutSeconds * 1000)
            {
                // 检查锁文件是否被新实例删除（表示新实例已完成初始化）
                if (!File.Exists(_restartLockFile))
                {
                    return true;
                }

                await Task.Delay(checkIntervalMs);
                totalWaitTime += checkIntervalMs;
            }

            return false;
        }

        /// <summary>
        /// 通知静默重启完成（由新实例调用）
        /// </summary>
        public void NotifyRestartComplete()
        {
            try
            {
                // 删除锁文件，通知旧实例可以退出
                if (File.Exists(_restartLockFile))
                {
                    File.Delete(_restartLockFile);
                    Console.WriteLine("静默重启完成通知已发送");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送重启完成通知失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        public void CleanupTempFiles()
        {
            try
            {
                if (File.Exists(_tempStateFile))
                {
                    File.Delete(_tempStateFile);
                }
                if (File.Exists(_restartLockFile))
                {
                    File.Delete(_restartLockFile);
                }
                Console.WriteLine("临时文件已清理");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理临时文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查并清理过期的临时文件
        /// </summary>
        public void CleanupExpiredFiles()
        {
            try
            {
                // 清理超过1小时的锁文件（避免意外情况导致的死锁）
                if (File.Exists(_restartLockFile))
                {
                    var lockFileTime = File.GetCreationTime(_restartLockFile);
                    if (DateTime.Now - lockFileTime > TimeSpan.FromHours(1))
                    {
                        File.Delete(_restartLockFile);
                        Console.WriteLine("已清理过期的重启锁文件");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理过期文件失败: {ex.Message}");
            }
        }
    }
} 