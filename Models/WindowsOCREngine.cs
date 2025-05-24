using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime; // For AsBuffer, AsStream
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging; // For BitmapDecoder, SoftwareBitmap
using Windows.Media.Ocr;

namespace OCR.Models
{
    public class WindowsOCREngine : IOCREngine
    {
        private OcrEngine _ocrEngine;
        private string _currentLanguage;

        public WindowsOCREngine() : this(null) // 无参构造函数调用带参构造函数
        {
        }

        public WindowsOCREngine(string languageCode)
        {
            InitializeEngine(languageCode);
        }

        private void InitializeEngine(string languageCode)
        {
            try
            {
                Language languageToUse = null;
                if (!string.IsNullOrEmpty(languageCode) && OcrEngine.IsLanguageSupported(new Language(languageCode)))
                {
                    languageToUse = new Language(languageCode);
                    _currentLanguage = languageCode;
                }
                else
                {
                    // 尝试用户首选语言
                    var userLanguages = Windows.Globalization.ApplicationLanguages.Languages;
                    var supportedUserLang = userLanguages.FirstOrDefault(l => OcrEngine.IsLanguageSupported(new Language(l)));
                    if (supportedUserLang != null)
                    {
                        languageToUse = new Language(supportedUserLang);
                        _currentLanguage = supportedUserLang;
                    }
                    else // 回退到第一个可用的或英语
                    {
                        var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
                        if (availableLanguages.Any())
                        {
                            languageToUse = availableLanguages.First();
                            _currentLanguage = languageToUse.LanguageTag;
                        }
                        else if (OcrEngine.IsLanguageSupported(new Language("en"))) // Fallback to English
                        {
                            languageToUse = new Language("en");
                            _currentLanguage = "en";
                        }
                    }
                }
                
                if (languageToUse != null)
                {
                    _ocrEngine = OcrEngine.TryCreateFromLanguage(languageToUse);
                }

                if (_ocrEngine == null) // 如果最终还是没有成功创建引擎
                {
                    // 尝试使用系统第一个支持的语言 (作为最后的手段)
                    var availableDefault = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault();
                    if (availableDefault != null)
                    {
                         _ocrEngine = OcrEngine.TryCreateFromLanguage(availableDefault);
                         _currentLanguage = availableDefault.LanguageTag;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("初始化OCR引擎失败: " + ex.Message, ex);
            }

            if (_ocrEngine == null)
            {
                throw new InvalidOperationException("无法创建OCR引擎，可能是系统不支持OCR功能或没有安装相应的语言包。");
            }
        }

        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (_ocrEngine == null)
                throw new InvalidOperationException("OCR引擎未初始化");

            try
            {
                SoftwareBitmap softwareBitmap;
                using (var stream = new MemoryStream())
                {
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp); // 使用BMP格式，Windows OCR可能更喜欢
                    stream.Position = 0;
                    var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
                
                var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
                var result = new StringBuilder();
                foreach (var line in ocrResult.Lines)
                {
                    result.AppendLine(line.Text);
                }
                return result.ToString().TrimEnd('\n', '\r'); // Trim trailing newlines
            }
            catch (Exception ex)
            {
                throw new Exception("Windows OCR识别失败: " + ex.Message, ex);
            }
        }
        
        // ConvertBitmapToSoftwareBitmapAsync (私有方法，上面已集成逻辑)

        public IEnumerable<string> GetSupportedLanguages()
        {
            try
            {
                return OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag).ToList();
            }
            catch
            {
                return new List<string> { "en" }; // Fallback
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                throw new ArgumentException("语言代码不能为空");

            try
            {
                var newLanguage = new Language(languageCode);
                if (!OcrEngine.IsLanguageSupported(newLanguage))
                    throw new ArgumentException($"OCR不支持语言: {languageCode}");

                var newEngine = OcrEngine.TryCreateFromLanguage(newLanguage);
                if (newEngine == null)
                    throw new InvalidOperationException($"无法为语言 {languageCode} 创建OCR引擎");

                _ocrEngine = newEngine;
                _currentLanguage = languageCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"设置OCR语言 {languageCode} 失败: {ex.Message}", ex);
            }
        }

        public string CurrentLanguage => _currentLanguage;
        public string EngineName => "Windows OCR";

        // No explicit Dispose needed as OcrEngine is a Windows Runtime type managed by the GC.
    }
}