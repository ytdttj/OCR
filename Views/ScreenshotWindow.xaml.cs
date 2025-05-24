using System;
using System.Drawing; // For Bitmap, Graphics, Rectangle (from System.Drawing.Common)
using System.Windows;
using System.Windows.Controls; // For Canvas
using System.Windows.Input;
using System.Windows.Media; // For VisualTreeHelper, DpiScale
// using System.Windows.Media.Imaging; // Not strictly needed if not converting bitmap for display here
using FormsScreen = System.Windows.Forms.Screen; // Alias to avoid conflict with a potential local Screen class
using Point = System.Windows.Point; // Alias for System.Windows.Point

namespace OCR.Views
{
    public partial class ScreenshotWindow : Window
    {
        private Bitmap _fullScreenBitmap; 
        private bool _isSelecting;
        private Point _startPointWpf; // MouseDown point in WPF units relative to canvasOverlay
        private Rect _selectionRectWpf; // Selection rectangle in WPF units

        public Bitmap CroppedScreenshot { get; private set; }

        public event EventHandler<Bitmap> ScreenshotTaken;
        public event EventHandler ScreenshotCancelled;

        public ScreenshotWindow(Bitmap fullScreenBitmap)
        {
            InitializeComponent();

            _fullScreenBitmap = fullScreenBitmap ?? throw new ArgumentNullException(nameof(fullScreenBitmap));
            _isSelecting = false;
            this.Cursor = Cursors.Cross;

            // We will set window size and position precisely in the Loaded event
            // when DPI information is reliably available.
            this.Loaded += ScreenshotWindow_Loaded;
        }

        private void ScreenshotWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get primary screen bounds in physical pixels
            Rectangle primaryScreenBounds = FormsScreen.PrimaryScreen.Bounds;

            // Get DPI for the current monitor where this window is displayed
            var dpiInfo = VisualTreeHelper.GetDpi(this); // Gets DpiScale

            // Calculate window size and position in WPF device-independent units (logical units)
            // The window should exactly cover the primary screen.
            // Left/Top are usually 0 for the primary screen relative to itself.
            this.Left = primaryScreenBounds.Left / dpiInfo.DpiScaleX;
            this.Top = primaryScreenBounds.Top / dpiInfo.DpiScaleY;
            this.Width = primaryScreenBounds.Width / dpiInfo.DpiScaleX;
            this.Height = primaryScreenBounds.Height / dpiInfo.DpiScaleY;

