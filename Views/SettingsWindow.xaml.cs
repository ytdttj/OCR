using OCR.Models;
using OCR.Services;
using System;
using System.Collections.Generic;
using System.IO; // For Path and Directory
using System.Linq;
using System.Reflection; // For Assembly.GetExecutingAssembly().Location
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
// For Windows OCR languages, if not accessing OcrEngine directly
// using Windows.Globalization; 
// using Windows.Media.Ocr; 

namespace OCR.Views
{
    // Helper class for ComboBox language selection
    public class LanguageSelectionItem
    {
        public string DisplayName { get; set; }
        public string Code { get; set; } // For Tesseract: chi_sim; For Windows: zh-CN

        public override string ToString() => DisplayName; // For simple binding if needed
    }

    public partial class SettingsWindow : Window
    {
        // Definition for OcrEngineSelection, placed inside SettingsWindow class
        private class OcrEngineSelection
        {
            public string DisplayName { get; set; }
            public OcrEngineType EngineType { get; set; } // OcrEngineType from Models namespace
        }

        private readonly AppSettings _settings;
        private readonly HotkeyService _hotkeyService;
        // private readonly OCRService _ocrService; 

        private List<OcrEngineSelection> _ocrEngineOptions; // Now OcrEngineSelection should be found
        private List<LanguageSelectionItem> _tesseractLanguages;
        private List<LanguageSelectionItem> _windowsOcrLanguages;
        private List<LanguageSelectionItem> _paddleOcrLanguages;


        public SettingsWindow(AppSettings settings, HotkeyService hotkeyService /*, OCRService ocrService */) 
        {
            InitializeComponent();
            
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            // _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            
            _tesseractLanguages = new List<LanguageSelectionItem>();
            _windowsOcrLanguages = new List<LanguageSelectionItem>();
            _paddleOcrLanguages = new List<LanguageSelectionItem>();

            InitializeKeyOptions();
            InitializeOcrEngineOptions(); 
            LoadSettings(); 
        }

        private void InitializeKeyOptions()
        {
            var keys = new List<string>();
            for (char c = 'A'; c <= 'Z'; c++) keys.Add(c.ToString());
            for (char c = '0'; c <= '9'; c++) keys.Add(c.ToString());
            for (int i = 1; i <= 12; i++) keys.Add($"F{i}");
            
            cmbKey.ItemsSource = keys;
        }

        private void InitializeOcrEngineOptions()
        {
            // Now using the internally defined OcrEngineSelection
            _ocrEngineOptions = new List<OcrEngineSelection>
            {
                new OcrEngineSelection { DisplayName = "Windows OCR (系统)", EngineType = OcrEngineType.WindowsOCR },
                new OcrEngineSelection { DisplayName = "Tesseract OCR", EngineType = OcrEngineType.TesseractOCR },
                new OcrEngineSelection { DisplayName = "PaddleOCR", EngineType = OcrEngineType.PaddleOCR }
            };

            cmbOcrEngine.ItemsSource = _ocrEngineOptions;
            cmbOcrEngine.DisplayMemberPath = "DisplayName";
        }

