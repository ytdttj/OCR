using System;
using System.Drawing;
using System.Drawing.Imaging; // For ImageFormat
using System.IO; // For Path
using System.Windows.Forms; // For Screen

namespace OCR.Services
{
    public class ScreenshotService
    {
        /// <summary>
        /// 捕获主显示器的整个屏幕。
        /// </summary>
        /// <returns>表示主屏幕截图的 Bitmap 对象，如果失败则返回 null。</returns>
        public Bitmap CaptureScreen()
        {
            try
            {
                // 获取主显示器的边界
                Rectangle bounds = Screen.PrimaryScreen.Bounds;

                // 创建一个与屏幕大小相同的 Bitmap
                Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

                // 从该 Bitmap 创建一个 Graphics 对象
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    // 将屏幕内容复制到 Graphics 对象
                    // CopyFromScreen 使用的是屏幕左上角的绝对坐标
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }
                return screenshot;
            }
            catch (Exception ex)
            {
                // 记录错误或向用户显示消息
                Console.WriteLine($"捕获屏幕失败: {ex.Message}");
                // 在实际应用中，可能需要更复杂的错误处理，例如通过事件通知调用者
                return null;
            }
        }

        /// <summary>
        /// 将截图保存到文件。
        /// </summary>
        /// <param name="screenshot">要保存的 Bitmap。</param>
        /// <param name="filePath">完整的文件路径。</param>
        /// <param name="format">图像格式。</param>
        public bool SaveScreenshot(Bitmap screenshot, string filePath, ImageFormat format)
        {
            if (screenshot == null)
                throw new ArgumentNullException(nameof(screenshot));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                screenshot.Save(filePath, format);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存截图失败到 {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}