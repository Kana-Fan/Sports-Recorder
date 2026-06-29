using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SportsRecorder.Services
{
    public static class JsonStorageService
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            // 使用 JavaScriptEncoder.Create 允許中文 UTF-8 編碼
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true, // 美化 JSON
            PropertyNameCaseInsensitive = true
        };

        private static string GetDataFolder()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(basePath, "Assets", "Data");

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            return dataFolder;
        }

        /// <summary>儲存資料，失敗回傳 false</summary>
        /// <example>JsonStorageService.Save(Records, "training.json");</example>
        public static bool Save<T>(T data, string fileName)
        {
            try
            {
                string folder = GetDataFolder();
                string fullPath = Path.Combine(folder, fileName);

                string json = JsonSerializer.Serialize(data, _options);

                File.WriteAllText(fullPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>讀取資料（檔案不存在回傳空</summary>
        /// <example>Records = JsonStorageService.Load<T<T>>("training.json");</example>
        public static T Load<T>(string fileName)
        where T : new() // 保證 T 有無參數建構子
        {
            try
            {
                string folder = GetDataFolder();
                string fullPath = Path.Combine(folder, fileName);

                if (!File.Exists(fullPath))
                {
                    return new T();
                }

                string json = File.ReadAllText(fullPath);

                return JsonSerializer.Deserialize<T>(json, _options)
                       ?? new T();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Load] 讀取失敗: {ex.Message}");
                return new T();
            }
        }
    }
}
