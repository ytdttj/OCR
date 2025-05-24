using OCR.Services;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WpfPoint = System.Windows.Point;

namespace OCR.Views
{
    /// <summary>
    /// ResultWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ResultWindow : Window
    {
        private readonly ClipboardService _clipboardService;
        private readonly ScreenshotService _screenshotService;
        private Bitmap _screenshot;
        private string _ocrResult;

        // 缩放相关字段
        private double _zoomLevel = 1.0;
        private const double MinZoom = 0.1;
        private const double MaxZoom = 5.0;
        private const double ZoomStep = 0.1;

        // 适配防抖相关字段
        private System.Windows.Threading.DispatcherTimer _resizeTimer;
        private bool _isFirstLoad = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="screenshot">截图</param>
        /// <param name="ocrResult">OCR识别结果</param>
        /// <param name="clipboardService">剪贴板服务</param>
        /// <param name="screenshotService">截图服务</param>
        public ResultWindow(Bitmap screenshot, string ocrResult, ClipboardService clipboardService, ScreenshotService screenshotService)
        {
            InitializeComponent();

            _screenshot = screenshot;
            _ocrResult = ocrResult;
            _clipboardService = clipboardService;
            _screenshotService = screenshotService;

            // 设置窗口内容
            DisplayScreenshot();
            txtOcrResult.Text = _ocrResult;

            // 设置窗口关闭事件
            this.Closed += ResultWindow_Closed;
            this.Loaded += ResultWindow_Loaded;
        }

        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        private void ResultWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化防抖计时器
            _resizeTimer = new System.Windows.Threading.DispatcherTimer();
            _resizeTimer.Interval = TimeSpan.FromMilliseconds(100);
            _resizeTimer.Tick += ResizeTimer_Tick;

            // 延迟执行适配，确保UI完全加载
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FitImageToContainer();
                _isFirstLoad = false;
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void ResultWindow_Closed(object sender, EventArgs e)
        {
            // 停止并释放计时器
            _resizeTimer?.Stop();
            _resizeTimer = null;
            
            // 释放截图资源
            _screenshot?.Dispose();
        }

        /// <summary>
        /// ScrollViewer尺寸变化事件处理
        /// </summary>
        private void imgScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 跳过初始加载时的尺寸变化事件
            if (_isFirstLoad) return;

            // 使用防抖机制，避免频繁重新适配
            if (_resizeTimer != null)
            {
                _resizeTimer.Stop();
                _resizeTimer.Start();
            }
        }