        private void LoadTesseractLanguages()
        {
            _tesseractLanguages.Clear();
            try
            {
                string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
                string tessdataDir = Path.Combine(baseDirectory, "tessdata");

                if (Directory.Exists(tessdataDir))
                {
                    var trainedDataFiles = Directory.GetFiles(tessdataDir, "*.traineddata");
                    foreach (var file in trainedDataFiles)
                    {
                        string langCode = Path.GetFileNameWithoutExtension(file);
                        string displayName = langCode; 
                        switch (langCode.ToLower())
                        {
                            case "chi_sim": displayName = "简体中文 (Simplified Chinese)"; break;
                            case "chi_tra": displayName = "繁體中文 (Traditional Chinese)"; break;
                            case "eng": displayName = "English"; break;
                            case "jpn": displayName = "日本語 (Japanese)"; break;
                        }
                        _tesseractLanguages.Add(new LanguageSelectionItem { Code = langCode, DisplayName = displayName });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载Tesseract语言文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            _tesseractLanguages = _tesseractLanguages.OrderBy(l => l.DisplayName).ToList();
            cmbTesseractLanguages.ItemsSource = _tesseractLanguages;
            cmbTesseractLanguages.DisplayMemberPath = "DisplayName";
        }

        private void LoadWindowsOcrLanguages()
        {
            _windowsOcrLanguages.Clear();
            try
            {
                var ocrLanguages = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages;
                foreach (var lang in ocrLanguages)
                {
                    _windowsOcrLanguages.Add(new LanguageSelectionItem { Code = lang.LanguageTag, DisplayName = $"{lang.DisplayName} ({lang.LanguageTag})" });
                }
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"加载Windows OCR支持的语言列表失败: {ex.Message}\n请确保您的系统支持Windows OCR功能。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _windowsOcrLanguages.Add(new LanguageSelectionItem { Code = "en-US", DisplayName = "English (US) - Default" }); 
            }
            
            _windowsOcrLanguages = _windowsOcrLanguages.OrderBy(l => l.DisplayName).ToList();
            cmbWindowsOcrLanguages.ItemsSource = _windowsOcrLanguages;
            cmbWindowsOcrLanguages.DisplayMemberPath = "DisplayName";
        }

        private void LoadPaddleOcrLanguages()
        {
            _paddleOcrLanguages.Clear();
            _paddleOcrLanguages.Add(new LanguageSelectionItem { Code = "ch", DisplayName = "中文 (Chinese)" });
            _paddleOcrLanguages.Add(new LanguageSelectionItem { Code = "en", DisplayName = "English" });
            
            cmbPaddleOcrLanguages.ItemsSource = _paddleOcrLanguages;
            cmbPaddleOcrLanguages.DisplayMemberPath = "DisplayName";
        }

        private void LoadSettings()
        {
            chkCtrl.IsChecked = _settings.Hotkey.Ctrl;
            chkShift.IsChecked = _settings.Hotkey.Shift;
            chkAlt.IsChecked = _settings.Hotkey.Alt;
            cmbKey.SelectedItem = _settings.Hotkey.Key;
            if (string.IsNullOrEmpty(cmbKey.SelectedItem?.ToString()) && cmbKey.Items.Count > 0)
            {
                 cmbKey.SelectedItem = _settings.Hotkey.Key ?? "C";
                 if(cmbKey.SelectedIndex == -1 && cmbKey.Items.Contains(_settings.Hotkey.Key)) cmbKey.SelectedItem = _settings.Hotkey.Key;
                 else if (cmbKey.SelectedIndex == -1) cmbKey.SelectedIndex = 0;
            }

            var selectedEngineOption = _ocrEngineOptions.FirstOrDefault(opt => opt.EngineType == _settings.SelectedOcrEngine);
            cmbOcrEngine.SelectedItem = selectedEngineOption ?? _ocrEngineOptions.FirstOrDefault();
            
            UpdateOcrEngineSpecificSettingVisibility(); 

            if (_settings.SelectedOcrEngine == OcrEngineType.TesseractOCR)
            {
                var tesseractLangItem = _tesseractLanguages.FirstOrDefault(l => l.Code.Equals(_settings.TesseractLanguage, StringComparison.OrdinalIgnoreCase));
                cmbTesseractLanguages.SelectedItem = tesseractLangItem ?? _tesseractLanguages.FirstOrDefault();
            }
            else if (_settings.SelectedOcrEngine == OcrEngineType.WindowsOCR)
            {
                var windowsLangItem = _windowsOcrLanguages.FirstOrDefault(l => l.Code.Equals(_settings.LanguageCode, StringComparison.OrdinalIgnoreCase));
                cmbWindowsOcrLanguages.SelectedItem = windowsLangItem ?? _windowsOcrLanguages.FirstOrDefault();
            }
            else if (_settings.SelectedOcrEngine == OcrEngineType.PaddleOCR)
            {
                LoadPaddleOcrLanguages();
                var paddleLangItem = _paddleOcrLanguages.FirstOrDefault(l => l.Code.Equals(_settings.PaddleOcrLanguage, StringComparison.OrdinalIgnoreCase));
                cmbPaddleOcrLanguages.SelectedItem = paddleLangItem ?? _paddleOcrLanguages.FirstOrDefault();
                
                txtPaddleModelPath.Text = _settings.PaddleOcrModelPath;
                chkPaddleUseGpu.IsChecked = _settings.PaddleOcrUseGpu;
                txtPaddleMaxSideLen.Text = _settings.PaddleOcrMaxSideLen.ToString();
            }
            
            chkStartMinimized.IsChecked = _settings.StartMinimized;
            chkAutoCopyOcrResult.IsChecked = _settings.AutoCopyOcrResult;
        }

        private bool ApplyHotkeySettingsFromUi()
        {
            if (!(chkCtrl.IsChecked == true || chkShift.IsChecked == true || chkAlt.IsChecked == true) || cmbKey.SelectedItem == null)
            {
                MessageBox.Show("请至少选择一个修饰键（Ctrl、Shift 或 Alt）并选择一个主键。", "快捷键无效", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            _settings.Hotkey.Ctrl = chkCtrl.IsChecked ?? false;
            _settings.Hotkey.Shift = chkShift.IsChecked ?? false;
            _settings.Hotkey.Alt = chkAlt.IsChecked ?? false;
            _settings.Hotkey.Key = cmbKey.SelectedItem?.ToString() ?? "C";
            return true;
        }

        private void ApplyOtherSettingsFromUi()
        {
            if (cmbOcrEngine.SelectedItem is OcrEngineSelection selectedEngine)
            {
                _settings.SelectedOcrEngine = selectedEngine.EngineType;
                if (selectedEngine.EngineType == OcrEngineType.TesseractOCR)
                {
                    _settings.TesseractLanguage = (cmbTesseractLanguages.SelectedItem as LanguageSelectionItem)?.Code ?? _tesseractLanguages.FirstOrDefault()?.Code ?? "eng";
                }
                else if (selectedEngine.EngineType == OcrEngineType.WindowsOCR)
                {
                    _settings.LanguageCode = (cmbWindowsOcrLanguages.SelectedItem as LanguageSelectionItem)?.Code ?? _windowsOcrLanguages.FirstOrDefault()?.Code ?? "en-US";
                }
                else if (selectedEngine.EngineType == OcrEngineType.PaddleOCR)
                {
                    _settings.PaddleOcrLanguage = (cmbPaddleOcrLanguages.SelectedItem as LanguageSelectionItem)?.Code ?? _paddleOcrLanguages.FirstOrDefault()?.Code ?? "ch";
                    _settings.PaddleOcrModelPath = txtPaddleModelPath.Text;
                    _settings.PaddleOcrUseGpu = chkPaddleUseGpu.IsChecked ?? false;
                    _settings.PaddleOcrMaxSideLen = int.TryParse(txtPaddleMaxSideLen.Text, out int maxSideLen) ? maxSideLen : 1000;
                }
            }
            
            _settings.StartMinimized = chkStartMinimized.IsChecked ?? false;
            _settings.AutoCopyOcrResult = chkAutoCopyOcrResult.IsChecked ?? false;
        }
        
        private void btnSaveHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (!ApplyHotkeySettingsFromUi())
            {
                return; 
            }
            _settings.Save(); 

            bool hotkeyUpdatedSuccessfully = false;
            try
            {
                hotkeyUpdatedSuccessfully = _hotkeyService.UpdateHotkeyFromSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用新快捷键时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (hotkeyUpdatedSuccessfully)
            {
                MessageBox.Show("快捷键设置已保存并尝试应用。", "快捷键已保存", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                 MessageBox.Show("快捷键设置已保存，但尝试应用新快捷键失败。请检查是否有其他提示或重启主程序。", "快捷键提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbOcrEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOcrEngineSpecificSettingVisibility();
        }

        private void UpdateOcrEngineSpecificSettingVisibility()
        {
            panelWindowsOcrSettings.Visibility = Visibility.Collapsed;
            panelTesseractOcrSettings.Visibility = Visibility.Collapsed;
            panelPaddleOcrSettings.Visibility = Visibility.Collapsed;

            if (cmbOcrEngine.SelectedItem is OcrEngineSelection selectedEngine) // Now uses the internally defined OcrEngineSelection
            {
                switch (selectedEngine.EngineType)
                {
                    case OcrEngineType.WindowsOCR:
                        panelWindowsOcrSettings.Visibility = Visibility.Visible;
                        if (!cmbWindowsOcrLanguages.HasItems) LoadWindowsOcrLanguages(); 
                        break;
                    case OcrEngineType.TesseractOCR:
                        panelTesseractOcrSettings.Visibility = Visibility.Visible;
                        if (!cmbTesseractLanguages.HasItems) LoadTesseractLanguages(); 
                        break;
                    case OcrEngineType.PaddleOCR:
                        panelPaddleOcrSettings.Visibility = Visibility.Visible;
                        if (!cmbPaddleOcrLanguages.HasItems) LoadPaddleOcrLanguages();
                        break;
                }
            }
        }
        
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ApplyHotkeySettingsFromUi()) 
            {
                return; 
            }
            ApplyOtherSettingsFromUi(); 
            _settings.Save(); 

            bool hotkeyUpdatedSuccessfully = false;
            try
            {
                hotkeyUpdatedSuccessfully = _hotkeyService.UpdateHotkeyFromSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"尝试应用新快捷键时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (hotkeyUpdatedSuccessfully)
            {
                MessageBox.Show("所有设置已保存。\n新的快捷键和OCR引擎设置将在主程序中应用（快捷键可能已立即生效）。",
                                "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("所有设置已保存，但尝试应用新的快捷键失败。请检查主程序提示或重新启动。",
                                "保存部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            this.DialogResult = true; 
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnBrowseModelPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择PaddleOCR模型文件夹",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹",
                Filter = "文件夹|*.folder"
            };

            if (dialog.ShowDialog() == true)
            {
                txtPaddleModelPath.Text = Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void cmbPaddleOcrLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPaddleOcrLanguages.SelectedItem is LanguageSelectionItem selectedLang)
            {
                // 根据选择的语言自动设置模型路径
                string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
                string modelPath = Path.Combine(baseDirectory, "Models", "PaddleOCR", selectedLang.Code);
                
                // 检查该语言的模型文件夹是否存在
                if (Directory.Exists(modelPath))
                {
                    txtPaddleModelPath.Text = modelPath;
                }
                else
                {
                    // 如果特定语言文件夹不存在，使用通用路径
                    string generalPath = Path.Combine(baseDirectory, "Models", "PaddleOCR");
                    txtPaddleModelPath.Text = generalPath;
                }
            }
        }
    }
}