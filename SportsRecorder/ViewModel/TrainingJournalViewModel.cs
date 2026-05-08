using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class TrainingJournalViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private ObservableCollection<TrainingJournalItem> Records { get; set; } // 全部的資料

        // 部位與運動的對應字典
        public Dictionary<string, List<string>> StrengthTypes { get; set; }

        private static readonly string FileName = "records.json";

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
                SetProperty(ref _selectedDate, value);
                LoadOrCreateLog(value); // 日期變更就重新載入
            }
        }

        // 當前這天的資料
        private TrainingJournalItem _currentLog;
        public TrainingJournalItem CurrentLog
        {
            get => _currentLog;
            private set => SetProperty(ref _currentLog, value);
        }

        // 暴露給 ComboBox 用的選項清單
        public ObservableCollection<ExerciseTypeOption> ExerciseTypeOptions { get; } = new ObservableCollection<ExerciseTypeOption>
        {
            new ExerciseTypeOption("有氧運動", ExerciseType.Cardio),
            new ExerciseTypeOption("重量訓練", ExerciseType.Strength),
        };

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

        // Commands
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }

        public TrainingJournalViewModel()
        {
            _dialogService = new DialogService();

            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>(FileName); // 讀存檔

            CardioTypes = GetCardioTypes();

            StrengthTypes = GetStrengthTypes();

            BodyTypes = StrengthTypes.Keys.ToList();

            // Commands
            AddEntryCommand = new RelayCommand(() =>
            {
                if (SelectedType == ExerciseType.Cardio)
                {
                    CurrentLog.Cardios.Add(new Cardio());
                }

                else
                {
                    CurrentLog.Strengths.Add(new Strength());
                }

                RecalculateTotalDuration();
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

                    if (JsonStorageService.Save(Records, FileName))
                    {
                        RecalculateTotalDuration();
                        _dialogService.ShowMessage("儲存成功！");
                    }

                    else
                    {
                        _dialogService.ShowMessage("儲存失敗！！！");
                    }
                }
            });

            SaveCommand = new RelayCommand(() =>
            {
                // 排序與刪除空行
                Records = new ObservableCollection<TrainingJournalItem>(
                    Records
                    .Where(x => (x.Cardios != null && x.Cardios.Count > 0) || (x.Strengths != null && x.Strengths.Count > 0))
                    .OrderBy(x => x.Date));

                if (JsonStorageService.Save(Records, FileName))
                {
                    RecalculateTotalDuration();
                    _dialogService.ShowMessage("儲存成功！");
                }

                else
                {
                    _dialogService.ShowMessage("儲存失敗！！！");
                }
            });

            LoadOrCreateLog(DateTime.Today);
        }

        private void LoadOrCreateLog(DateTime date)
        {
            // 找到當天資料，沒有就新建一筆
            var log = Records.FirstOrDefault(r => r.Date.Date == date.Date);
            if (log == null)
            {
                log = new TrainingJournalItem { Date = date };
                Records.Add(log);
            }

            CurrentLog = log;
            RecalculateTotalDuration();
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
    }

    public class ExerciseTypeOption
    {
        public string DisplayName { get; }
        public ExerciseType Value { get; }

        public ExerciseTypeOption(string displayName, ExerciseType value)
        {
            DisplayName = displayName;
            Value = value;
        }

        public override string ToString() => DisplayName;
    }
}