        /// <summary>
        /// 防抖计时器触发事件
        /// </summary>
        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            _resizeTimer?.Stop();
            FitImageToContainer();
        }

        /// <summary>
        /// 显示截图
        /// </summary>
        private void DisplayScreenshot()
        {
            if (_screenshot != null)
            {
                imgScreenshot.Source = ConvertBitmapToBitmapSource(_screenshot);
            }
        }

        /// <summary>
        /// 自适应显示图片到容器 - 图片上下居中、靠左显示，确保完整适配
        /// </summary>
        private void FitImageToContainer()
        {
            if (_screenshot == null) return;

            // 获取截图原始尺寸
            double imageWidth = _screenshot.Width;
            double imageHeight = _screenshot.Height;

            // 获取图片显示区域尺寸
            double containerWidth = imgScrollViewer.ActualWidth;
            double containerHeight = imgScrollViewer.ActualHeight;

            // 检查容器尺寸有效性
            if (containerWidth <= 0 || containerHeight <= 0) 
            {
                // 如果容器尺寸无效，延迟重试
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitImageToContainer();
                }), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }

            // 预留边距空间（考虑可能的滚动条，使用更小的边距）
            const double margin = 5;
            double availableWidth = Math.Max(containerWidth - margin, containerWidth * 0.95);
            double availableHeight = Math.Max(containerHeight - margin, containerHeight * 0.95);

            // 计算X和Y方向的缩放比例
            double scaleX = availableWidth / imageWidth;
            double scaleY = availableHeight / imageHeight;
            
            // 选择较小的缩放比例，确保缩放后的图片长宽都小于显示区域
            double scale = Math.Min(scaleX, scaleY);
            
            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"Image: {imageWidth}x{imageHeight}, Container: {containerWidth}x{containerHeight}");
            System.Diagnostics.Debug.WriteLine($"Available: {availableWidth}x{availableHeight}, ScaleX: {scaleX:F3}, ScaleY: {scaleY:F3}, Final Scale: {scale:F3}");
            
            // 限制缩放比例在合理范围内
            scale = Math.Max(0.05, Math.Min(5.0, scale));
            
            // 应用缩放
            _zoomLevel = scale;
            ApplyZoom();
            
            // 重置滚动位置，确保图片从左上角开始显示
            ResetScrollPosition();
        }

        /// <summary>
        /// 应用缩放
        /// </summary>
        private void ApplyZoom()
        {
            // 应用缩放变换
            imgScaleTransform.ScaleX = _zoomLevel;
            imgScaleTransform.ScaleY = _zoomLevel;

            // 更新缩放比例显示
            txtZoomLevel.Text = $"{Math.Round(_zoomLevel * 100)}%";
        }

        /// <summary>
        /// 重置ScrollViewer位置，确保图片靠左且垂直居中
        /// </summary>
        private void ResetScrollPosition()
        {
            // 延迟执行，确保缩放变换已经应用
            Dispatcher.BeginInvoke(new Action(() =>
            {
                imgScrollViewer.ScrollToHorizontalOffset(0);
                
                // 计算垂直居中位置
                double totalHeight = imgScrollViewer.ExtentHeight;
                double viewportHeight = imgScrollViewer.ViewportHeight;
                double centerOffset = Math.Max(0, (totalHeight - viewportHeight) / 2);
                imgScrollViewer.ScrollToVerticalOffset(centerOffset);
                
                System.Diagnostics.Debug.WriteLine($"Scroll reset - TotalHeight: {totalHeight}, ViewportHeight: {viewportHeight}, CenterOffset: {centerOffset}");
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 放大按钮点击事件
        /// </summary>
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel < MaxZoom)
            {
                _zoomLevel = Math.Min(MaxZoom, _zoomLevel + ZoomStep);
                ApplyZoom();
            }
        }

        /// <summary>
        /// 缩小按钮点击事件
        /// </summary>
        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel > MinZoom)
            {
                _zoomLevel = Math.Max(MinZoom, _zoomLevel - ZoomStep);
                ApplyZoom();
            }
        }

        /// <summary>
        /// 适配窗口按钮点击事件
        /// </summary>
        private void btnFitToWindow_Click(object sender, RoutedEventArgs e)
        {
            FitImageToContainer();
        }

        /// <summary>
        /// 原始尺寸按钮点击事件
        /// </summary>
        private void btnActualSize_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = 1.0;
            ApplyZoom();
        }

        /// <summary>
        /// 鼠标滚轮事件 - 缩放控制
        /// </summary>
        private void imgScreenshot_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 只有按住Ctrl键时才进行缩放
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
                double newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, _zoomLevel + delta));
                
                if (newZoom != _zoomLevel)
                {
                    _zoomLevel = newZoom;
                    ApplyZoom();
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// 将System.Drawing.Bitmap转换为BitmapSource
        /// </summary>
        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        /// <summary>
        /// 保存截图按钮点击事件
        /// </summary>
        private void btnSaveScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (_screenshot == null)
            {
                MessageBox.Show("没有可用的截图", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 创建保存文件对话框
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG图像|*.png|JPEG图像|*.jpg|BMP图像|*.bmp|所有文件|*.*",
                    DefaultExt = ".png",
                    FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                // 显示对话框
                if (saveFileDialog.ShowDialog() == true)
                {
                    // 获取保存路径
                    string filePath = saveFileDialog.FileName;
                    
                    // 确定图像格式
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;
                    string extension = Path.GetExtension(filePath).ToLower();
                    
                    if (extension == ".jpg" || extension == ".jpeg")
                    {
                        format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    }
                    else if (extension == ".bmp")
                    {
                        format = System.Drawing.Imaging.ImageFormat.Bmp;
                    }

                    // 保存图像
                    _screenshotService.SaveScreenshot(_screenshot, filePath, format);
                    
                    // 显示成功消息
                    MessageBox.Show($"图像已成功保存到：{filePath}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 复制文字按钮点击事件
        /// </summary>
        private void btnCopyText_Click(object sender, RoutedEventArgs e)
        {
            string text = txtOcrResult.Text;
            if (!string.IsNullOrEmpty(text))
            {
                if (_clipboardService.CopyText(text))
                {
                    MessageBox.Show("文本已复制到剪贴板", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("复制文本失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("没有可用的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}