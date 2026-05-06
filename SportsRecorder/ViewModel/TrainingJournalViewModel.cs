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
        private ObservableCollection<TrainingJournalItem> Records { get; set; } // 全部的資料

        private static readonly string FileName = "records.json";

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
            Records = JsonStorageService.Load(FileName); // 讀存檔

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

                JsonStorageService.Save(Records, FileName);
                RecalculateTotalDuration();
            });

            SaveCommand = new RelayCommand(() => {
                JsonStorageService.Save(Records, FileName);
                RecalculateTotalDuration();
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
