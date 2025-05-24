using OCR.Models;
using OCR.Services;
using PaddleOCRSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace OCR.Models
{
    public class PaddleOCREngine : IOCREngine, IDisposable
    {
        private readonly AppSettings _settings;
        private string _currentLanguage;
        private bool _disposed = false;
        private PaddleOCRSharp.PaddleOCREngine _paddleEngine;
        private readonly ResourceExtractionService _resourceService;

        public string EngineName => "PaddleOCR";
        public string CurrentLanguage => _currentLanguage;

        public PaddleOCREngine(string language, AppSettings? settings = null)
        {
            _settings = settings ?? AppSettings.Load();
            _currentLanguage = language ?? "ch";
            _resourceService = new ResourceExtractionService();
            
            InitializePaddleOCR();
        }

        private void InitializePaddleOCR()
        {
            try
            {
                // 获取用户数据目录中的模型路径
                string modelPath = _settings.GetPaddleOcrModelPath();
                
                // 检查模型文件是否存在
                if (!Directory.Exists(modelPath))
                {
                    throw new DirectoryNotFoundException($"PaddleOCR模型目录不存在: {modelPath}");
                }

                // 构建模型配置
                var config = new OCRModelConfig
                {
                    det_infer = Path.Combine(modelPath, "det_model"),
                    rec_infer = Path.Combine(modelPath, "rec_model"),
                    cls_infer = Path.Combine(modelPath, "cls_model"),
                    keys = _resourceService.GetUserModelPath(
                        _currentLanguage == "en" 
                            ? "Models/PaddleOCR/en_dict.txt" 
                            : "Models/PaddleOCR/ppocr_keys_v1.txt"
                    )
                };

                // 验证必需的模型文件
                if (!Directory.Exists(config.det_infer))
                    throw new DirectoryNotFoundException($"检测模型目录不存在: {config.det_infer}");
                if (!Directory.Exists(config.rec_infer))
                    throw new DirectoryNotFoundException($"识别模型目录不存在: {config.rec_infer}");

                // 初始化PaddleOCR引擎
                _paddleEngine = new PaddleOCRSharp.PaddleOCREngine(config, new OCRParameter());
                
                Console.WriteLine($"PaddleOCR引擎初始化成功，语言: {_currentLanguage}，模型路径: {modelPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PaddleOCR初始化失败: {ex.Message}");
                throw;
            }
        }

        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PaddleOCREngine));

            try
            {
                // PaddleOCRSharp的DetectText方法是同步的，我们在Task.Run中运行它
                return await Task.Run(() =>
                {
                    OCRResult result = _paddleEngine.DetectText(image);
                    return result?.Text ?? string.Empty;
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"OCR recognition failed: {ex.Message}", ex);
            }
        }

        public IEnumerable<string> GetSupportedLanguages()
        {
            return new[] { "ch", "en" };
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                throw new ArgumentException("Language code cannot be null or empty", nameof(languageCode));

            var supportedLanguages = GetSupportedLanguages();
            if (!supportedLanguages.Contains(languageCode))
                throw new ArgumentException($"Unsupported language: {languageCode}", nameof(languageCode));

            _currentLanguage = languageCode;
            
            // 注意：PaddleOCRSharp的语言切换需要重新初始化引擎
            // 这里可以根据需要重新初始化
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _paddleEngine?.Dispose();
                _disposed = true;
            }
        }
    }
} 