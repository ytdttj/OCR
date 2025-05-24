using OCR.Models; // Models namespace for AppSettings and OcrEngineType
using System;
using System.Drawing;
using System.Linq; // For Array.Empty and ToArray
using System.Threading.Tasks;

namespace OCR.Services
{
    public class OCRService : IDisposable
    {
        private IOCREngine _currentOcrEngine;
        private readonly Func<OcrEngineType, string, IOCREngine> _engineFactory; // Corrected OcrEngineType
        private readonly AppSettings _settings;

        public IOCREngine CurrentEngine => _currentOcrEngine;

        public OCRService(AppSettings settings, Func<OcrEngineType, string, IOCREngine> engineFactory) // Corrected OcrEngineType
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
            
            InitializeEngine(_settings.SelectedOcrEngine, _settings.TesseractLanguage);
        }

        private void InitializeEngine(OcrEngineType engineType, string tesseractLanguage) // Corrected OcrEngineType
        {
            try
            {
                string language = engineType == OcrEngineType.TesseractOCR ? tesseractLanguage : _settings.LanguageCode; // Use general LanguageCode for WindowsOCR
                _currentOcrEngine = _engineFactory(engineType, language);
            }
            catch (Exception ex)
            {
                if (engineType != OcrEngineType.WindowsOCR)
                {
                    try
                    {
                        Console.WriteLine($"警告: 初始化 {engineType} 失败 (语言: {tesseractLanguage}): {ex.Message}. 尝试回退到 Windows OCR.");
                        _currentOcrEngine = _engineFactory(OcrEngineType.WindowsOCR, _settings.LanguageCode); // Use general LanguageCode
                    }
                    catch (Exception fallbackEx)
                    {
                        throw new InvalidOperationException($"初始化主OCR引擎 ({engineType}) 和回退OCR引擎 (WindowsOCR) 均失败。主引擎错误: {ex.Message}; 回退引擎错误: {fallbackEx.Message}", fallbackEx);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"初始化OCR引擎 {engineType} (语言: { (engineType == OcrEngineType.TesseractOCR ? tesseractLanguage : _settings.LanguageCode) }) 失败: {ex.Message}", ex);
                }
            }
        }

        public void SwitchEngine(OcrEngineType engineType, string languageToUse = null) // Corrected OcrEngineType
        {
            if (_currentOcrEngine is IDisposable disposableEngine)
            {
                disposableEngine.Dispose();
            }
            
            string lang = languageToUse;
            if (string.IsNullOrEmpty(lang))
            {
                lang = engineType == OcrEngineType.TesseractOCR ? _settings.TesseractLanguage : _settings.LanguageCode;
            }

            InitializeEngine(engineType, lang); // Pass correct language based on engine type
        }

        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (_currentOcrEngine == null)
                throw new InvalidOperationException("OCR引擎未初始化。");

            try
            {
                return await _currentOcrEngine.RecognizeTextAsync(image);
            }
            catch (Exception ex)
            {
                // Corrected: Removed trailing backslash before the closing quote
                throw new Exception($"OCR识别失败 ({_currentOcrEngine.EngineName}): {ex.Message}", ex);
            }
        }

        public string[] GetSupportedLanguages()
        {
            if (_currentOcrEngine == null) return Array.Empty<string>();
            try
            {
                return _currentOcrEngine.GetSupportedLanguages()?.ToArray() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Corrected: Removed trailing backslash before the closing quote
                throw new Exception($"获取支持的语言列表失败 ({_currentOcrEngine.EngineName}): {ex.Message}", ex);
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                throw new ArgumentException("语言代码不能为空", nameof(languageCode));
            if (_currentOcrEngine == null)
                throw new InvalidOperationException("OCR引擎未初始化。");

            try
            {
                // Update AppSettings for the respective engine
                if (_currentOcrEngine.EngineName.Equals("Tesseract OCR", StringComparison.OrdinalIgnoreCase))
                {
                    _settings.TesseractLanguage = languageCode;
                }
                else if (_currentOcrEngine.EngineName.Equals("Windows OCR", StringComparison.OrdinalIgnoreCase))
                {
                     _settings.LanguageCode = languageCode; // Assuming Windows OCR uses the general LanguageCode
                }
                _settings.Save(); // Save settings after changing language preference

                _currentOcrEngine.SetLanguage(languageCode);
            }
            catch (Exception ex)
            {
                // Corrected: Removed trailing backslash before the closing quote
                throw new Exception($"设置OCR语言失败 ({_currentOcrEngine.EngineName}, 语言: {languageCode}): {ex.Message}", ex);
            }
        }

        public string GetCurrentLanguage()
        {
            return _currentOcrEngine?.CurrentLanguage;
        }

        public string GetCurrentEngineName()
        {
            return _currentOcrEngine?.EngineName ?? "未初始化";
        }

        public void Dispose()
        {
            if (_currentOcrEngine is IDisposable disposable)
            {
                disposable.Dispose();
                _currentOcrEngine = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}