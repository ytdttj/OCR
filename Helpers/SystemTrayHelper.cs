using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Windows;

namespace OCR.Helpers
{
    /// <summary>
    /// 系统托盘辅助类，用于管理系统托盘图标
    /// </summary>
    public class SystemTrayHelper : IDisposable
    {
        // 系统托盘图标
        private TaskbarIcon _taskbarIcon;
        
        // 主窗口引用
        private Window _mainWindow;

        /// <summary>
        /// 初始化系统托盘辅助类
        /// </summary>
        /// <param name="mainWindow">主窗口</param>
        /// <param name="iconPath">图标路径</param>
        /// <param name="tooltip">工具提示文本</param>
        public SystemTrayHelper(Window mainWindow, string iconPath, string tooltip)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            // 初始化系统托盘图标
            _taskbarIcon = new TaskbarIcon
            {
                IconSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                ToolTipText = tooltip
            };

            // 添加双击事件处理
            _taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick;
        }

        /// <summary>
        /// 托盘图标双击事件处理
        /// </summary>
        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        public void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        }

        /// <summary>
        /// 隐藏主窗口（最小化到托盘）
        /// </summary>
        public void HideMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Hide();
            }
        }

        /// <summary>
        /// 设置上下文菜单
        /// </summary>
        /// <param name="contextMenu">要设置的上下文菜单</param>
        public void SetContextMenu(System.Windows.Controls.ContextMenu contextMenu)
        {
            if (_taskbarIcon != null)
            {
                _taskbarIcon.ContextMenu = contextMenu;
            }
        }

        /// <summary>
        /// 显示通知气泡
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="icon">图标类型</param>
        public void ShowNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            if (_taskbarIcon != null)
            {
                _taskbarIcon.ShowBalloonTip(title, message, icon);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_taskbarIcon != null)
            {
                _taskbarIcon.TrayMouseDoubleClick -= TaskbarIcon_TrayMouseDoubleClick;
                _taskbarIcon.Dispose();
                _taskbarIcon = null;
            }
        }
    }
} 