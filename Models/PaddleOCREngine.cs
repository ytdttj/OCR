using OCR.Models;
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
        private readonly string _modelBasePath;

        public string EngineName => "PaddleOCR";
        public string CurrentLanguage => _currentLanguage;

        public PaddleOCREngine(string language, AppSettings? settings = null)
        {
            _settings = settings ?? AppSettings.Load();
            _currentLanguage = language ?? "ch";
            
            // 获取模型文件路径
            _modelBasePath = GetModelBasePath();
            
            // 初始化PaddleOCR引擎
            InitializePaddleOCR();
        }

        private string GetModelBasePath()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            return Path.Combine(exePath, "Models", "PaddleOCR");
        }

        private void InitializePaddleOCR()
        {
            try
            {
                // 使用默认配置（内置轻量级中英文V3模型）
                OCRModelConfig config = null;
                
                // 如果需要使用自定义模型，可以这样配置：
                // OCRModelConfig config = new OCRModelConfig();
                // config.det_infer = Path.Combine(_modelBasePath, "ch_PP-OCRv4_det_server_infer");
                // config.cls_infer = Path.Combine(_modelBasePath, "ch_ppocr_mobile_v2.0_cls_infer");
                // config.rec_infer = Path.Combine(_modelBasePath, "ch_PP-OCRv4_rec_server_infer");
                // config.keys = Path.Combine(_modelBasePath, "ppocr_keys.txt");

                // OCR参数配置
                OCRParameter ocrParameter = new OCRParameter();
                ocrParameter.cpu_math_library_num_threads = 10;
                ocrParameter.enable_mkldnn = true;
                ocrParameter.cls = false;
                ocrParameter.det = true;
                ocrParameter.use_angle_cls = false;
                ocrParameter.det_db_score_mode = true;

                // 创建PaddleOCR引擎
                _paddleEngine = new PaddleOCRSharp.PaddleOCREngine(config, ocrParameter);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize PaddleOCR engine: {ex.Message}", ex);
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