            // Ensure canvasOverlay (if named differently in XAML, adjust here) also matches this size.
            // In XAML, canvasOverlay's Width/Height are bound to Window's ActualWidth/Height,
            // so this should be automatic if the bindings are correct.
            // If canvasOverlay is the direct child of Window and Grid is not used, or Grid doesn't alter size.
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelAndClose();
            }
            else if (e.Key == Key.Enter && btnCapture.Visibility == Visibility.Visible)
            {
                CaptureAndClose();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isSelecting = true;
                // GetPosition relative to canvasOverlay, these are WPF device-independent units
                _startPointWpf = e.GetPosition(canvasOverlay); 

                borderSelection.Visibility = Visibility.Collapsed;
                btnCapture.Visibility = Visibility.Collapsed;
                btnCancel.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPointWpf = e.GetPosition(canvasOverlay);

                double x = Math.Min(_startPointWpf.X, currentPointWpf.X);
                double y = Math.Min(_startPointWpf.Y, currentPointWpf.Y);
                double width = Math.Abs(currentPointWpf.X - _startPointWpf.X);
                double height = Math.Abs(currentPointWpf.Y - _startPointWpf.Y);

                _selectionRectWpf = new Rect(x, y, width, height);

                Canvas.SetLeft(borderSelection, x);
                Canvas.SetTop(borderSelection, y);
                borderSelection.Width = width;
                borderSelection.Height = height;
                borderSelection.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && e.LeftButton == MouseButtonState.Released)
            {
                _isSelecting = false;

                if (_selectionRectWpf.Width < 5 || _selectionRectWpf.Height < 5)
                {
                    borderSelection.Visibility = Visibility.Collapsed;
                    btnCapture.Visibility = Visibility.Collapsed;
                    btnCancel.Visibility = Visibility.Collapsed;
                    return;
                }

                // --- 新的按钮定位逻辑 (尝试版本 2) ---
                double buttonMargin = 8; // 按钮之间的明确间距
                double edgeOffset = 2;   // 按钮组与选区边缘的微小偏移

                // 获取按钮的期望大小 (最好在XAML中设置固定大小，如 Width="28" Height="28")
                double btnWidth = btnCapture.Width > 0 && !double.IsNaN(btnCapture.Width) ? btnCapture.Width : 28;
                double btnHeight = btnCapture.Height > 0 && !double.IsNaN(btnCapture.Height) ? btnCapture.Height : 28;
                // 假设取消按钮和确认按钮大小相同

                // 垂直定位：按钮组在选区下方，或者如果空间不足则在上方
                double buttonsTop = _selectionRectWpf.Bottom + edgeOffset;
                if (buttonsTop + btnHeight > canvasOverlay.ActualHeight) // canvasOverlay 是截图窗口的Canvas
                {
                    buttonsTop = _selectionRectWpf.Top - btnHeight - edgeOffset;
                }
                // 确保按钮不会跑到Canvas顶部以上
                if (buttonsTop < 0) buttonsTop = edgeOffset;


                // 水平定位：
                // 确认按钮 (✓) 在右边
                double captureLeft = _selectionRectWpf.Right - btnWidth - edgeOffset;
                
                // 取消按钮 (✗) 在确认按钮的左边
                double cancelLeft = captureLeft - btnWidth - buttonMargin;

                // 如果选区太靠左，导致按钮会超出Canvas的左边界，则调整
                if (cancelLeft < edgeOffset)
                {
                    // 将按钮组整体右移，使最左边的取消按钮的左边缘在 edgeOffset 处
                    cancelLeft = edgeOffset;
                    captureLeft = cancelLeft + btnWidth + buttonMargin;
                }
                
                // 另一种情况：如果选区太窄，可能仍然导致重叠或排列不佳
                // 确保 captureLeft 不会小于 cancelLeft + btnWidth + buttonMargin
                // (虽然上面的逻辑应该已经处理了，但作为安全检查)
                if (captureLeft < cancelLeft + btnWidth + buttonMargin) {
                     captureLeft = cancelLeft + btnWidth + buttonMargin;
                }

                // 再次检查按钮是否会超出Canvas的右边界
                if (captureLeft + btnWidth > canvasOverlay.ActualWidth - edgeOffset)
                {
                    // 如果确认按钮会超出，则将整个按钮组左移
                    captureLeft = canvasOverlay.ActualWidth - btnWidth - edgeOffset;
                    cancelLeft = captureLeft - btnWidth - buttonMargin;
                     // 再次检查cancelLeft是否小于edgeOffset
                    if (cancelLeft < edgeOffset) cancelLeft = edgeOffset;
                }


                Canvas.SetLeft(btnCapture, captureLeft);
                Canvas.SetTop(btnCapture, buttonsTop);
                btnCapture.Visibility = Visibility.Visible;

                Canvas.SetLeft(btnCancel, cancelLeft);
                Canvas.SetTop(btnCancel, buttonsTop);
                btnCancel.Visibility = Visibility.Visible;
                // --- 结束新的按钮定位逻辑 ---
            }
            else if (_isSelecting == false && e.LeftButton == MouseButtonState.Released)
            {
                // 如果不是在选择过程中释放鼠标（例如，只是在窗口上点击了一下但没有拖动）
                // 或者选择区域过小后，也需要确保按钮是隐藏的。
                // （上面的逻辑在选择区域过小时已经处理了，这里作为补充）
                if (borderSelection.Visibility == Visibility.Visible && (_selectionRectWpf.Width < 5 || _selectionRectWpf.Height < 5) )
                {
                    borderSelection.Visibility = Visibility.Collapsed;
                }
                // 确保在没有有效选区时按钮总是隐藏的
                 if(btnCapture.Visibility == Visibility.Visible || btnCancel.Visibility == Visibility.Visible)
                 {
                    if(_selectionRectWpf.Width < 5 || _selectionRectWpf.Height < 5 || borderSelection.Visibility == Visibility.Collapsed)
                    {
                        btnCapture.Visibility = Visibility.Collapsed;
                        btnCancel.Visibility = Visibility.Collapsed;
                    }
                 }
            }
        }
        
        private void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            CaptureAndClose();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelAndClose();
        }

        private void CaptureAndClose()
        {
            if (_selectionRectWpf.Width <= 0 || _selectionRectWpf.Height <= 0)
            {
                MessageBox.Show("无效的选择区域。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                CancelAndClose(); // Ensure proper cleanup and event firing on invalid selection
                return;
            }

            try
            {
                // Get DPI for coordinate conversion
                var dpiInfo = VisualTreeHelper.GetDpi(this);
                double dpiScaleX = dpiInfo.DpiScaleX;
                double dpiScaleY = dpiInfo.DpiScaleY;

                // Convert WPF units (_selectionRectWpf) to physical pixels
                // The _selectionRectWpf coordinates are relative to the canvasOverlay,
                // which is assumed to be aligned with the top-left of the primary screen 
                // (due to window positioning in ScreenshotWindow_Loaded).
                // _fullScreenBitmap is a bitmap of the primary screen starting at its (0,0) in physical pixels.
                
                int physicalX = (int)Math.Round(_selectionRectWpf.X * dpiScaleX);
                int physicalY = (int)Math.Round(_selectionRectWpf.Y * dpiScaleY);
                int physicalWidth = (int)Math.Round(_selectionRectWpf.Width * dpiScaleX);
                int physicalHeight = (int)Math.Round(_selectionRectWpf.Height * dpiScaleY);

                // Basic sanity check for converted values
                if (physicalWidth <= 0 || physicalHeight <= 0)
                {
                    MessageBox.Show("选择区域转换后尺寸无效。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    CancelAndClose();
                    return;
                }
                
                // Clamp selection to the bounds of the _fullScreenBitmap (physical pixels)
                physicalX = Math.Max(0, Math.Min(physicalX, _fullScreenBitmap.Width - 1));
                physicalY = Math.Max(0, Math.Min(physicalY, _fullScreenBitmap.Height - 1));
                // Ensure width/height don't go beyond bitmap dimensions from the clamped X,Y
                physicalWidth = Math.Max(1, Math.Min(physicalWidth, _fullScreenBitmap.Width - physicalX));
                physicalHeight = Math.Max(1, Math.Min(physicalHeight, _fullScreenBitmap.Height - physicalY));
                
                if (physicalWidth <= 0 || physicalHeight <= 0) // Re-check after clamping
                {
                     MessageBox.Show("截图区域在调整后无效。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                     CancelAndClose();
                     return;
                }

                CroppedScreenshot = new Bitmap(physicalWidth, physicalHeight, _fullScreenBitmap.PixelFormat); // Use original PixelFormat
                using (Graphics g = Graphics.FromImage(CroppedScreenshot))
                {
                    // Source rectangle from _fullScreenBitmap (physical pixels)
                    System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle(physicalX, physicalY, physicalWidth, physicalHeight);
                    // Destination rectangle in the new CroppedScreenshot (starts at 0,0)
                    System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(0, 0, physicalWidth, physicalHeight);
                    
                    g.DrawImage(_fullScreenBitmap, destRect, srcRect, GraphicsUnit.Pixel);
                }
                ScreenshotTaken?.Invoke(this, CroppedScreenshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                CroppedScreenshot?.Dispose(); 
                CroppedScreenshot = null;
                ScreenshotCancelled?.Invoke(this, EventArgs.Empty); // Notify of cancellation/failure
            }
            finally
            {
                this.Close(); // This will trigger OnClosed where _fullScreenBitmap is disposed
            }
        }

        private void CancelAndClose()
        {
            CroppedScreenshot?.Dispose(); // Dispose if any partial crop was made and then cancelled
            CroppedScreenshot = null;
            ScreenshotCancelled?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _fullScreenBitmap?.Dispose(); 
        }
    }
}