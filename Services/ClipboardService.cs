using System;
using System.Drawing;
using System.IO; // Required for BitmapEncoder, PngBitmapEncoder etc.
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging; // Required for BitmapSource, PngBitmapEncoder etc.

namespace OCR.Services
{
    public class ClipboardService
    {
        public bool CopyText(string text) // Kept as is
        {
            if (string.IsNullOrEmpty(text))
                return false;

            Exception lastException = null;
            for (int i = 0; i < 5; i++) // Retry mechanism
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Thread.Sleep(100);
                }
            }
            if (lastException != null)
                Console.WriteLine($"复制文本到剪贴板失败: {lastException.Message}");
            return false;
        }

        // Renamed from CopyImage to SetImage to match MainWindow's call
        public bool SetImage(Bitmap image)
        {
            if (image == null)
                return false;

            Exception lastException = null;
            for (int i = 0; i < 5; i++) // Retry mechanism
            {
                try
                {
                    // Convert System.Drawing.Bitmap to System.Windows.Media.Imaging.BitmapSource
                    using (var memory = new MemoryStream())
                    {
                        image.Save(memory, System.Drawing.Imaging.ImageFormat.Png); // Save to memory stream as PNG
                        memory.Position = 0;

                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load into memory
                        bitmapImage.EndInit();
                        bitmapImage.Freeze(); // Freeze for use on other threads if necessary, and for clipboard

                        Clipboard.SetImage(bitmapImage);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Thread.Sleep(100);
                }
            }
            if (lastException != null)
                Console.WriteLine($"设置图像到剪贴板失败: {lastException.Message}");
            return false;
        }


        public string GetText() // Kept as is
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    return Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从剪贴板获取文本失败: {ex.Message}");
            }
            return null;
        }

        public Bitmap GetImage() // Kept as is, but ensure it returns System.Drawing.Bitmap
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource bitmapSource = Clipboard.GetImage();
                    if (bitmapSource != null)
                    {
                        // Convert BitmapSource to System.Drawing.Bitmap
                        BitmapEncoder encoder = new PngBitmapEncoder(); // Or BmpBitmapEncoder, JpegBitmapEncoder
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        using (var stream = new MemoryStream())
                        {
                            encoder.Save(stream);
                            stream.Seek(0, SeekOrigin.Begin);
                            return new Bitmap(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从剪贴板获取图像失败: {ex.Message}");
            }
            return null;
        }

        public void Clear() // Kept as is
        {
            try
            {
                Clipboard.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清空剪贴板失败: {ex.Message}");
            }
        }
    }
}