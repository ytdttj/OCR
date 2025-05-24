using OCR.Models;
using OCR.Services;
using OCR.Views;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace OCR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private readonly ScreenshotService _screenshotService;
        private readonly OCRService _ocrService;
        private readonly HotkeyService _hotkeyService;
        private readonly ClipboardService _clipboardService;
        private AppSettings _settings; // Made non-readonly to allow refresh
        private bool _isExplicitlyClosed = false;

        public MainWindow()
        {
            InitializeComponent();

            // Load settings first
            LoadAndApplySettings();

            // Initialize services with proper parameters
            _screenshotService = new ScreenshotService();
            _clipboardService = new ClipboardService();
            _ocrService = new OCRService(_settings, CreateOCREngineFactory());
            _hotkeyService = new HotkeyService();

            // Register hotkeys from settings
            RegisterHotkeysFromSettings();

            this.Loaded += MainWindow_Loaded;
            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;

            // Tray icon double-click
            if (TaskbarIcon != null)
            {
                TaskbarIcon.TrayMouseDoubleClick += TaskbarIcon_DoubleClick;
            }
        }

        private Func<OcrEngineType, string, IOCREngine> CreateOCREngineFactory()
        {
            return (engineType, language) =>
            {
                switch (engineType)
                {
                    case OcrEngineType.WindowsOCR:
                        return new WindowsOCREngine(language);
                    case OcrEngineType.TesseractOCR:
                        return new TesseractOCREngine(language);
                    case OcrEngineType.PaddleOCR:
                        return new PaddleOCREngine(language, _settings);
                    default:
                        throw new ArgumentException($"不支持的OCR引擎类型: {engineType}");
                }
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if should start minimized to tray based on settings
            if (_settings.StartMinimized)
            {
                // Hide the main window and show tray icon
                this.Hide();
                this.ShowInTaskbar = false;
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Show the main window normally
                this.Show();
                this.ShowInTaskbar = true;
                if (TaskbarIcon != null)
                {
                    TaskbarIcon.Visibility = Visibility.Visible;
                }
            }
        }

        private void LoadAndApplySettings()
        {
            _settings = AppSettings.Load();
        }

        private void RegisterHotkeysFromSettings()
        {
            // Unregister any existing hotkeys
            _hotkeyService.UnregisterAllHotkeys();

            var currentModifiers = GetHotkeyModifiers();
            var currentKey = GetHotkeyKey();

            if (currentModifiers != ModifierKeys.None && currentKey != Key.None)
            {
                if (!_hotkeyService.RegisterHotkey(currentModifiers, currentKey, TakeScreenshotActionForHotkey))
                {
                    MessageBox.Show($"注册快捷键 {currentModifiers}+{currentKey} 失败。可能已被其他程序占用。", "快捷键错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Debug.WriteLine($"快捷键已注册: {currentModifiers} + {currentKey}");
                }
            }
            else
            {
                 Debug.WriteLine("快捷键未配置或配置不完整 (需要修饰键和主键)。");
            }
        }

        private ModifierKeys GetHotkeyModifiers()
        {
            ModifierKeys modifiers = ModifierKeys.None;
            if (_settings.Hotkey.Ctrl) modifiers |= ModifierKeys.Control;
            if (_settings.Hotkey.Shift) modifiers |= ModifierKeys.Shift;
            if (_settings.Hotkey.Alt) modifiers |= ModifierKeys.Alt;
            return modifiers;
        }

        private Key GetHotkeyKey()
        {
            try
            {
                if (!string.IsNullOrEmpty(_settings.Hotkey.Key))
                {
                    return (Key)Enum.Parse(typeof(Key), _settings.Hotkey.Key, true);
                }
            }
            catch
            {
                Debug.WriteLine($"无法解析快捷键: {_settings.Hotkey.Key}");
            }
            return Key.None;
        }
        
        // This method is for the hotkey to call
        public void TakeScreenshotActionForHotkey()
        {
            // Ensure this runs on the UI thread
            Dispatcher.Invoke(() =>
            {
                PerformScreenshot();
            });
        }

        private async void PerformScreenshot()
        {
            this.Hide();
            if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Collapsed;

            await System.Threading.Tasks.Task.Delay(200); 

            Bitmap screenCapture = null;
            try
            {
                screenCapture = _screenshotService.CaptureScreen(); 
                Debug.WriteLine("屏幕截图完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截取屏幕失败: {ex.Message}", "截图错误", MessageBoxButton.OK, MessageBoxImage.Error);
                RestoreMainWindowVisibility();
                return;
            }

            if (screenCapture == null)
            {
                MessageBox.Show("未能截取到屏幕图像。", "截图错误", MessageBoxButton.OK, MessageBoxImage.Error);
                RestoreMainWindowVisibility();
                return;
            }

            var screenshotWindow = new Views.ScreenshotWindow(screenCapture);
            screenshotWindow.Owner = this;
            
            // 使用更简单的事件处理方式
            Bitmap croppedImage = null;
            
            screenshotWindow.ScreenshotTaken += (s, capturedBitmap) => {
                Debug.WriteLine("ScreenshotTaken 事件触发");
                croppedImage = capturedBitmap;
            };
            
            screenshotWindow.ScreenshotCancelled += (s, args) => {
                Debug.WriteLine("ScreenshotCancelled 事件触发");
                croppedImage = null;
            };

            Debug.WriteLine("显示截图窗口");
            screenshotWindow.ShowDialog();
            Debug.WriteLine("截图窗口关闭");

            // 检查是否有有效的截图
            if (croppedImage != null)
            {
                Debug.WriteLine($"获得截图，尺寸: {croppedImage.Width}x{croppedImage.Height}");
                
                try
                {
                    // 复制截图到剪贴板
                    if (!_clipboardService.SetImage(croppedImage))
                    {
                        Debug.WriteLine("复制截图到剪贴板失败");
                    }
                    else
                    {
                        Debug.WriteLine("截图已复制到剪贴板");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"复制截图到剪贴板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // 进行OCR识别
                string ocrResultText = string.Empty;
                try
                {
                    Debug.WriteLine("开始OCR识别");
                    ocrResultText = await _ocrService.RecognizeTextAsync(croppedImage);
                    Debug.WriteLine($"OCR识别完成，结果长度: {ocrResultText?.Length ?? 0}");
                    
                    if (_settings.AutoCopyOcrResult && !string.IsNullOrEmpty(ocrResultText))
                    {
                        _clipboardService.CopyText(ocrResultText);
                        Debug.WriteLine("OCR结果已自动复制到剪贴板");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"OCR识别失败: {ex.Message}");
                    MessageBox.Show($"OCR识别失败: {ex.Message}", "OCR错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // 显示结果窗口
                try
                {
                    Debug.WriteLine("创建ResultWindow");
                    var resultWindow = new Views.ResultWindow(croppedImage, ocrResultText, _clipboardService, _screenshotService);
                    resultWindow.Owner = this;
                    Debug.WriteLine("显示ResultWindow");
                resultWindow.ShowDialog();
                    Debug.WriteLine("ResultWindow关闭");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"显示结果窗口失败: {ex.Message}");
                    MessageBox.Show($"显示结果失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    croppedImage?.Dispose(); // 如果ResultWindow创建失败，需要手动释放
                }
            }
            else
            {
                Debug.WriteLine("截图被取消或失败，croppedImage 为 null");
            }
            
            RestoreMainWindowVisibility();
            screenCapture?.Dispose();
        }

        private void RestoreMainWindowVisibility()
        {
            if (!_isExplicitlyClosed)
            {
                if (WindowState == WindowState.Minimized && ShowInTaskbar == false) // Was minimized to tray
                {
                    if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Visible;
                    // Don't show the main window itself, just the tray icon
                }
                else // Was hidden but not minimized to tray, or was normal
                {
                    this.Show();
                    this.Activate();
                    if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Collapsed; // Hide tray icon if window is shown
                }
            }
        }

        // --- UI Event Handlers ---
        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            PerformScreenshot();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _hotkeyService.UnregisterAllHotkeys(); 
            var settingsWindow = new SettingsWindow(_settings, _hotkeyService); // Pass HotkeyService
            settingsWindow.Owner = this;
            bool? dialogResult = settingsWindow.ShowDialog();
            
            LoadAndApplySettings(); // Reload settings as they might have changed
            _ocrService.SwitchEngine(_settings.SelectedOcrEngine); // Switch OCR engine based on new settings
            RegisterHotkeysFromSettings(); 
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _isExplicitlyClosed = true;
            Application.Current.Shutdown();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Visible;
            }
            else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
            {
                this.ShowInTaskbar = true; // Ensure it's true when not minimized
                if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Collapsed;
            }
        }
        
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExplicitlyClosed)
            {
                e.Cancel = true; 
                WindowState = WindowState.Minimized; 
            }
            else
            {
                // This is an explicit close, proceed with shutdown logic
                Dispose(); // Call Dispose to clean up resources
            }
        }

        // --- Tray Icon Event Handlers ---
        private void TaskbarIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }

        private void ContextMenuShow_Click(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }
        
        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.ShowInTaskbar = true;
            if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Collapsed;
        }

        private void ContextMenuCapture_Click(object sender, RoutedEventArgs e)
        {
            PerformScreenshot();
        }

        private void ContextMenuSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsButton_Click(sender, e);
        }

        private void ContextMenuExit_Click(object sender, RoutedEventArgs e)
        {
            _isExplicitlyClosed = true;
            Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _hotkeyService?.Dispose();
            _ocrService?.Dispose();
        }
    }
}