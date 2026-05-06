using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SportsRecorder.ViewModel
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
            string dataFolder = Path.Combine(basePath, "Data");

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            return dataFolder;
        }

        /// <summary>儲存資料</summary>
        /// <example>JsonStorageService.Save(Records, "training.json");</example>
        public static void Save(ObservableCollection<TrainingJournalItem> data, string fileName)
        {
            try
            {
                string folder = GetDataFolder();
                string fullPath = Path.Combine(folder, fileName);

                string json = JsonSerializer.Serialize(data, _options);
                File.WriteAllText(fullPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save error: {ex.Message}");
            }
        }

        /// <summary>讀取資料（檔案不存在回傳空</summary>
        /// <example>Records = JsonStorageService.Load("training.json");</example>
        public static ObservableCollection<TrainingJournalItem> Load(string fileName)
        {
            try
            {
                string folder = GetDataFolder();
                string fullPath = Path.Combine(folder, fileName);

                if (!File.Exists(fullPath))
                {
                    return new ObservableCollection<TrainingJournalItem>();
                }

                string json = File.ReadAllText(fullPath, Encoding.UTF8);

                var data = JsonSerializer.Deserialize<ObservableCollection<TrainingJournalItem>>(json, _options);

                return data ?? new ObservableCollection<TrainingJournalItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load error: {ex.Message}");
                return new ObservableCollection<TrainingJournalItem>();
            }
        }
    }
}
