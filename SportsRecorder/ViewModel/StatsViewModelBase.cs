using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using SportsRecorder.Services;

using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    // ViewModel/StatsViewModelBase.cs
    internal abstract class StatsViewModelBase : TrainingRecordsViewModel, IDisposable
    {
        // --- AxisColor ---
        private SKColor _axisColor = ThemeService.AxisColor;
        public SKColor AxisColor
        {
            get => _axisColor;
            set => SetProperty(ref _axisColor, value);
        }

        // --- Chart axes / frame ---
        private DrawMarginFrame _drawMarginFrame;
        public DrawMarginFrame DrawMarginFrame
        {
            get => _drawMarginFrame;
            set => SetProperty(ref _drawMarginFrame, value);
        }

        private ICartesianAxis[] _xAxes;
        public ICartesianAxis[] XAxes
        {
            get => _xAxes;
            set => SetProperty(ref _xAxes, value);
        }

        private ICartesianAxis[] _yAxes;
        public ICartesianAxis[] YAxes
        {
            get => _yAxes;
            set => SetProperty(ref _yAxes, value);
        }

        // --- TotalDay ---
        private string _totalDay = "";
        public string TotalDay
        {
            get => _totalDay;
            set => SetProperty(ref _totalDay, value);
        }

        // --- Date range ---
        protected DateTime _startDate = DateTime.Today.AddDays(-6);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value);

                if ((EndDate - StartDate).Days < 6)
                {
                    _endDate = StartDate.AddDays(6);
                    OnPropertyChanged(nameof(EndDate));
                    _dialogService.ShowMessage("~已自動調整為最小 7 天區間~");
                }
                else if ((EndDate - StartDate).Days > 30)
                {
                    _endDate = StartDate.AddDays(30);
                    OnPropertyChanged(nameof(EndDate));
                    _dialogService.ShowMessage("~已自動調整為最大 31 天區間~");
                }

                OnDateRangeChanged();
            }
        }

        protected DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                SetProperty(ref _endDate, value);

                if ((EndDate - StartDate).Days < 6)
                {
                    _startDate = EndDate.AddDays(-6);
                    OnPropertyChanged(nameof(StartDate));
                    _dialogService.ShowMessage("~已自動調整為最小 7 天區間~");
                }
                else if ((EndDate - StartDate).Days > 30)
                {
                    _startDate = EndDate.AddDays(-30);
                    OnPropertyChanged(nameof(StartDate));
                    _dialogService.ShowMessage("~已自動調整為最大 31 天區間~");
                }

                OnDateRangeChanged();
            }
        }

        public bool IsThisWeek
        {
            get
            {
                return EndDate == DateTime.Today && StartDate == DateTime.Today.AddDays(-6);
            }
        }

        // --- Commands ---
        public ICommand Refresh { get; }

        protected StatsViewModelBase()
        {
            ThemeService.ThemeChanged += OnThemeChanged;

            Refresh = new RelayCommand(execute: ExecuteRefresh, canExecute: CanExecuteRefresh);
        }

        private bool CanExecuteRefresh() => !IsThisWeek;

        private void ExecuteRefresh()
        {
            _startDate = DateTime.Today.AddDays(-6);
            _endDate = DateTime.Today;
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnRefresh();
            _dialogService.ShowMessage("~已恢復到這一周~");
        }

        protected abstract void ClearSelectionLegend();

        // 日期區間改變後，子類補做的事（更新 axes、重跑 GetData 等）
        protected abstract void OnDateRangeChanged();

        // Refresh 後子類補做的事（重置 SheetTitle 等）
        protected virtual void OnRefresh() { }

        // 子類實作圖表初始化
        protected abstract void InitChart();

        private void OnThemeChanged()
        {
            AxisColor = ThemeService.AxisColor;
            InitChart();
        }

        public void Dispose()
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
        }
    }
}
