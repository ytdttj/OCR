using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization; // Required for JsonStringEnumConverter

namespace OCR.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OcrEngineType // 统一大小写
    {
        WindowsOCR,
        TesseractOCR,
        PaddleOCR // 启用PaddleOCR
    }

    public class HotkeySettings // 将快捷键相关设置提取到一个单独的类中，以便管理
    {
        public bool Ctrl { get; set; } = true;
        public bool Shift { get; set; } = true;
        public bool Alt { get; set; } = false;
        public string Key { get; set; } = "C"; // 确保 Key 是 string 类型
    }


    public class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory,
            "settings.json"); // 建议使用 .json 扩展名

        // Hotkey property
        public HotkeySettings Hotkey { get; set; } = new HotkeySettings();


        public string LanguageCode { get; set; } = "en";
        public string SavePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "ScreenshotOCR");
        public bool StartMinimized { get; set; } = false;
        public bool SaveHistory { get; set; } = true;
        public bool AutoCopyOcrResult { get; set; } = false;
        
        // 现有快捷键属性，将被上面的 HotkeySettings 替代，但在 Load 方法中可以考虑迁移
        public bool HotkeyCtrl { get; set; } = true;
        public bool HotkeyShift { get; set; } = true;
        public bool HotkeyAlt { get; set; } = false;
        public string HotkeyKey { get; set; } = "C";


        public OcrEngineType SelectedOcrEngine { get; set; } = OcrEngineType.WindowsOCR; // 统一大小写
        public string TesseractLanguage { get; set; } = "eng";

        // PaddleOCR相关设置
        public string PaddleOcrModelPath { get; set; } = "./Models/PaddleOCR/";
        public string PaddleOcrDevice { get; set; } = "CPU";
        public string PaddleOcrLanguage { get; set; } = "ch";
        public int PaddleOcrMaxSideLen { get; set; } = 960;
        public bool PaddleOcrUseGpu { get; set; } = false;
        public int PaddleOcrGpuMemory { get; set; } = 500;

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, options);

                    // 迁移旧的独立快捷键设置到 HotkeySettings 对象
                    if (settings != null)
                    {
                        // 如果 Hotkey 对象是默认的，并且旧的独立字段有值，则迁移
                        if (settings.Hotkey.Ctrl == new HotkeySettings().Ctrl && settings.HotkeyCtrl != new HotkeySettings().Ctrl)
                        {
                            settings.Hotkey.Ctrl = settings.HotkeyCtrl;
                        }
                        if (settings.Hotkey.Shift == new HotkeySettings().Shift && settings.HotkeyShift != new HotkeySettings().Shift)
                        {
                            settings.Hotkey.Shift = settings.HotkeyShift;
                        }
                        if (settings.Hotkey.Alt == new HotkeySettings().Alt && settings.HotkeyAlt != new HotkeySettings().Alt)
                        {
                            settings.Hotkey.Alt = settings.HotkeyAlt;
                        }
                        if (settings.Hotkey.Key == new HotkeySettings().Key && settings.HotkeyKey != new HotkeySettings().Key)
                        {
                           settings.Hotkey.Key = settings.HotkeyKey;
                        }
                    }
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载设置失败: {ex.Message}");
            }
            return new AppSettings();
        }
    }
}