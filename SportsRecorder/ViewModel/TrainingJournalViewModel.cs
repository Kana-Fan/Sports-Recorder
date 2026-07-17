using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class TrainingJournalViewModel : TrainingRecordsViewModel
    {
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
                if (HasChange)
                {
                    _dialogService.ShowMessage("有未儲存的變更，請儲存後再切換類別");
                    // 延遲通知，讓 WPF 本輪綁定完成後再還原 ComboBox
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        OnPropertyChanged(nameof(SelectedType));
                    }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                    return;
                }

                SetProperty(ref _selectedType, value);
                OnPropertyChanged(nameof(IsCardio));
                OnPropertyChanged(nameof(IsStrength));
                OnPropertyChanged(nameof(IsDataEmpty));

                AddPropertyChanged();
            }
        }

        protected override ExerciseType? CurrentType => SelectedType;
        protected override ObservableCollection<Cardio>? CurrentCardios => CurrentLog?.Cardios;
        protected override ObservableCollection<Strength>? CurrentStrengths => CurrentLog?.Strengths;

        private int _cardiosTotalDuration = 0;
        public int CardiosTotalDuration
        {
            get => _cardiosTotalDuration;
            set => SetProperty(ref _cardiosTotalDuration, value);
        }

        public bool IsToday
        {
            get
            {
                return SelectedDate == DateTime.Today;
            }
        }

        // 只顯示符合目前 SelectedType 的範本
        public IEnumerable<WorkoutTemplate> FilteredTemplates =>
            Templates?.Where(t => t.Type == SelectedType) ?? Enumerable.Empty<WorkoutTemplate>();
        private static T DeepClone<T>(T obj) =>
            JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj))!;

        // Commands
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand Refresh { get; }
        public ICommand ApplyTemplateCommand { get; }
        public ICommand SaveAsTemplateCommand { get; }

        public TrainingJournalViewModel()
        {
            LoadOrCreateLog(DateTime.Today);

            RefreshHasRecord();

            // 初始化時，為現有的所有項目訂閱事件
            foreach (var item in Records)
            {
                item.PropertyChanged += (s, e) => UpdateDirtyState();
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

            Refresh = new RelayCommand(execute: ExecuteRefresh, canExecute: CanExecuteRefresh);

            ApplyTemplateCommand = new RelayCommand<WorkoutTemplate>(template =>
            {
                if (template == null)
                {
                    return;
                }

                if (template.Type == ExerciseType.Cardio)
                {
                    foreach (var c in template.Cardios)
                    {
                        CurrentLog.Cardios.Add(DeepClone(c));
                    }

                    RecalculateTotalDuration();
                }
                else
                {
                    foreach (var s in template.Strengths)
                    {
                        CurrentLog.Strengths.Add(DeepClone(s));
                    }
                }

                AddPropertyChanged();   // 重新訂閱新加入項目的 PropertyChanged
                UpdateDirtyState();
                OnPropertyChanged(nameof(IsDataEmpty));
            });

            SaveAsTemplateCommand = new RelayCommand(() =>
            {
                var name = _dialogService.Prompt("請輸入範本名稱：");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                name = name.Trim();

                // 檢查是否有重複名字
                if (IsNameDuplicated(name, SelectedType))
                {
                    return;
                }

                var template = new WorkoutTemplate
                {
                    Name = name,
                    Type = SelectedType,
                    Cardios = SelectedType == ExerciseType.Cardio
                        ? new ObservableCollection<Cardio>(CurrentLog.Cardios.Select(DeepClone))
                        : new(),
                    Strengths = SelectedType == ExerciseType.Strength
                        ? new ObservableCollection<Strength>(CurrentLog.Strengths.Select(DeepClone))
                        : new(),
                };

                Templates.Add(template);
                JsonStorageService.Save(Templates, TemplatesFileName);
                OnPropertyChanged(nameof(FilteredTemplates));
                _dialogService.ShowMessage("範本已儲存！");
            });
        }

        private bool CanExecuteRefresh() => !IsToday;

        private void ExecuteRefresh()
        {
            SelectedDate = DateTime.Today;

            if (!HasChange)
            {
                _dialogService.ShowMessage("~已恢復到今日~");
            }
        }

        private void ExecuteSave()
        {
            // 只檢查當天資料 (CurrentLog) 內的空項目
            if (!RemoveEmptyEntries(SelectedType, CurrentLog.Cardios, CurrentLog.Strengths))
            {
                return;
            }

            // 排序與刪除空行 資料大時很耗時
            var sorted = Records
                .Where(x => (x.Cardios != null && x.Cardios.Count > 0) || (x.Strengths != null && x.Strengths.Count > 0))
                .OrderBy(x => x.Date)
                .ToList();

            Records.Clear();
            foreach (var r in sorted)
            {
                Records.Add(r);
            }

            if (SaveAndNotify(Records, RecordsFileName))
            {
                OnPropertyChanged(nameof(IsDataEmpty));
            }

            // CurrentLog 可能已被清掉，重新載入
            LoadOrCreateLog(SelectedDate);
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
            SubscribeItemsChanged(SelectedType == ExerciseType.Cardio ? CurrentLog.Cardios : CurrentLog.Strengths);
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

        protected override void UpdateDirtyState()
        {
            CompareAndSetDirty(Records, RecordsFileName);

            RecalculateTotalDuration();
            RefreshHasRecord();
        }
    }
}
