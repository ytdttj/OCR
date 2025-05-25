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
        /// 检查并提取所有需要的资源文件（同步版本）
        /// </summary>
        public bool EnsureResourcesExtractedSync()
        {
            try
            {
                // 创建用户数据目录
                Directory.CreateDirectory(_userDataPath);
                
                // 创建调试日志文件
                string logFile = Path.Combine(_userDataPath, "extraction_log.txt");
                File.WriteAllText(logFile, $"[{DateTime.Now}] 开始资源提取过程（同步版本）\n");
                
                // 检查是否需要提取资源
                if (ShouldExtractResources())
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now}] 需要提取资源\n");
                    ExtractAllResourcesSync();
                    UpdateVersionFile();
                    File.AppendAllText(logFile, $"[{DateTime.Now}] 资源提取完成\n");
                    return true;
                }
                
                File.AppendAllText(logFile, $"[{DateTime.Now}] 无需提取资源（版本匹配）\n");
                return false;
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_userDataPath, "extraction_error.txt");
                File.WriteAllText(errorLog, $"[{DateTime.Now}] 资源提取失败: {ex.Message}\n{ex.StackTrace}");
                Console.WriteLine($"资源提取失败: {ex.Message}");
                return false;
            }
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

        /// <summary>
        /// 检查是否为首次运行或需要重新提取资源
        /// </summary>
        public bool IsFirstRunOrNeedsExtraction()
        {
            return ShouldExtractResources();
        }

        /// <summary>
        /// 创建静默重启标记文件
        /// </summary>
        public void CreateSilentRestartMarker()
        {
            try
            {
                string markerFile = Path.Combine(_userDataPath, "silent_restart.marker");
                File.WriteAllText(markerFile, DateTime.Now.ToString());
                Console.WriteLine("静默重启标记已创建");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建静默重启标记失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查静默重启标记是否存在
        /// </summary>
        public bool HasSilentRestartMarker()
        {
            try
            {
                string markerFile = Path.Combine(_userDataPath, "silent_restart.marker");
                return File.Exists(markerFile);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清除静默重启标记
        /// </summary>
        public void ClearSilentRestartMarker()
        {
            try
            {
                string markerFile = Path.Combine(_userDataPath, "silent_restart.marker");
                if (File.Exists(markerFile))
                {
                    File.Delete(markerFile);
                    Console.WriteLine("静默重启标记已清除");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清除静默重启标记失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证资源提取是否成功完成
        /// </summary>
        public bool VerifyResourcesExtracted()
        {
            try
            {
                // 检查关键目录是否存在
                string paddleOcrPath = Path.Combine(_userDataPath, "Models", "PaddleOCR");
                string tessdataPath = Path.Combine(_userDataPath, "tessdata");
                
                if (!Directory.Exists(paddleOcrPath) || !Directory.Exists(tessdataPath))
                {
                    Console.WriteLine("关键目录不存在，资源提取未完成");
                    return false;
                }

                // 检查版本文件是否存在
                string versionFile = Path.Combine(_userDataPath, "app_version.txt");
                if (!File.Exists(versionFile))
                {
                    Console.WriteLine("版本文件不存在，资源提取未完成");
                    return false;
                }

                // 检查是否有实际的资源文件
                var paddleFiles = Directory.GetFiles(paddleOcrPath, "*", SearchOption.AllDirectories);
                var tessFiles = Directory.GetFiles(tessdataPath, "*", SearchOption.AllDirectories);
                
                if (paddleFiles.Length == 0 || tessFiles.Length == 0)
                {
                    Console.WriteLine("资源文件为空，资源提取未完成");
                    return false;
                }

                Console.WriteLine($"资源验证成功: PaddleOCR文件数={paddleFiles.Length}, Tesseract文件数={tessFiles.Length}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"验证资源提取失败: {ex.Message}");
                return false;
            }
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

        /// <summary>
        /// 提取所有资源（同步版本）
        /// </summary>
        private void ExtractAllResourcesSync()
        {
            string logFile = Path.Combine(_userDataPath, "extraction_log.txt");
            
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                
                File.AppendAllText(logFile, $"[{DateTime.Now}] 开始提取资源（同步版本）\n");
                File.AppendAllText(logFile, $"[{DateTime.Now}] 程序集位置: {assembly.Location}\n");
                File.AppendAllText(logFile, $"[{DateTime.Now}] 是否为单文件: {string.IsNullOrEmpty(assembly.Location)}\n");
                File.AppendAllText(logFile, $"[{DateTime.Now}] 总共找到 {resourceNames.Length} 个嵌入资源\n");
                
                Console.WriteLine($"总共找到 {resourceNames.Length} 个嵌入资源");

                if (resourceNames.Length == 0)
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now}] 警告：没有找到任何嵌入资源！尝试备用方法...\n");
                    Console.WriteLine("警告：没有找到任何嵌入资源！尝试备用方法...");
                    
                    // 单文件发布环境的备用方法：尝试已知的资源名称
                    TryExtractKnownResourcesSync(assembly, logFile);
                    return;
                }

                int extractedCount = 0;
                foreach (var resourceName in resourceNames)
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now}] 发现资源: {resourceName}\n");
                    
                    // 更宽松的匹配条件
                    if (resourceName.Contains("PaddleOCR") || resourceName.Contains("tessdata"))
                    {
                        ExtractResourceGenericSync(assembly, resourceName);
                        extractedCount++;
                    }
                }

                File.AppendAllText(logFile, $"[{DateTime.Now}] 成功提取 {extractedCount} 个资源文件\n");
                Console.WriteLine($"成功提取 {extractedCount} 个资源文件");

                // 创建版本文件
                CreateVersionFilesSync();
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"[{DateTime.Now}] 提取资源时发生异常: {ex.Message}\n{ex.StackTrace}\n");
                Console.WriteLine($"提取资源时发生异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 单文件发布环境的备用资源提取方法（同步版本）
        /// </summary>
        private void TryExtractKnownResourcesSync(Assembly assembly, string logFile)
        {
            // 已知的资源名称模式（基于项目配置和实际目录结构）
            var knownResourcePatterns = new List<string>();
            
            // Tesseract 资源
            knownResourcePatterns.AddRange(new[]
            {
                "OCR.tessdata.chi_sim.traineddata",
                "OCR.tessdata.chi_tra.traineddata", 
                "OCR.tessdata.eng.traineddata",
                "OCR.tessdata.jpn.traineddata"
            });
            
            // PaddleOCR 字典文件
            knownResourcePatterns.AddRange(new[]
            {
                "OCR.Models.PaddleOCR.ppocr_keys_v1.txt",
                "OCR.Models.PaddleOCR.en_dict.txt"
            });
            
            // PaddleOCR 中文模型文件
            var chModelFiles = new[]
            {
                "inference.pdmodel", "inference.pdiparams", "pdiparams.info"
            };
            var modelTypes = new[] { "det_model", "rec_model", "cls_model" };
            
            foreach (var modelType in modelTypes)
            {
                foreach (var fileName in chModelFiles)
                {
                    knownResourcePatterns.Add($"OCR.Models.PaddleOCR.ch.{modelType}.{fileName}");
                }
            }
            
            // PaddleOCR 英文模型文件（如果存在）
            foreach (var modelType in modelTypes)
            {
                foreach (var fileName in chModelFiles)
                {
                    knownResourcePatterns.Add($"OCR.Models.PaddleOCR.en.{modelType}.{fileName}");
                }
            }

            int extractedCount = 0;
            foreach (var resourcePattern in knownResourcePatterns)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourcePattern);
                    if (stream != null)
                    {
                        ExtractResourceGenericSync(assembly, resourcePattern);
                        extractedCount++;
                        File.AppendAllText(logFile, $"[{DateTime.Now}] 成功提取已知资源: {resourcePattern}\n");
                    }
                    else
                    {
                        File.AppendAllText(logFile, $"[{DateTime.Now}] 未找到已知资源: {resourcePattern}\n");
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(logFile, $"[{DateTime.Now}] 提取已知资源失败 {resourcePattern}: {ex.Message}\n");
                }
            }

            File.AppendAllText(logFile, $"[{DateTime.Now}] 备用方法成功提取 {extractedCount} 个资源文件\n");
            Console.WriteLine($"备用方法成功提取 {extractedCount} 个资源文件");
            
            if (extractedCount > 0)
            {
                CreateVersionFilesSync();
            }
        }

        /// <summary>
        /// 提取所有资源（异步版本）
        /// </summary>
        private async Task ExtractAllResources()
        {
            string logFile = Path.Combine(_userDataPath, "extraction_log.txt");
            
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();
                
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 开始提取资源（异步版本）\n");
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 程序集位置: {assembly.Location}\n");
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 是否为单文件: {string.IsNullOrEmpty(assembly.Location)}\n");
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 总共找到 {resourceNames.Length} 个嵌入资源\n");
                
                Console.WriteLine($"总共找到 {resourceNames.Length} 个嵌入资源");

                if (resourceNames.Length == 0)
                {
                    await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 警告：没有找到任何嵌入资源！尝试备用方法...\n");
                    Console.WriteLine("警告：没有找到任何嵌入资源！尝试备用方法...");
                    
                    // 单文件发布环境的备用方法：尝试已知的资源名称
                    await TryExtractKnownResourcesAsync(assembly, logFile);
                    return;
                }

                int extractedCount = 0;
                foreach (var resourceName in resourceNames)
                {
                    await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 发现资源: {resourceName}\n");
                    
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
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 提取资源时发生异常: {ex.Message}\n{ex.StackTrace}\n");
                Console.WriteLine($"提取资源时发生异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 单文件发布环境的备用资源提取方法（异步版本）
        /// </summary>
        private async Task TryExtractKnownResourcesAsync(Assembly assembly, string logFile)
        {
            // 已知的资源名称模式（基于项目配置和实际目录结构）
            var knownResourcePatterns = new List<string>();
            
            // Tesseract 资源
            knownResourcePatterns.AddRange(new[]
            {
                "OCR.tessdata.chi_sim.traineddata",
                "OCR.tessdata.chi_tra.traineddata", 
                "OCR.tessdata.eng.traineddata",
                "OCR.tessdata.jpn.traineddata"
            });
            
            // PaddleOCR 字典文件
            knownResourcePatterns.AddRange(new[]
            {
                "OCR.Models.PaddleOCR.ppocr_keys_v1.txt",
                "OCR.Models.PaddleOCR.en_dict.txt"
            });
            
            // PaddleOCR 中文模型文件
            var chModelFiles = new[]
            {
                "inference.pdmodel", "inference.pdiparams", "pdiparams.info"
            };
            var modelTypes = new[] { "det_model", "rec_model", "cls_model" };
            
            foreach (var modelType in modelTypes)
            {
                foreach (var fileName in chModelFiles)
                {
                    knownResourcePatterns.Add($"OCR.Models.PaddleOCR.ch.{modelType}.{fileName}");
                }
            }
            
            // PaddleOCR 英文模型文件（如果存在）
            foreach (var modelType in modelTypes)
            {
                foreach (var fileName in chModelFiles)
                {
                    knownResourcePatterns.Add($"OCR.Models.PaddleOCR.en.{modelType}.{fileName}");
                }
            }

            int extractedCount = 0;
            foreach (var resourcePattern in knownResourcePatterns)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourcePattern);
                    if (stream != null)
                    {
                        await ExtractResourceGeneric(assembly, resourcePattern);
                        extractedCount++;
                        await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 成功提取已知资源: {resourcePattern}\n");
                    }
                    else
                    {
                        await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 未找到已知资源: {resourcePattern}\n");
                    }
                }
                catch (Exception ex)
                {
                    await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 提取已知资源失败 {resourcePattern}: {ex.Message}\n");
                }
            }

            await File.AppendAllTextAsync(logFile, $"[{DateTime.Now}] 备用方法成功提取 {extractedCount} 个资源文件\n");
            Console.WriteLine($"备用方法成功提取 {extractedCount} 个资源文件");
            
            if (extractedCount > 0)
            {
                await CreateVersionFiles();
            }
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

                string targetPath = GetTargetPath(resourceName);
                
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

        private void ExtractResourceGenericSync(Assembly assembly, string resourceName)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) 
                {
                    Console.WriteLine($"无法获取资源流: {resourceName}");
                    return;
                }

                string targetPath = GetTargetPath(resourceName);
                
                // 确保目标目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                // 提取文件
                using var fileStream = File.Create(targetPath);
                stream.CopyTo(fileStream);
                
                Console.WriteLine($"已提取: {resourceName} -> {targetPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"提取资源失败 {resourceName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据嵌入式资源名称确定目标路径
        /// </summary>
        private string GetTargetPath(string resourceName)
        {
            // 嵌入式资源名称格式：OCR.tessdata.chi_sim.traineddata 或 OCR.Models.PaddleOCR.ch.det_model.inference.pdmodel
            
            if (resourceName.Contains("tessdata"))
            {
                // Tesseract资源：OCR.tessdata.chi_sim.traineddata -> tessdata/chi_sim.traineddata
                string fileName = resourceName.Substring(resourceName.IndexOf("tessdata.") + 9);
                return Path.Combine(_userDataPath, "tessdata", fileName);
            }
            else if (resourceName.Contains("Models.PaddleOCR"))
            {
                // PaddleOCR资源：OCR.Models.PaddleOCR.ch.det_model.inference.pdmodel
                string relativePath = resourceName.Substring(resourceName.IndexOf("Models.PaddleOCR.") + 17);
                
                // 将点分隔的路径转换为实际路径
                // ch.det_model.inference.pdmodel -> ch/det_model/inference.pdmodel
                string[] parts = relativePath.Split('.');
                
                if (parts.Length >= 3)
                {
                    // 语言/模型类型/文件名
                    string language = parts[0]; // ch, en
                    string modelType = parts[1]; // det_model, rec_model, cls_model
                    string fileName = string.Join(".", parts.Skip(2)); // inference.pdmodel, inference.pdiparams等
                    
                    return Path.Combine(_userDataPath, "Models", "PaddleOCR", language, modelType, fileName);
                }
                else if (parts.Length == 1)
                {
                    // 字典文件：ppocr_keys_v1.txt, en_dict.txt
                    return Path.Combine(_userDataPath, "Models", "PaddleOCR", parts[0]);
                }
                else
                {
                    // 其他文件，直接放在PaddleOCR目录下
                    string fileName = string.Join(".", parts);
                    return Path.Combine(_userDataPath, "Models", "PaddleOCR", fileName);
                }
            }
            else
            {
                // 其他资源，提取最后的文件名
                string fileName = resourceName.Substring(resourceName.LastIndexOf('.') + 1);
                if (resourceName.Count(c => c == '.') > 1)
                {
                    // 如果有多个点，取最后两个部分作为文件名
                    var parts = resourceName.Split('.');
                    if (parts.Length >= 2)
                    {
                        fileName = $"{parts[parts.Length - 2]}.{parts[parts.Length - 1]}";
                    }
                }
                return Path.Combine(_userDataPath, "unknown", fileName);
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

        private void CreateVersionFilesSync()
        {
            // Models/version.txt
            string modelsVersionPath = Path.Combine(_userDataPath, "Models", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(modelsVersionPath)!);
            File.WriteAllText(modelsVersionPath, _appVersion);

            // tessdata/version.txt
            string tessdataVersionPath = Path.Combine(_userDataPath, "tessdata", "version.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(tessdataVersionPath)!);
            File.WriteAllText(tessdataVersionPath, _appVersion);
        }

        private void UpdateVersionFile()
        {
            string versionFile = Path.Combine(_userDataPath, "app_version.txt");
            File.WriteAllText(versionFile, _appVersion);
        }
    }
} 