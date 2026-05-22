using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;

namespace SportsRecorder.ViewModel
{
    public class DataManagementViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        private string OpenFileType = "";

        private ObservableCollection<TrainingJournalItem> NewRecords;

        // 選擇匯入的檔案位置
        private string _fileName = "請選擇檔案";
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        // Commands
        public ICommand OpenFileCommand { get; }
        public ICommand UpLoadFileCommand { get; }
        public ICommand DownLoadFileCommand { get; }

        public DataManagementViewModel()
        {
            _dialogService = new DialogService();

            // Commands
            OpenFileCommand = new RelayCommand(() =>
            {
                // 建立一個 OpenFileDialog 實例
                OpenFileDialog openFileDialog = new OpenFileDialog();

                // 設定篩選器 (Filter) - 格式為: "名稱|*.副檔名"
                openFileDialog.Filter = "JSON檔案 (*.json)|*.json|Excel檔案 (*.xlsx)|*.xlsx";

                // 如果使用者選擇了檔案並按下「確定」
                if (openFileDialog.ShowDialog() == true)
                {
                    // 取得選擇的檔案路徑
                    FileName = openFileDialog.FileName;

                    OpenFileType = openFileDialog.FilterIndex == 1 ? ".json" : ".xlsx";
                }
            });

            UpLoadFileCommand = new RelayCommand(ExecuteUploadFile);

            DownLoadFileCommand = new RelayCommand(ExecuteDownloadFile);
        }

        private void ExecuteUploadFile()
        {
            if (OpenFileType == "")
            {
                _dialogService.ShowMessage("請先選擇檔案");
                return;
            }

            if (OpenFileType == ".json")
            {
                NewRecords = LoadJsonFile(FileName);
            }

            else // ".xlsx"
            {
                LoadExcelFile(FileName);
            }

            if (NewRecords == null || NewRecords.Count == 0)
            {
                Clean();
                return;
            }

            BackupAndWriteIn();
            Clean();
        }

        private void Clean()
        {
            // 清空
            NewRecords = new ObservableCollection<TrainingJournalItem>();
            OpenFileType = "";
            FileName = "請選擇檔案";
        }

        private void BackupAndWriteIn()
        {
            string recordFileName = "records.json";
            string backupFileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json";

            // 先把本來的資料做備份
            ObservableCollection<TrainingJournalItem> oldRecords = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>(recordFileName);

            if (oldRecords != null && oldRecords.Count > 0 && JsonStorageService.Save(oldRecords, backupFileName))
            {
                _dialogService.ShowMessage("備份成功~");

                if (_dialogService.Confirm("確定要匯入紀錄嗎？"))
                {
                    // 把新的資料存進去
                    if (JsonStorageService.Save(NewRecords, recordFileName))
                    {
                        _dialogService.ShowMessage("匯入成功~");
                    }

                    else
                    {
                        _dialogService.ShowMessage("匯入失敗，請稍後在試~");
                    }
                }
            }

            else
            {
                _dialogService.ShowMessage("備份失敗，請稍後在試~");
            }
        }

        private void ExecuteDownloadFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "JSON檔案 (*.json)|*.json|Excel檔案 (*.xlsx)|*.xlsx";

            if (saveFileDialog.ShowDialog() == false)
            {
                return;
            }

            // 取得內容並存檔
            ObservableCollection<TrainingJournalItem> records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>("records.json");

            if (records == null || records.Count == 0)
            {
                _dialogService.ShowMessage("沒有可以匯出的內容~");
            }

