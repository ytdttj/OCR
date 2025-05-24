using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace OCR.Services
{
    public class ResourceExtractionService
    {
        private readonly string _userDataPath;
        private readonly string _appVersion;
        
        public ResourceExtractionService()
        {
            // 用户数据目录：C:\Users\{USERNAME}\AppData\Local\YTOCR\
            _userDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YTOCR"
            );
            
            // 获取当前程序版本
            _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        }

        /// <summary>
        /// 检查并提取所有需要的资源文件
        /// </summary>
        public async Task<bool> EnsureResourcesExtracted()
        {
            try
            {
                // 创建用户数据目录
                Directory.CreateDirectory(_userDataPath);
                
                // 创建调试日志文件
                string logFile = Path.Combine(_userDataPath, "extraction_log.txt");
                await File.WriteAllTextAsync(logFile, $"[{DateTime.Now}] 开始资源提取过程\n");
                
                // 检查是否需要提取资源
                if (ShouldExtractResources())
                {
                    await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 需要提取资源\n");
                    await ExtractAllResources();
                    UpdateVersionFile();
                    await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 资源提取完成\n");
                    return true;
                }
                
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 无需提取资源（版本匹配）\n");
                return false;
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_userDataPath, "extraction_error.txt");
                await File.WriteAllTextAsync(errorLog, $"[{DateTime.Now}] 资源提取失败: {ex.Message}\n{ex.StackTrace}");
                Console.WriteLine($"资源提取失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取用户数据目录中的模型路径
        /// </summary>
        public string GetUserModelPath(string relativePath)
        {
            return Path.Combine(_userDataPath, relativePath);
        }

        /// <summary>
        /// 获取PaddleOCR模型路径
        /// </summary>
        public string GetPaddleOCRModelPath(string language)
        {
            return Path.Combine(_userDataPath, "Models", "PaddleOCR", language);
        }

        /// <summary>
        /// 获取Tesseract数据路径
        /// </summary>
        public string GetTesseractDataPath()
        {
            return Path.Combine(_userDataPath, "tessdata");
        }

        private bool ShouldExtractResources()
        {
            // 检查版本文件
            string versionFile = Path.Combine(_userDataPath, "app_version.txt");
            if (!File.Exists(versionFile))
                return true;

            try
            {
                string existingVersion = File.ReadAllText(versionFile).Trim();
                return existingVersion != _appVersion;
            }
            catch
            {
                return true;
            }
        }

        private async Task ExtractAllResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            string logFile = Path.Combine(_userDataPath, "extraction_log.txt");
            
            // 调试：输出所有嵌入的资源名称
            await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] === 嵌入的资源列表 ===\n");
            foreach (var name in resourceNames)
            {
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] Resource: {name}\n");
                Console.WriteLine($"Resource: {name}");
            }
            await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 总共找到 {resourceNames.Length} 个嵌入资源\n");
            Console.WriteLine($"总共找到 {resourceNames.Length} 个嵌入资源");

            if (resourceNames.Length == 0)
            {
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 警告：没有找到任何嵌入资源！\n");
                Console.WriteLine("警告：没有找到任何嵌入资源！");
                return;
            }

            int extractedCount = 0;
            foreach (var resourceName in resourceNames)
            {
                // 更宽松的匹配条件
                if (resourceName.Contains("PaddleOCR") || resourceName.Contains("tessdata"))
                {
                    await ExtractResourceGeneric(assembly, resourceName);
                    extractedCount++;
                }
            }

            await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 成功提取 {extractedCount} 个资源文件\n");
            Console.WriteLine($"成功提取 {extractedCount} 个资源文件");

            // 创建版本文件
            await CreateVersionFiles();
        }

        private async Task ExtractResourceGeneric(Assembly assembly, string resourceName)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) 
                {
                    Console.WriteLine($"无法获取资源流: {resourceName}");
                    return;
                }

                // 简化的文件名提取
                string fileName = resourceName;
                int lastDotIndex = resourceName.LastIndexOf('.');
                int secondLastDotIndex = resourceName.LastIndexOf('.', lastDotIndex - 1);
                
                if (secondLastDotIndex > 0)
                {
                    fileName = resourceName.Substring(secondLastDotIndex + 1);
                }

                string targetPath;
                
                // 根据资源类型决定目标路径
                if (resourceName.Contains("tessdata"))
                {
                    targetPath = Path.Combine(_userDataPath, "tessdata", fileName);
                }
                else if (resourceName.Contains("PaddleOCR"))
                {
                    // 简化的PaddleOCR文件处理
                    if (resourceName.Contains(".ch.") || resourceName.Contains(".en."))
                    {
                        // 模型文件
                        string lang = resourceName.Contains(".ch.") ? "ch" : "en";
                        string modelType = "unknown";
                        
                        if (resourceName.Contains("det_model")) modelType = "det_model";
                        else if (resourceName.Contains("rec_model")) modelType = "rec_model";
                        else if (resourceName.Contains("cls_model")) modelType = "cls_model";
                        
                        targetPath = Path.Combine(_userDataPath, "Models", "PaddleOCR", lang, modelType, fileName);
                    }
                    else
                    {
                        // 字典文件
                        targetPath = Path.Combine(_userDataPath, "Models", "PaddleOCR", fileName);
                    }
                }
                else
                {
                    // 其他文件
                    targetPath = Path.Combine(_userDataPath, "unknown", fileName);
                }

                // 确保目标目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                // 提取文件
                using var fileStream = File.Create(targetPath);
                await stream.CopyToAsync(fileStream);
                
                Console.WriteLine($"已提取: {resourceName} -> {targetPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取资源失败 {resourceName}: {ex.Message}");
            }
        }

        private async Task CreateVersionFiles()
        {
            // Models/version.txt
            string modelsVersionPath = Path.Combine(_userDataPath, "Models", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(modelsVersionPath)!);
            await File.WriteAllTextAsync(modelsVersionPath, _appVersion);

            // tessdata/version.txt
            string tessdataVersionPath = Path.Combine(_userDataPath, "tessdata", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(tessdataVersionPath)!);
            await File.WriteAllTextAsync(tessdataVersionPath, _appVersion);
        }

        private void UpdateVersionFile()
        {
            string versionFile = Path.Combine(_userDataPath, "app_version.txt");
            File.WriteAllText(versionFile, _appVersion);
        }
    }
} 