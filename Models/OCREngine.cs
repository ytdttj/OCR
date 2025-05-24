using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace OCR.Models
{
    /// <summary>
    /// OCR引擎接口，定义OCR功能的核心方法
    /// </summary>
    public interface IOCREngine
    {
        /// <summary>
        /// 识别图像中的文本
        /// </summary>
        /// <param name="image">要进行OCR识别的图像</param>
        /// <returns>识别到的文本</returns>
        Task<string> RecognizeTextAsync(Bitmap image);
        
        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        /// <returns>支持的语言代码列表</returns>
        IEnumerable<string> GetSupportedLanguages();
        
        /// <summary>
        /// 设置OCR识别语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        void SetLanguage(string languageCode);
        
        /// <summary>
        /// 当前设置的识别语言
        /// </summary>
        string CurrentLanguage { get; }
        
        /// <summary>
        /// OCR引擎名称
        /// </summary>
        string EngineName { get; }
    }
} 