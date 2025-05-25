using System;
using System.Windows;

namespace OCR.Models
{
    /// <summary>
    /// 窗口状态数据结构，用于静默重启时保持窗口状态
    /// </summary>
    public class WindowState
    {
        /// <summary>
        /// 窗口左侧位置
        /// </summary>
        public double Left { get; set; }

        /// <summary>
        /// 窗口顶部位置
        /// </summary>
        public double Top { get; set; }

        /// <summary>
        /// 窗口宽度
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 窗口高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 窗口状态（正常、最小化、最大化）
        /// </summary>
        public System.Windows.WindowState WinState { get; set; }

        /// <summary>
        /// 是否在任务栏显示
        /// </summary>
        public bool ShowInTaskbar { get; set; }

        /// <summary>
        /// 窗口是否可见
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// 状态保存时间（用于过期检测）
        /// </summary>
        public DateTime SavedAt { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WindowState()
        {
            // 设置默认值
            Left = 100;
            Top = 100;
            Width = 350;
            Height = 250;
            WinState = System.Windows.WindowState.Normal;
            ShowInTaskbar = true;
            IsVisible = true;
            SavedAt = DateTime.Now;
        }

        /// <summary>
        /// 从窗口创建状态对象
        /// </summary>
        /// <param name="window">源窗口</param>
        /// <returns>窗口状态对象</returns>
        public static WindowState FromWindow(Window window)
        {
            return new WindowState
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
        }

        /// <summary>
        /// 将状态应用到窗口
        /// </summary>
        /// <param name="window">目标窗口</param>
        public void ApplyToWindow(Window window)
        {
            try
            {
                // 验证坐标是否在屏幕范围内
                if (IsValidPosition())
                {
                    window.Left = Left;
                    window.Top = Top;
                }

                // 验证尺寸是否合理
                if (IsValidSize())
                {
                    window.Width = Width;
                    window.Height = Height;
                }

                window.WindowState = WinState;
                window.ShowInTaskbar = ShowInTaskbar;

                // 可见性需要特殊处理
                if (IsVisible)
                {
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用窗口状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证窗口位置是否有效
        /// </summary>
        /// <returns>位置是否有效</returns>
        private bool IsValidPosition()
        {
            // 检查位置是否在屏幕范围内
            try
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;

                return Left >= -Width && Left < screenWidth &&
                       Top >= 0 && Top < screenHeight;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证窗口尺寸是否有效
        /// </summary>
        /// <returns>尺寸是否有效</returns>
        private bool IsValidSize()
        {
            // 检查尺寸是否合理（不能太小或太大）
            const double minWidth = 300;
            const double minHeight = 200;
            const double maxWidth = 2000;
            const double maxHeight = 1500;

            return Width >= minWidth && Width <= maxWidth &&
                   Height >= minHeight && Height <= maxHeight;
        }

        /// <summary>
        /// 检查状态是否过期
        /// </summary>
        /// <param name="maxAge">最大有效期</param>
        /// <returns>是否过期</returns>
        public bool IsExpired(TimeSpan maxAge)
        {
            return DateTime.Now - SavedAt > maxAge;
        }

        /// <summary>
        /// 输出状态信息（用于调试）
        /// </summary>
        /// <returns>状态描述字符串</returns>
        public override string ToString()
        {
            return $"WindowState: Position=({Left},{Top}), Size=({Width},{Height}), " +
                   $"State={WinState}, ShowInTaskbar={ShowInTaskbar}, " +
                   $"IsVisible={IsVisible}, SavedAt={SavedAt:yyyy-MM-dd HH:mm:ss}";
        }
    }
} 