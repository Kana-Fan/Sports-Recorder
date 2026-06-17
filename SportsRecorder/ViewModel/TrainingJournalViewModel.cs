using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class TrainingJournalViewModel : TrainingRecordsViewModel
    {
        // 部位與運動的對應字典
        public Dictionary<string, List<string>> StrengthTypes { get; set; }

        // 暴露給 ComboBox 用的選項清單
        public ObservableCollection<ExerciseTypeOption> ExerciseTypeOptions { get; } = new ObservableCollection<ExerciseTypeOption>
        {
            new ExerciseTypeOption("有氧運動", ExerciseType.Cardio,   "#FF43A047"),
            new ExerciseTypeOption("重量訓練", ExerciseType.Strength, "#EF5350"),
        };

        // 有氧運動的類型
        private List<String> _cardioTypes;
        public List<String> CardioTypes
        {
            get => _cardioTypes;
            set => SetProperty(ref _cardioTypes, value);
        }

        // 重訓的部位類型
        private List<String> _bodyTypes;
        public List<String> BodyTypes
        {
            get => _bodyTypes;
            set => SetProperty(ref _bodyTypes, value);
        }

        // 當前選擇日期
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (HasChange)
                {
                    _dialogService.ShowMessage("當前日期有未儲存的項目，請先儲存");
                }

                else
                {
                    SetProperty(ref _selectedDate, value);
                    LoadOrCreateLog(value); // 日期變更就重新載入
                }
            }
        }

        // 當前這天的資料
        private TrainingJournalItem _currentLog;
        public TrainingJournalItem CurrentLog
        {
            get => _currentLog;
            set
            {
                _currentLog = value;
                OnPropertyChanged(nameof(CurrentLog));
                OnPropertyChanged(nameof(IsDataEmpty));
            }
        }

        // 當前類型 (配合 ComboBox 還有表格變化)
        private ExerciseType _selectedType = ExerciseType.Cardio;
        public ExerciseType SelectedType
        {
            get => _selectedType;
            set
            {
                SetProperty(ref _selectedType, value);
                OnPropertyChanged(nameof(IsCardio));
                OnPropertyChanged(nameof(IsStrength));

                AddPropertyChanged();
            }
        }

        public bool IsCardio => SelectedType == ExerciseType.Cardio;
        public bool IsStrength => SelectedType == ExerciseType.Strength;

        private int _cardiosTotalDuration = 0;
        public int CardiosTotalDuration
        {
            get => _cardiosTotalDuration;
            set => SetProperty(ref _cardiosTotalDuration, value);
        }

        // 判斷集合是否為空或數量為 0
        public bool IsDataEmpty
        {
            get
            {
                return SelectedType == ExerciseType.Cardio ?
                    CurrentLog.Cardios.Count() == 0 : CurrentLog.Strengths.Count() == 0;
            }
        }

        // Commands
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }

        public TrainingJournalViewModel()
        {
            CardioTypes = GetCardioTypes();

            StrengthTypes = GetStrengthTypes();

            BodyTypes = StrengthTypes.Keys.ToList();

            LoadOrCreateLog(DateTime.Today);

            RefreshHasRecord();

            // 初始化時，為現有的所有項目訂閱事件
            foreach (var item in Records)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }

            // 監聽集合的變化（新增、刪除、排序）
            Records.CollectionChanged += (s, e) => UpdateDirtyState();

            // Commands
            AddEntryCommand = new RelayCommand(() =>
            {
                if (SelectedType == ExerciseType.Cardio)
                {
                    CurrentLog.Cardios.Add(new Cardio());
                    RecalculateTotalDuration();
                }

                else
                {
                    CurrentLog.Strengths.Add(new Strength());
                }

                AddPropertyChanged();
                UpdateDirtyState();
                OnPropertyChanged(nameof(IsDataEmpty));
            });

            DeleteEntryCommand = new RelayCommand<ExerciseEntryBase>(entry =>
            {
                if (_dialogService.Confirm("確定要刪除這筆紀錄嗎？", "確認刪除"))
                {
                    if (entry == null)
                    {
                        return;
                    }

                    if (entry is Cardio c)
                    {
                        CurrentLog.Cardios.Remove(c);
                    }

                    else if (entry is Strength s)
                    {
                        CurrentLog.Strengths.Remove(s);
                    }

                    UpdateDirtyState();
                    OnPropertyChanged(nameof(IsDataEmpty));
                }
            });

            SaveCommand = new RelayCommand(execute: ExecuteSave, canExecute: () => HasChange);
        }

        private void ExecuteSave()
        {
            // 先清理各記錄內的空項目
            foreach (var record in Records)
            {
                var emptyCardios = record.Cardios?
                    .Where(c => c.Name == "-選擇運動名稱-").ToList() ?? [];
                foreach (var c in emptyCardios)
                {
                    record.Cardios.Remove(c);
                }

                var emptyStrengths = record.Strengths?
                    .Where(s => s.BodyPart == "-選擇部位-" || s.Name == "-選擇訓練名稱-").ToList() ?? [];
                foreach (var s in emptyStrengths)
                {
                    record.Strengths.Remove(s);
                }
            }

            // 排序與刪除空行
            var sorted = Records
                .Where(x => (x.Cardios != null && x.Cardios.Count > 0) || (x.Strengths != null && x.Strengths.Count > 0))
                .OrderBy(x => x.Date)
                .ToList();

            Records.Clear();
            foreach (var r in sorted)
            {
                Records.Add(r);
            }

            // CurrentLog 可能已被清掉，重新載入
            LoadOrCreateLog(SelectedDate);

            if (JsonStorageService.Save(Records, RecordsFileName))
            {
                OnPropertyChanged(nameof(IsDataEmpty));
                _dialogService.ShowMessage("儲存成功並刪除空的運動！");
                HasChange = false;
            }

            else
            {
                _dialogService.ShowMessage("儲存失敗！！！");
            }
        }

        private void LoadOrCreateLog(DateTime date)
        {
            // 找到當天資料，沒有就新建一筆
            var log = Records.FirstOrDefault(r => r.Date.Date == date.Date);
            if (log == null)
            {
                log = new TrainingJournalItem { Date = date };
                Records.Add(log);
                HasChange = false;
            }

            CurrentLog = log;

            AddPropertyChanged();

            RecalculateTotalDuration();
        }

        // 增加訂閱
        private void AddPropertyChanged()
        {
            if (SelectedType == ExerciseType.Cardio)
            {
                foreach (var item in CurrentLog.Cardios)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            else
            {
                foreach (var item in CurrentLog.Strengths)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
        }

        // 計算總時間
        private void RecalculateTotalDuration()
        {
            if (CurrentLog == null)
            {
                return;
            }

            CurrentLog.CardiosTotalDuration = CurrentLog.Cardios.Sum(c => c.DurationMin);
            CardiosTotalDuration = CurrentLog.CardiosTotalDuration;
        }

        // 讀取有氧運動的類型
        private static List<String> GetCardioTypes()
        {
            var loadedItems = JsonStorageService.Load<ObservableCollection<CardioItem>>("CardioTypes.json");

            List<string> types = loadedItems.Select(x => x.Name).ToList();

            return types;
        }

        // 讀取重量訓練的類型
        private static Dictionary<string, List<string>> GetStrengthTypes()
        {
            var loadedItems = JsonStorageService.Load<ObservableCollection<StrengthItem>>("StrengthTypes.json");

            var exerciseMap = loadedItems
                .GroupBy(e => e.BodyPart)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Name).ToList()
                );

            return exerciseMap;
        }

        // 當任何一個 TrainingJournalItem 的數值改變時，都會觸發這裡
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 任何欄位改變，就執行 JSON 比對
            UpdateDirtyState();
        }

        private void UpdateDirtyState()
        {
            ObservableCollection<TrainingJournalItem> oldCardioTypes = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>(RecordsFileName);

            string currentJson = JsonSerializer.Serialize(Records);
            string oldJson = JsonSerializer.Serialize(oldCardioTypes);

            RecalculateTotalDuration();
            RefreshHasRecord();

            // 更新狀態，這會觸發 UI 按鈕的啟用/停用
            HasChange = currentJson != oldJson;
        }
    }

    public class ExerciseTypeOption
    {
        public string DisplayName { get; }
        public ExerciseType Value { get; }
        public string DotColor { get; }

        public ExerciseTypeOption(string displayName, ExerciseType value, string dotColor)
        {
            DisplayName = displayName;
            Value = value;
            DotColor = dotColor;
        }

        public override string ToString() => DisplayName;
    }
}
