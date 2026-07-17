using SportsRecorder.Helpers;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace SportsRecorder.ViewModel
{
    internal abstract class TrainingRecordsViewModel : BaseViewModel
    {
        public static readonly string RecordsFileName = "records.json";
        protected static readonly string TemplatesFileName = "Templates.json";

        private ObservableCollection<TrainingJournalItem> _records;
        public ObservableCollection<TrainingJournalItem> Records
        {
            get => _records;
            protected set
            {
                _records = value;
                OnPropertyChanged(nameof(Records));
            }
        }

        private Dictionary<DateTime, List<TrainingType>> _hasRecord;
        public Dictionary<DateTime, List<TrainingType>> HasRecord
        {
            get => _hasRecord;
            set
            {
                _hasRecord = value;
                OnPropertyChanged(nameof(HasRecord));
            }
        }

        private List<int> _availableYears;
        public List<int> AvailableYears
        {
            get => _availableYears;
            private set
            {
                _availableYears = value;
                OnPropertyChanged(nameof(AvailableYears));
            }
        }

        // 部位與運動的對應字典 / 有氧運動與重訓部位清單，兩個 ViewModel 都需要用來組成 ComboBox 選項
        public Dictionary<string, List<string>> StrengthTypes { get; protected set; }
        public List<string> CardioTypes { get; protected set; }
        public List<string> BodyTypes { get; protected set; }

        // 暴露給 ComboBox 用的選項清單
        public ObservableCollection<ExerciseTypeOption> ExerciseTypeOptions { get; } = new ObservableCollection<ExerciseTypeOption>
        {
            new ExerciseTypeOption("有氧運動", ExerciseType.Cardio,   "#FF43A047"),
            new ExerciseTypeOption("重量訓練", ExerciseType.Strength, "#EF5350"),
        };

        private ObservableCollection<WorkoutTemplate> _templates;
        public ObservableCollection<WorkoutTemplate> Templates
        {
            get => _templates;
            set => SetProperty(ref _templates, value);
        }

        // 目前選取的運動類型（日誌用 SelectedType，範本用 SelectedTemplate.Type）
        // 保持 virtual（非 abstract），避免影響其他既有的子類別（例如 HeatMapViewModel）
        protected virtual ExerciseType? CurrentType => null;

        public bool IsCardio => CurrentType == ExerciseType.Cardio;
        public bool IsStrength => CurrentType == ExerciseType.Strength;

        // 目前正在編輯的有氧/重訓項目集合（日誌用 CurrentLog，範本用 SelectedTemplate）
        protected virtual ObservableCollection<Cardio>? CurrentCardios => null;
        protected virtual ObservableCollection<Strength>? CurrentStrengths => null;

        // 判斷集合是否為空或數量為 0
        public bool IsDataEmpty
        {
            get
            {
                if (CurrentType == null)
                {
                    return true;
                }

                return CurrentType == ExerciseType.Cardio
                    ? (CurrentCardios?.Count ?? 0) == 0
                    : (CurrentStrengths?.Count ?? 0) == 0;
            }
        }

        protected TrainingRecordsViewModel()
        {
            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>(RecordsFileName);
            RefreshHasRecord();

            CardioTypes = GetCardioTypes();
            StrengthTypes = GetStrengthTypes();
            BodyTypes = StrengthTypes.Keys.ToList();

            Templates = JsonStorageService.Load<ObservableCollection<WorkoutTemplate>>(TemplatesFileName);
        }

        protected void RefreshHasRecord(bool needCardios = true, bool needStrengths = true)
        {
            var dict = new Dictionary<DateTime, List<TrainingType>>();

            foreach (var item in Records)
            {
                var types = new List<TrainingType>();

                if (needCardios && item.Cardios?.Count > 0)
                    types.Add(TrainingType.Cardio);

                if (needStrengths && item.Strengths?.Count > 0)
                    types.Add(TrainingType.Strength);

                if (types.Count > 0)
                    dict[item.Date.Date] = types;
            }

            HasRecord = dict;

            AvailableYears = Records
                .Select(r => r.Date.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
        }

        // 讀取有氧運動的類型
        protected static List<string> GetCardioTypes()
        {
            var loadedItems = JsonStorageService.Load<ObservableCollection<CardioItem>>("CardioTypes.json");

            return loadedItems.Select(x => x.Name).ToList();
        }

        // 讀取重量訓練的類型
        protected static Dictionary<string, List<string>> GetStrengthTypes()
        {
            var loadedItems = JsonStorageService.Load<ObservableCollection<StrengthItem>>("StrengthTypes.json");

            return loadedItems
                .GroupBy(e => e.BodyPart)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Name).ToList()
                );
        }

        // 檢查名稱是否重複（同類型底下比對，忽略頭尾空白）
        protected bool IsNameDuplicated(string name, ExerciseType type, WorkoutTemplate? excluding = null)
        {
            if (Templates.Any(t =>
                t != excluding &&
                string.Equals(t.Name.Trim(), name, StringComparison.Ordinal)))
            {
                _dialogService.ShowMessage("已存在相同名稱的範本，請換一個名稱！");
                return true;
            }

            return false;
        }

        // 儲存前先移除未填寫完的空項目；回傳 false 代表使用者取消儲存
        protected bool RemoveEmptyEntries(ExerciseType type, ObservableCollection<Cardio>? cardios, ObservableCollection<Strength>? strengths)
        {
            if (type == ExerciseType.Cardio)
            {
                var emptyCardios = cardios?.Where(c => c.Name == "-選擇運動名稱-").ToList() ?? [];
                if (emptyCardios.Count != 0 && !_dialogService.Confirm("有氧運動有未填寫完的項目，確定要儲存(自動刪除未填寫完的)嗎?"))
                {
                    return false;
                }

                foreach (var c in emptyCardios)
                {
                    cardios!.Remove(c);
                }
            }
            else // Strengths
            {
                var emptyStrengths = strengths?.Where(s => s.BodyPart == "-選擇部位-" || s.Name == "-選擇訓練名稱-").ToList() ?? [];
                if (emptyStrengths.Count != 0 && !_dialogService.Confirm("重量訓練有未填寫完的項目，確定要儲存(自動刪除未填寫完的)嗎?"))
                {
                    return false;
                }

                foreach (var s in emptyStrengths)
                {
                    strengths!.Remove(s);
                }
            }

            return true;
        }

        // 將資料寫入檔案並顯示成功/失敗訊息，成功時清除 HasChange
        protected bool SaveAndNotify<T>(T data, string fileName)
        {
            if (JsonStorageService.Save(data, fileName))
            {
                _dialogService.ShowMessage("儲存成功並刪除未填寫完的運動！");
                HasChange = false;
                return true;
            }

            _dialogService.ShowMessage("儲存失敗！！！");
            return false;
        }

        // 訂閱集合內每個項目的 PropertyChanged，欄位變更時觸發 UpdateDirtyState
        protected void SubscribeItemsChanged(IEnumerable<ExerciseEntryBase>? items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        // 任何欄位改變，就執行 JSON 比對
        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateDirtyState();
        }

        // 保持 virtual（非 abstract），避免影響其他既有的子類別
        protected virtual void UpdateDirtyState()
        {
        }

        // 比對目前資料與檔案內容是否相同，更新 HasChange（會觸發 UI 按鈕的啟用/停用）
        protected void CompareAndSetDirty<T>(T current, string fileName)
            where T : new()
        {
            var old = JsonStorageService.Load<T>(fileName);

            string currentJson = JsonSerializer.Serialize(current);
            string oldJson = JsonSerializer.Serialize(old);

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
