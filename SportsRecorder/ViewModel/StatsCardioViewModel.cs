using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    class StatsCardioViewModel : BaseViewModel, IDisposable
    {
        private readonly IDialogService _dialogService;

        private static bool _sheetIsTime = true;

        private static readonly SKColor[] DarkPalette =
        [
            new SKColor(0x4D, 0x72, 0xC8), // 靛藍
            new SKColor(0x7A, 0x52, 0xB0), // 深紫
            new SKColor(0x3D, 0x8A, 0x9E), // 深青
            new SKColor(0xA0, 0x50, 0x80), // 暗玫紅
            new SKColor(0x66, 0x55, 0xA8), // 藍紫
            new SKColor(0xA0, 0x60, 0x40), // 暗橙棕
            new SKColor(0x4A, 0x7F, 0xA8), // 鋼藍
            new SKColor(0x8A, 0x4A, 0x6E), // 紫紅
            new SKColor(0x3D, 0x7A, 0x62), // 暗綠
            new SKColor(0x5A, 0x8A, 0x7A), // 灰綠
        ];

        // 空集合時 代表全顯示
        private HashSet<string> _visibleSeries = new();

        private List<TrainingJournalItem> _lastCardioRecords = new();

        private ObservableCollection<LegendItem> _legendItems = new();
        public ObservableCollection<LegendItem> LegendItems
        {
            get => _legendItems;
            set => SetProperty(ref _legendItems, value);
        }

        private ObservableCollection<TrainingJournalItem> Records { get; set; } // 全部的資料

        private string _sheetTitle;
        public string SheetTitle
        {
            get => _sheetTitle;
            set => SetProperty(ref _sheetTitle, value);
        }

        // 區間內的資料(直方圖用) LiveChartsCore.Defaults 的type
        private ISeries[] _selectRecord;
        public ISeries[] SelectRecord
        {
            get => _selectRecord;
            set
            {
                _selectRecord = value;
                OnPropertyChanged(nameof(SelectRecord));
                OnPropertyChanged(nameof(IsDataEmpty));
            }
        }

        // 區間內的總運動類型(表格用)
        private ObservableCollection<Cardio> _selectSheetData;
        public ObservableCollection<Cardio> SelectSheetData
        {
            get => _selectSheetData;
            set => SetProperty(ref _selectSheetData, value);
        }

        // 期間內的有氧運動總運動時間or距離
        private string _totalTxt = "";
        public string TotalTxt
        {
            get => _totalTxt;
            set
            {
                SetProperty(ref _totalTxt, value);
            }
        }

        private string _totalData = "";
        public string TotalData
        {
            get => _totalData;
            set
            {
                SetProperty(ref _totalData, value);
            }
        }

        // 開始日期
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                SetProperty(ref _startDate, value);
                // 如果區間<7 就強制 = 7
                if ((EndDate - StartDate).Days < 7)
                {
                    _endDate = StartDate.AddDays(7);
                    OnPropertyChanged(nameof(EndDate));
                    _dialogService.ShowMessage("~已自動調整為最小 7 天區間~");
                }
                // 如果區間>30 就強制 = 30
                else if ((EndDate - StartDate).Days > 30)
                {
                    _endDate = StartDate.AddDays(30);
                    OnPropertyChanged(nameof(EndDate));
                    _dialogService.ShowMessage("~已自動調整為最大 30 天區間~");
                }

                GetData();
            }
        }

        // 結束日期
        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                SetProperty(ref _endDate, value);
                // 如果區間<7 就強制 = 7
                if ((EndDate - StartDate).Days < 7)
                {
                    _startDate = EndDate.AddDays(-7);
                    OnPropertyChanged(nameof(StartDate));
                    _dialogService.ShowMessage("~已自動調整為最小 7 天區間~");
                }
                // 如果區間>30 就強制 = 30
                else if ((EndDate - StartDate).Days > 30)
                {
                    _startDate = EndDate.AddDays(-30);
                    OnPropertyChanged(nameof(StartDate));
                    _dialogService.ShowMessage("~已自動調整為最大 30 天區間~");
                }

                GetData();
            }
        }

        // 判斷集合是否為空或數量為 0
        public bool IsDataEmpty
        {
            get
            {
                return _lastCardioRecords.Count() == 0;
            }
        }

        // 背景線條顏色
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

        private SKColor _axisColor = ThemeService.AxisColor;
        public SKColor AxisColor
        {
            get => _axisColor;
            set => SetProperty(ref _axisColor, value);
        }

        // Commands
        public ICommand SwitchSheet { get; }

        public ICommand ToggleSeries { get; }

        public ICommand Refresh { get; }

        public StatsCardioViewModel()
        {
            _dialogService = new DialogService();

            Records = JsonStorageService.Load<ObservableCollection<TrainingJournalItem>>("records.json");

            SheetTitle = "每日運動總時間";

            InitChart();
            GetData();

            ThemeService.ThemeChanged += ChangeColor;

            SwitchSheet = new RelayCommand(() =>
            {
                _sheetIsTime = !_sheetIsTime;

                GetData();
            });

            ToggleSeries = new RelayCommand<string>(name =>
            {
                if (_visibleSeries.Contains(name))
                {
                    _visibleSeries.Remove(name);
                }
                else
                {
                    _visibleSeries.Add(name);
                }

                if (_visibleSeries.Count == LegendItems.Count)
                {
                    _visibleSeries.Clear();
                }

                foreach (var item in LegendItems)
                {
                    item.IsVisible = _visibleSeries.Count > 0 && !_visibleSeries.Contains(item.Name);
                }

                UpdateBarChart(_lastCardioRecords);
            });

            Refresh = new RelayCommand(() =>
            {
                _startDate = DateTime.Today.AddDays(-7);
                _endDate = DateTime.Today;

                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(EndDate));

                SheetTitle = "每日運動總時間";
                GetData();

                _dialogService.ShowMessage("~已恢復到這一周~");
            });
        }

        // 獲取期間內的有氧運動資料與總運動時間
        private void GetData()
        {
            SheetTitle = _sheetIsTime ? "每日運動總時間" : "每日運動總距離";

            YAxisLabeler = _sheetIsTime ? value => $"{value:N0} min" : value => $"{value:N2} km";

            // 期間內的有氧運動總時間
            TotalTxt = _sheetIsTime ? "總運動時間" : "總運動距離";

            if (YAxes?[0] is Axis yAxis)
                yAxis.Labeler = YAxisLabeler;

            // 篩選區間內的紀錄
            var cardioRecords =
                Records
                .Where(r =>
                r.Date.Date >= StartDate.Date && r.Date.Date <= EndDate.Date &&
                r.Cardios != null && r.Cardios.Count > 0)
                .ToList();

            _lastCardioRecords = cardioRecords;

            if (cardioRecords.Count == 0)
            {
                _dialogService.ShowMessage("還沒有運動紀錄喔~ 快去運動吧");
            }

            // 區間內的資料（直方圖用：日期 + 各運動時間or 各運動距離)
            UpdateBarChart(cardioRecords);

            // 區間內的總運動類型（表格用：同名有氧合併加總）
            UpdateSheet(cardioRecords);
        }

        private void UpdateBarChart(List<TrainingJournalItem> cardioRecords)
        {
            // 先把所有運動找出來
            var allCardios = cardioRecords
                .SelectMany(r => r.Cardios)
                .Select(s => s.Name)
                .Distinct()
                .ToList();

            // 把日期資料攤平
            var dateGroups = cardioRecords
                .Select(r => new
                {
                    r.Date.Date,
                    DurationMins = r.Cardios
                        .GroupBy(s => s.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => (int)g.Sum(s => s.DurationMin)),
                    DistanceKms = r.Cardios
                        .GroupBy(s => s.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => (double)g.Sum(s => s.DistanceKm))
                })
                .ToList();

            TotalData = _sheetIsTime
                        ? $"{dateGroups.Sum(d => d.DurationMins.Values.Sum()) / 60} 小時 {dateGroups.Sum(d => d.DurationMins.Values.Sum()) % 60} 分鐘"
                        : $"{dateGroups.Sum(d => d.DistanceKms.Values.Sum()):F2} 公里";

            // 計算每日總和加入折線
            var totalSeries = new LineSeries<DateTimePoint>
            {
                Name = "每日總計",
                IsVisible = _visibleSeries.Count == 0 || _visibleSeries.Contains("每日總計"),
                Values = dateGroups.Select(d => new DateTimePoint
                {
                    DateTime = d.Date,
                    Value = _sheetIsTime
                        ? d.DurationMins.Values.Sum()
                        : (double?)d.DistanceKms.Values.Sum()
                }).ToArray(),
                Fill = null,
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(SKColor.Parse("#FF7C8EFF")),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#FF7C8EFF")) { StrokeThickness = 3 },
                Stroke = new SolidColorPaint(SKColor.Parse("#FF7C8EFF")) { StrokeThickness = 5 },
                DataLabelsPaint = new SolidColorPaint(AxisColor)
            };

            SelectRecord = allCardios.Select((Name, index) =>
                (ISeries)new StackedColumnSeries<DateTimePoint>
                {
                    Name = Name,
                    Values = dateGroups.Select(d => new DateTimePoint
                    {
                        DateTime = d.Date,
                        Value = _sheetIsTime ?
                        (d.DurationMins.TryGetValue(Name, out var t) ? t : null) : (d.DistanceKms.TryGetValue(Name, out var w) ? w : null)
                    }).ToArray(),

                    StackGroup = 0,
                    IsVisible = _visibleSeries.Count == 0 || _visibleSeries.Contains(Name),
                    ShowDataLabels = true,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#FFFFFFFF")), // 數值的顏色
                    Fill = new SolidColorPaint(DarkPalette[index % DarkPalette.Length]) // 循環取色
                }
            ).Append(totalSeries).ToArray();

            // 更新圖例（只在系列有變動時重建）
            if (LegendItems.Count != allCardios.Count ||
                !LegendItems.Select(l => l.Name).SequenceEqual(allCardios))
            {
                LegendItems = new ObservableCollection<LegendItem>(
                    allCardios.Select((name, i) => new LegendItem
                    {
                        Name = name,
                        Color = "#" + DarkPalette[i % DarkPalette.Length].Red.ToString("X2")
                                   + DarkPalette[i % DarkPalette.Length].Green.ToString("X2")
                                   + DarkPalette[i % DarkPalette.Length].Blue.ToString("X2"),
                        IsVisible = _visibleSeries.Contains(name)
                    })
                    .Append(new LegendItem
                    {
                        Name = "每日總計",
                        Color = "#A1A7CC", // 你折線的顏色
                        IsVisible = _visibleSeries.Count > 0 && _visibleSeries.Contains("每日總計")
                    })
                );
            }
        }

        private void UpdateSheet(List<TrainingJournalItem> cardioRecords)
        {
            SelectSheetData = new ObservableCollection<Cardio>(
                cardioRecords
                    .SelectMany(r => r.Cardios)
                    .GroupBy(c => c.Name)
                    .Select(g => new Cardio
                    {
                        Name = g.Key,
                        DistanceKm = g.Sum(c => c.DistanceKm),
                        Times = g.Sum(c => c.Times),
                        DurationMin = g.Sum(c => c.DurationMin)
                    })
                    .OrderBy(x => x.Times)
            );
        }

        public Func<DateTime, string> Formatter { get; set; } = date => date.ToString("MM/dd");

        public Func<double, string> YAxisLabeler { get; set; } = _sheetIsTime ? value => $"{value:N0} min" : value => $"{value:N2} km";

        /// <summary>
        /// 把統計圖的設定改成用程式碼設定
        /// </summary>
        private void InitChart()
        {
            DrawMarginFrame = ChartTheme.BuildMarginFrame(AxisColor);
            XAxes = ChartTheme.BuildDateTimeXAxes(AxisColor);
            YAxes = ChartTheme.BuildYAxes(YAxisLabeler, AxisColor);
            UpdateBarChart(_lastCardioRecords);
        }

        private void ChangeColor()
        {
            AxisColor = ThemeService.AxisColor;
            InitChart();
        }

        public void Dispose()
        {
            ThemeService.ThemeChanged -= ChangeColor;
        }
    }
}
