using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq; // For Select
using System.Reflection;
using System.Threading.Tasks;
using Tesseract; // Namespace from the Tesseract NuGet package
using OCR.Models;
using OCR.Services;

namespace OCR.Models
{
    public class TesseractOCREngine : IOCREngine, IDisposable
    {
        private TesseractEngine _engine;
        private readonly AppSettings _settings;
        private string _currentLanguage;
        private bool _disposed = false;
        private readonly ResourceExtractionService _resourceService;

        public string EngineName => "Tesseract OCR";
        public string CurrentLanguage => _currentLanguage;

        public TesseractOCREngine(string language, AppSettings? settings = null)
        {
            _settings = settings ?? AppSettings.Load();
            _currentLanguage = ConvertLanguageCode(language ?? "en");
            _resourceService = new ResourceExtractionService();
            
            // 初始化时设置语言并创建引擎
            SetLanguage(_currentLanguage);
        }

        private string GetTessDataPath()
        {
            // 使用ResourceExtractionService获取用户数据目录中的tessdata路径
            return _resourceService.GetTesseractDataPath();
        }

        private string ConvertLanguageCode(string language)
        {
            // 将标准语言代码转换为Tesseract语言代码
            return language?.ToLower() switch
            {
                "ch" or "chinese" or "zh" => "chi_sim", // 简体中文
                "en" or "english" => "eng",             // 英文
                "jp" or "japanese" or "ja" => "jpn",    // 日文
                _ => "eng" // 默认为英文
            };
        }

        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            if (_engine == null)
                throw new InvalidOperationException("Tesseract引擎未初始化或已释放。");
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            // TesseractEngine.Process是同步的，在Task.Run中运行以避免阻塞UI线程
            return await Task.Run(() =>
            {
                try
                {
                    _engine.DefaultPageSegMode = PageSegMode.Auto; // 或者根据需要选择
                    using (var ms = new MemoryStream())
                    {
                        // 使用PNG格式以获得较好的无损质量，确保Tesseract能很好处理
                        // 对于某些图像，Tiffอาจ是更好的选择，但这取决于具体情况
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var imageBytes = ms.ToArray();
                        using (var img = Pix.LoadFromMemory(imageBytes))
                        {
                            using (var page = _engine.Process(img))
                            {
                                return page.GetText()?.Trim() ?? string.Empty;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 可以记录更详细的错误信息
                    System.Diagnostics.Debug.WriteLine($"Tesseract OCR 识别失败: {ex.ToString()}");
                    throw new Exception($"Tesseract OCR识别失败: {ex.Message}", ex);
                }
            });
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new ArgumentException("Tesseract语言代码不能为空。", nameof(languageCode));

            // 检查语言文件是否存在
            string targetLangFile = Path.Combine(GetTessDataPath(), $"{languageCode}.traineddata");
            if (!File.Exists(targetLangFile))
            {
                throw new FileNotFoundException($"Tesseract语言文件 '{languageCode}.traineddata' 未在 '{GetTessDataPath()}' 中找到。无法设置语言。", targetLangFile);
            }

            if (_currentLanguage == languageCode && _engine != null)
            {
                return; // 语言未改变，引擎已存在
            }

            // 释放旧引擎（如果有）
            _engine?.Dispose();
            _engine = null;

            try
            {
                // TesseractEngine 的构造函数可以接受多种参数
                // EngineMode.Default 通常是 TesseractOnly 或 LstmOnly，取决于编译选项和版本
                // EngineMode.TesseractAndLstm 通常更慢但可能更准确 (在5.x中，Default可能等同于LstmOnly)
                _engine = new TesseractEngine(GetTessDataPath(), languageCode, EngineMode.Default);
                _currentLanguage = languageCode;
            }
            catch (Exception ex)
            {
                // 详细记录初始化失败的原因
                System.Diagnostics.Debug.WriteLine($"初始化Tesseract OCR引擎 (语言: {languageCode}) 失败: {ex.ToString()}");
                throw new InvalidOperationException($"初始化Tesseract OCR引擎 (语言: {languageCode}) 失败: {ex.Message}。请检查tessdata路径和VC++ Redistributable依赖。", ex);
            }
        }

        public IEnumerable<string> GetSupportedLanguages()
        {
            if (Directory.Exists(GetTessDataPath()))
            {
                try
                {
                    return Directory.EnumerateFiles(GetTessDataPath(), "*.traineddata")
                                    .Select(filePath => Path.GetFileNameWithoutExtension(filePath))
                                    .Where(lang => !string.IsNullOrEmpty(lang)) // 过滤掉可能的空文件名
                                    .ToList(); //ToList确保立即执行
                }
                catch (Exception ex)
                {
                     System.Diagnostics.Debug.WriteLine($"扫描tessdata目录失败: {ex.Message}");
                     return new List<string> { "eng" }; // 发生错误时回退
                }
            }
            // 如果目录不存在，也返回一个默认列表，因为构造函数会抛出错误
            return new List<string> { "eng" }; 
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_engine != null)
                {
                    _engine.Dispose();
                    _engine = null;
                }
            }
        }

        // Finalizer in case Dispose is not called
        ~TesseractOCREngine()
        {
            Dispose(false);
        }
    }
}