            // .json 檔案
            else if (saveFileDialog.FilterIndex == 1)
            {
                if (ExportToJson(records, saveFileDialog.FileName))
                {
                    _dialogService.ShowMessage("Json檔案匯出成功~");
                }

                else
                {
                    _dialogService.ShowMessage("Json檔案匯出失敗~");
                }
            }
            // .xlsx 檔案
            else if (saveFileDialog.FilterIndex == 2)
            {
                if (ExportToExcel(records, saveFileDialog.FileName))
                {
                    _dialogService.ShowMessage("Excel檔案匯出成功~");
                }

                else
                {
                    _dialogService.ShowMessage("Excel檔案匯出失敗~");
                }
            }
        }

        /// <summary>專門匯入Json（檔案不存在顯示錯誤訊息)</summary>
        private ObservableCollection<TrainingJournalItem> LoadJsonFile(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                AllowTrailingCommas = false,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            };

            try
            {
                // 檔案不存在
                if (!File.Exists(filePath))
                {
                    _dialogService.ShowMessage("檔案不存在!!!");
                    return new ObservableCollection<TrainingJournalItem>();
                }

                string json = File.ReadAllText(filePath);

                var data = JsonSerializer.Deserialize<ObservableCollection<TrainingJournalItem>>(json, options);

                if (data == null)
                {
                    _dialogService.ShowMessage("內容為空!!!");
                    return new ObservableCollection<TrainingJournalItem>();
                }

                foreach (var item in data)
                {
                    if (item.Date == default)
                    {
                        throw new Exception("JSON Date格式錯誤");
                    }

                    if (item.Cardios.Count == 0 && item.Strengths.Count == 0)
                    {
                        throw new Exception("JSON 內容格式錯誤");
                    }
                }

                return data;
            }
            catch (JsonException ex)
            {
                // JSON 格式錯誤
                _dialogService.ShowMessage("JSON 格式不符");

                return new ObservableCollection<TrainingJournalItem>();
            }
            catch (Exception ex)
            {
                // 其他錯誤
                _dialogService.ShowMessage($"發生錯誤：\n{ex.Message}");

                return new ObservableCollection<TrainingJournalItem>();
            }
        }

        /// <summary>專門匯入Excel（檔案不存在顯示錯誤訊息)</summary>
        private void LoadExcelFile(string filePath)
        {
            try
            {
                NewRecords = ImportFromExcel(filePath);
            }
            catch (FileNotFoundException ex)
            {
                // 檔案不存在
                _dialogService.ShowMessage($"{ex.Message}");
                NewRecords = new ObservableCollection<TrainingJournalItem>();
            }
            catch (ArgumentException ex)
            {
                // 副檔名錯誤
                _dialogService.ShowMessage($"{ex.Message}");
                NewRecords = new ObservableCollection<TrainingJournalItem>();
            }
            catch (InvalidDataException ex)
            {
                // 內容格式不正確，含有精確的行列資訊
                _dialogService.ShowMessage($"格式錯誤：{ex.Message}");
                NewRecords = new ObservableCollection<TrainingJournalItem>();
            }
            catch (IOException ex)
            {
                // 其他讀檔錯誤（檔案被佔用、權限不足等）
                _dialogService.ShowMessage($"讀取失敗：{ex.Message}");
                NewRecords = new ObservableCollection<TrainingJournalItem>();
            }
        }

        private ObservableCollection<TrainingJournalItem> ImportFromExcel(string filePath)
        {
            // ====== 檔案存在檢查 ======
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"找不到檔案：{filePath}");

            if (Path.GetExtension(filePath).ToLower() != ".xlsx")
                throw new ArgumentException($"檔案格式不正確，請提供 .xlsx 檔案：{filePath}");

            try
            {
                using var wb = new XLWorkbook(filePath);

                // ====== 工作表存在檢查 ======
                if (!wb.TryGetWorksheet("Cardio", out var cardioSheet))
                {
                    throw new InvalidDataException("Excel 中找不到 'Cardio' 工作表");
                }

                if (!wb.TryGetWorksheet("Strength", out var strengthSheet))
                {
                    throw new InvalidDataException("Excel 中找不到 'Strength' 工作表");
                }

                // ====== 讀取 Cardio ======
                var cardioMap = new Dictionary<string, List<Cardio>>();

                // 跳過標題
                foreach (var row in cardioSheet.RowsUsed().Skip(1))
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var date = ParseDate(row.Cell(1), "Cardio", rowNum, "Date");
                        cardioMap.TryAdd(date, new List<Cardio>());
                        cardioMap[date].Add(new Cardio
                        {
                            Name = ParseString(row.Cell(2), "Cardio", rowNum, "Name"),
                            DistanceKm = ParseDouble(row.Cell(3), "Cardio", rowNum, "DistanceKm"),
                            Times = ParseInt(row.Cell(4), "Cardio", rowNum, "Times"),
                            DurationMin = ParseInt(row.Cell(5), "Cardio", rowNum, "DurationMin"),
                        });
                    }
                    catch (InvalidDataException)
                    {
                        throw; // 直接往上拋，保留原本的錯誤訊息
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException($"[Cardio] 第 {rowNum} 行資料解析失敗：{ex.Message}");
                    }
                }

                // ====== 讀取 Strength ======
                var strengthMap = new Dictionary<string, List<Strength>>();

                // 跳過標題
                foreach (var row in strengthSheet.RowsUsed().Skip(1))
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var date = ParseDate(row.Cell(1), "Strength", rowNum, "Date");
                        strengthMap.TryAdd(date, new List<Strength>());
                        strengthMap[date].Add(new Strength
                        {
                            BodyPart = ParseString(row.Cell(2), "Strength", rowNum, "BodyPart"),
                            Name = ParseString(row.Cell(3), "Strength", rowNum, "Name"),
                            WeightKg = ParseDouble(row.Cell(4), "Strength", rowNum, "WeightKg"),
                            Sets = ParseInt(row.Cell(5), "Strength", rowNum, "Sets"),
                            Reps = ParseInt(row.Cell(6), "Strength", rowNum, "Reps"),
                        });
                    }
                    catch (InvalidDataException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException($"[Strength] 第 {rowNum} 行資料解析失敗：{ex.Message}");
                    }
                }

                // ====== 合併成 TrainingJournalItem ======
                var allDates = cardioMap.Keys.Union(strengthMap.Keys).OrderBy(d => d);
                var records = new ObservableCollection<TrainingJournalItem>();

                foreach (var date in allDates)
                {
                    var cardios = cardioMap.GetValueOrDefault(date) ?? new List<Cardio>();
                    var strengths = strengthMap.GetValueOrDefault(date) ?? new List<Strength>();

                    records.Add(new TrainingJournalItem
                    {
                        Date = DateTime.Parse(date),
                        CardiosTotalDuration = cardios.Sum(c => c.DurationMin),
                        Cardios = new ObservableCollection<Cardio>(cardios),
                        Strengths = new ObservableCollection<Strength>(strengths),
                    });
                }

                return records;
            }
            catch (InvalidDataException)
            {
                throw; // 格式錯誤直接往上拋
            }
            catch (Exception ex)
            {
                throw new IOException($"讀取 Excel 檔案時發生錯誤：{ex.Message}", ex);
            }
        }

        // ====== 輔助 Parse 方法 ======
        private string ParseDate(IXLCell cell, string sheet, int row, string col)
        {
            try
            {
                if (cell.DataType == XLDataType.DateTime)
                    return cell.GetDateTime().ToString("yyyy-MM-dd");

                var str = cell.GetString().Trim();
                if (DateTime.TryParse(str, out var dt))
                    return dt.ToString("yyyy-MM-dd");

                throw new InvalidDataException($"[{sheet}] 第 {row} 行 '{col}' 不是有效的日期：'{str}'");
            }
            catch (InvalidDataException) { throw; }
            catch { throw new InvalidDataException($"[{sheet}] 第 {row} 行 '{col}' 日期格式錯誤"); }
        }

        private string ParseString(IXLCell cell, string sheet, int row, string col)
        {
            var val = cell.GetString().Trim();
            if (string.IsNullOrEmpty(val))
                throw new InvalidDataException($"[{sheet}] 第 {row} 行 '{col}' 不可為空白");
            return val;
        }

        private double ParseDouble(IXLCell cell, string sheet, int row, string col)
        {
            try { return cell.GetDouble(); }
            catch { throw new InvalidDataException($"[{sheet}] 第 {row} 行 '{col}' 必須是數字，目前值：'{cell.GetString()}'"); }
        }

        private int ParseInt(IXLCell cell, string sheet, int row, string col)
        {
            try
            {
                var val = (int)cell.GetDouble();
                if (val < 0)
                    throw new InvalidDataException($"[{sheet}] 第 {row} 行 '{col}' 不可為負數：{val}");
                return val;
            }
            catch (InvalidDataException) { throw; }
            catch { throw new InvalidDataException($"[{sheet}] 第 {row} 行 '{col}' 必須是整數，目前值：'{cell.GetString()}'"); }
        }

        /// <summary>儲存Json，失敗回傳 false</summary>
        private bool ExportToJson<T>(T data, string fullPath)
        {
            var options = new JsonSerializerOptions
            {
                // 使用 JavaScriptEncoder.Create 允許中文 UTF-8 編碼
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true, // 美化 JSON
                PropertyNameCaseInsensitive = true
            };

            try
            {
                string json = JsonSerializer.Serialize(data, options);

                File.WriteAllText(fullPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>匯出Excel</summary>
        private bool ExportToExcel(ObservableCollection<TrainingJournalItem> records, string filePath)
        {
            try
            {
                using var wb = new XLWorkbook();

                // ====== Cardio 工作表 ======
                var cardioSheet = wb.Worksheets.Add("Cardio");

                // Header
                string[] cardioHeaders = ["日期", "運動名稱", "運動距離(km)", "運動次數", "運動時間(min)"];
                SetHeaders(cardioSheet, cardioHeaders, "#4472C4");

                int cardioRow = 2;
                foreach (var record in records)
                {
                    foreach (var c in record.Cardios)
                    {
                        cardioSheet.Cell(cardioRow, 1).Value = record.Date.ToString("yyyy-MM-dd");
                        cardioSheet.Cell(cardioRow, 2).Value = c.Name;
                        cardioSheet.Cell(cardioRow, 3).Value = c.DistanceKm;
                        cardioSheet.Cell(cardioRow, 4).Value = c.Times;
                        cardioSheet.Cell(cardioRow, 5).Value = c.DurationMin;
                        cardioRow++;
                    }
                }

                SetColumnWidths(cardioSheet, [11, 14, 14, 10, 14]);

                // ====== Strength 工作表 ======
                var strengthSheet = wb.Worksheets.Add("Strength");

                string[] strengthHeaders = ["日期", "訓練部位", "訓練名稱", "重量(kg)", "訓練組數", "訓練次數"];
                SetHeaders(strengthSheet, strengthHeaders, "#70AD47");

                int strengthRow = 2;
                foreach (var record in records)
                {
                    foreach (var s in record.Strengths)
                    {
                        strengthSheet.Cell(strengthRow, 1).Value = record.Date.ToString("yyyy-MM-dd");
                        strengthSheet.Cell(strengthRow, 2).Value = s.BodyPart;
                        strengthSheet.Cell(strengthRow, 3).Value = s.Name;
                        strengthSheet.Cell(strengthRow, 4).Value = s.WeightKg;
                        strengthSheet.Cell(strengthRow, 5).Value = s.Sets;
                        strengthSheet.Cell(strengthRow, 6).Value = s.Reps;
                        strengthRow++;
                    }
                }

                SetColumnWidths(strengthSheet, [11, 10, 11, 10, 10, 10]);

                wb.SaveAs(filePath);

                return true;
            }
            catch (IOException ex)
            {
                // 檔案被佔用（例如 Excel 已開啟該檔案）
                _dialogService.ShowMessage($"檔案存取失敗: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                // 沒有寫入權限
                _dialogService.ShowMessage($"權限不足: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // 其他未預期的錯誤
                _dialogService.ShowMessage($"匯出失敗: {ex.Message}");
                return false;
            }
        }

        private void SetHeaders(IXLWorksheet sheet, string[] headers, string hexColor)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(hexColor);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private void SetColumnWidths(IXLWorksheet sheet, double[] widths)
        {
            for (int i = 0; i < widths.Length; i++)
            {
                sheet.Column(i + 1).Width = widths[i];
            }
        }
    }
}
