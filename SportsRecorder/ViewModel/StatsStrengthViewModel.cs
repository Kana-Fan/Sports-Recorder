using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class StatsStrengthViewModel : StatsViewModelBase
    {
        private const string DefaultCoachImage = "/Assets/Images/coach.png";
        private const string MuscleCoachImage = "/Assets/Images/coachMuscle.png";

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

        private static readonly string[] GeometrySvgPaths =
        [
            "M 0 -10 A 10 10 0 1 1 -0.01 -10 Z",                                          // 圓
            "M -8 -8 L 8 -8 L 8 8 L -8 8 Z",                                              // 正方形
            "M 0 -10 L 10 0 L 0 10 L -10 0 Z",                                            // 菱形
            "M 0 -10 L 10 8 L -10 8 Z",                                                   // 三角形
            "M 0 -10 L 2.5 -3 L 10 -3 L 4 2 L 6 10 L 0 6 L -6 10 L -4 2 L -10 -3 L -2.5 -3 Z", // 星形
            "M 0 -10 L 3 -3 L 10 -3 L 5 2 L 7 10 L 0 5 L -7 10 L -5 2 L -10 -3 L -3 -3 Z", // 八角星
            "M -5 -10 L 5 -10 L 10 0 L 5 10 L -5 10 L -10 0 Z",                          // 六邊形
            "M 0 -10 L 6 -6 L 10 0 L 6 6 L 0 10 L -6 6 L -10 0 L -6 -6 Z",             // 八邊形
        ];

        private static bool _lineIsWeight = true;
        private static bool _lineIsSet = false;

        // 空集合時 代表全顯示
        private HashSet<string> _bodyPartVisibleSeries = new();

        private HashSet<string> _exercisesVisibleSeries = new();

        private List<TrainingJournalItem> _lastStrengthRecords = new();

        private List<string> _lastAllBodyParts = new();

        private Dictionary<string, SKColor> BodyPartColor { get; set; } // 部位對應的顏色

        private ObservableCollection<LegendItem> _bodyPartLegendItems = new();
        public ObservableCollection<LegendItem> BodyPartLegendItems
        {
            get => _bodyPartLegendItems;
            set => SetProperty(ref _bodyPartLegendItems, value);
        }

        private ObservableCollection<LegendItem> _exercisesLegendItems = new();
        public ObservableCollection<LegendItem> ExercisesLegendItems
        {
            get => _exercisesLegendItems;
            set => SetProperty(ref _exercisesLegendItems, value);
        }

        private string _nowBodyPart = string.Empty;
        public string NowBodyPart
        {
            get => _nowBodyPart;
            set
            {
                if (_nowBodyPart != value)
                {
                    _nowBodyPart = value;
                    OnPropertyChanged(nameof(NowBodyPart));
                }
            }
        }

        // 區間內的資料(直方圖用，一筆就是日期跟各部位的總重量)
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

        // 用來表示各運動的重量變化
        private ISeries[] _lineChartData;
        public ISeries[] LineChartData
        {
            get => _lineChartData;
            set
            {
                _lineChartData = value;
                OnPropertyChanged(nameof(LineChartData));
                OnPropertyChanged(nameof(IsLineChartEmpty));
            }
        }

        private string _lineChartTitle = string.Empty;
        public string LineChartTitle
        {
            get => _lineChartTitle;
            set
            {
                _lineChartTitle = value;
                OnPropertyChanged(nameof(LineChartTitle));
            }
        }

        private string _coachImagePath = DefaultCoachImage;
        public string CoachImagePath
        {
            get => _coachImagePath;
            set
            {
                _coachImagePath = value;
                OnPropertyChanged(nameof(CoachImagePath));
            }
        }

        // 判斷集合是否為空或數量為 0
        public bool IsDataEmpty
        {
            get
            {
                return _lastStrengthRecords.Count() == 0;
            }
        }

        public bool IsLineChartEmpty
        {
            get
            {
                return LineChartData.Count() == 0;
            }
        }

        // 背景線條顏色
        private DrawMarginFrame _lineDrawMarginFrame;
        public DrawMarginFrame LineDrawMarginFrame
        {
            get => _lineDrawMarginFrame;
            set => SetProperty(ref _lineDrawMarginFrame, value);
        }

        private ICartesianAxis[] _linexAxes;
        public ICartesianAxis[] LineXAxes
        {
            get => _linexAxes;
            set => SetProperty(ref _linexAxes, value);
        }

        private ICartesianAxis[] _lineYAxes;
        public ICartesianAxis[] LineYAxes
        {
            get => _lineYAxes;
            set => SetProperty(ref _lineYAxes, value);
        }

        // Commands
        public ICommand BodyPartToggleSeries { get; }
        public ICommand ExercisesToggleSeries { get; }
        public ICommand SwitchSheet { get; }

        public StatsStrengthViewModel()
        {
            InitChart();
            GetData();
            RefreshHasRecord(needCardios: false);

            BodyPartToggleSeries = new RelayCommand<string>(name =>
            {
                ClearLinechart();

                if (_bodyPartVisibleSeries.Contains(name))
                {
                    _bodyPartVisibleSeries.Clear(); // 再點一次 → 全顯示
                }
                else
                {
                    _bodyPartVisibleSeries.Clear();
                    _bodyPartVisibleSeries.Add(name); // 換成新選的

                    // 每個訓練動作的重量變化
                    NowBodyPart = name;
                    UpdateLineChart(_lastStrengthRecords, NowBodyPart);
                }

                foreach (var item in BodyPartLegendItems)
                {
                    item.IsVisible = _bodyPartVisibleSeries.Contains(item.Name);
                }

                UpdateBarChart(_lastAllBodyParts);
            });

            ExercisesToggleSeries = new RelayCommand<string>(name =>
            {
                if (_exercisesVisibleSeries.Contains(name))
                {
                    _exercisesVisibleSeries.Remove(name);
                }
                else
                {
                    _exercisesVisibleSeries.Add(name);
                }

                if (_exercisesVisibleSeries.Count == ExercisesLegendItems.Count)
                {
                    _exercisesVisibleSeries.Clear();
                }

                foreach (var item in ExercisesLegendItems)
                {
                    item.IsVisible = _exercisesVisibleSeries.Contains(item.Name);
                }

                UpdateLineChart(_lastStrengthRecords, NowBodyPart);
            });

            SwitchSheet = new RelayCommand(() =>
            {
                if (_lineIsWeight) // 如果現在是顯示重量
                {
                    _lineIsWeight = false;
                    _lineIsSet = true;
                }
                else if (!_lineIsWeight && _lineIsSet)
                {
                    _lineIsSet = false;
                }
                else
                {
                    _lineIsWeight = true;
                }

                LineChartTitle = _lineIsWeight ? "各部位運動每日最大訓練重量" : _lineIsSet ? "各部位運動每日組數" : "各部位運動每日次數";
                LineYAxisLabeler = _lineIsWeight ? value => $"{value:N0} kg" : _lineIsSet ? value => $"{value:N0} 組" : value => $"{value:N0} 次";

                if (LineYAxes?[0] is Axis yAxis)
                    yAxis.Labeler = LineYAxisLabeler;

                _bodyPartVisibleSeries.Clear();
                UpdateLineChart(_lastStrengthRecords, NowBodyPart);
            });
        }

        private void GetData()
        {
            // 初始化折線圖資料
            ClearLinechart();
            XAxes = ChartTheme.BuildDateTimeXAxes(AxisColor, minDate: _startDate, maxDate: _endDate);
            LineXAxes = ChartTheme.BuildDateTimeXAxes(AxisColor, minDate: _startDate, maxDate: _endDate);

            //  篩選區間內的紀錄
            var strengthRecords =
                Records
                .Where(r =>
                r.Date.Date >= StartDate.Date && r.Date.Date <= EndDate.Date &&
                r.Strengths != null && r.Strengths.Count > 0)
                .ToList();

            TotalDay = $"{strengthRecords.Count} / {(EndDate - StartDate).Days + 1} 天";

            if (strengthRecords.Count >= ((EndDate - StartDate).Days + 1) / 3 * 2)
            {
                CoachImagePath = MuscleCoachImage;
            }
            else
            {
                CoachImagePath = DefaultCoachImage;
            }

            var allBodyParts = strengthRecords
               .SelectMany(r => r.Strengths)
               .Select(s => s.BodyPart)
               .Distinct()
               .ToList();

            _lastAllBodyParts = allBodyParts;
            _lastStrengthRecords = strengthRecords;

            UpdateBodyPartColors(allBodyParts);

            // 每天各部位的總重量（直方圖用）
            _bodyPartVisibleSeries.Clear();
            UpdateBarChart(allBodyParts);

            if (strengthRecords.Count == 0)
            {
                _dialogService.ShowMessage("還沒有重訓紀錄喔~ 快去訓練吧");
            }
        }

        private void ClearLinechart()
        {
            LineChartData = new ISeries[] { };
            NowBodyPart = "請選擇運動部位";
            LineChartTitle = "各部位運動每日最大訓練重量";
            LineYAxisLabeler = value => $"{value:N0} kg";
            _lineIsWeight = true;
            _lineIsSet = false;
            ExercisesLegendItems.Clear();
            _exercisesVisibleSeries.Clear();
        }

        private void UpdateBodyPartColors(List<string> allBodyParts)
        {
            // 設定各部位的顏色
            BodyPartColor = allBodyParts
                .Select((bodyPart, index) => new
                {
                    bodyPart,
                    color = DarkPalette[index % DarkPalette.Length]
                })
                .ToDictionary(
                    x => x.bodyPart,
                    x => x.color
                );
        }

        private void UpdateBarChart(List<string> allBodyParts)
        {
            // 把日期資料攤平
            var dateGroups = _lastStrengthRecords
                .Select(r => new
                {
                    r.Date.Date,
                    ByPart = r.Strengths
                        .GroupBy(s => s.BodyPart)
                        .ToDictionary(
                            g => g.Key,
                            g => (double)g.Sum(s => s.WeightKg * s.Sets * s.Reps))
                })
                .ToList();

            SelectRecord = allBodyParts.Select((bodyPart) =>
                (ISeries)new ColumnSeries<DateTimePoint>
                {
                    Name = bodyPart,
                    Values = dateGroups.Select(d => new DateTimePoint
                    {
                        DateTime = d.Date,
                        Value = d.ByPart.TryGetValue(bodyPart, out var w) ? w : null
                    }).ToArray(),
                    IsVisible = _bodyPartVisibleSeries.Count == 0 || _bodyPartVisibleSeries.Contains(bodyPart),
                    ShowDataLabels = true,
                    DataLabelsPaint = new SolidColorPaint(AxisColor), // 數值的顏色
                    Fill = new SolidColorPaint(BodyPartColor[bodyPart]) // 循環取色
                }
            ).ToArray();

            // 更新圖例（只在系列有變動時重建）
            if (BodyPartLegendItems.Count != allBodyParts.Count ||
                !BodyPartLegendItems.Select(l => l.Name).SequenceEqual(allBodyParts))
            {
                BodyPartLegendItems = new ObservableCollection<LegendItem>(
                    allBodyParts.Select((name, i) => new LegendItem
                    {
                        Name = name,
                        Color = "#" + DarkPalette[i % DarkPalette.Length].Red.ToString("X2")
                                   + DarkPalette[i % DarkPalette.Length].Green.ToString("X2")
                                   + DarkPalette[i % DarkPalette.Length].Blue.ToString("X2"),
                        IsVisible = _bodyPartVisibleSeries.Contains(name)
                    })
                );
            }
        }

        private void UpdateLineChart(List<TrainingJournalItem> strengthRecords, string nowBodyPart)
        {
            if (nowBodyPart == "請選擇運動部位")
            {
                return;
            }

            // 直接過濾指定部位，建立折線系列
            var bodyGroup = strengthRecords
                .SelectMany(r => r.Strengths.Select(s => (r.Date.Date, Strength: s)))
                .Where(x => x.Strength.BodyPart == nowBodyPart)
                .GroupBy(x => x.Strength.BodyPart)
                .FirstOrDefault();

            if (bodyGroup == null)
            {
                return;
            }

            // 取得此部位的運動數量，生成相近色色盤
            int exerciseCount = bodyGroup
                .GroupBy(x => x.Strength.Name)
                .Count();

            SKColor baseColor = BodyPartColor.TryGetValue(nowBodyPart, out var c)
                ? c
                : DarkPalette[0];

            SKColor[] analogousPalette = GenerateAnalogousPalette(baseColor, exerciseCount);

            LineChartData = BuildWeightChangeSeries(bodyGroup, analogousPalette);

            // 更新圖例（只在系列有變動時重建）
            var allExercises = LineChartData
                .Select(s => s.Name)
                .Distinct()
                .ToList();

            ExercisesLegendItems = new ObservableCollection<LegendItem>(
                allExercises.Select((name, i) => new LegendItem
                {
                    Name = name,
                    Color = "#" + analogousPalette[i % analogousPalette.Length].Red.ToString("X2")
                               + analogousPalette[i % analogousPalette.Length].Green.ToString("X2")
                               + analogousPalette[i % analogousPalette.Length].Blue.ToString("X2"),
                    SvgPath = GeometrySvgPaths[i % GeometrySvgPaths.Length],
                    IsVisible = _exercisesVisibleSeries.Contains(name)
                })
            );
        }

        private ISeries[] BuildWeightChangeSeries(IGrouping<string, (DateTime Date, Strength Strength)> bodyGroup, SKColor[] palette)
        {
            var series = bodyGroup
                .GroupBy(x => x.Strength.Name)
                .Select((nameGroup, index) =>
                {
                    var strokeColor = palette[index % palette.Length];

                    return (ISeries)new LineSeries<DateTimePoint, VariableSVGPathGeometry>
                    {
                        Name = nameGroup.Key,
                        Values = nameGroup
                            .GroupBy(x => x.Date)
                            .Select(dayGroup => new DateTimePoint(
                                dayGroup.Key,
                                _lineIsWeight ? dayGroup.Max(x => x.Strength.WeightKg) : _lineIsSet ? dayGroup.Sum(x => x.Strength.Sets) : dayGroup.Sum(x => x.Strength.Reps)
                            ))
                            .OrderBy(p => p.DateTime)
                            .ToArray(),
                        Fill = null,
                        GeometryFill = new SolidColorPaint(strokeColor),
                        GeometryStroke = new SolidColorPaint(strokeColor) { StrokeThickness = 3 },
                        Stroke = new SolidColorPaint(strokeColor) { StrokeThickness = 3 },
                        GeometrySize = 15,
                        GeometrySvg = GeometrySvgPaths[index % GeometrySvgPaths.Length],
                        IsVisible = _exercisesVisibleSeries.Count == 0 || _exercisesVisibleSeries.Contains(nameGroup.Key),
                        Mapping = null,
                    };
                });

            return series.ToArray();
        }

        /// <summary>
        /// 從基底色產生色系
        /// </summary>
        private static SKColor[] GenerateAnalogousPalette(SKColor baseColor, int count)
        {
            // 轉換到 HSV
            baseColor.ToHsv(out float h, out float s, out float v);

            // 深色主題：淺色系（高明度、低飽和）；淺色主題：深色系（高飽和、較廣明度範圍）
            var (vStart, vEnd, vMin, vMax, sStart, sEnd, sMin, sMax) = ThemeService.IsDark
                ? (90f, 98f, 80f, 100f, 45f, 25f, 15f, 60f)
                : (70f, 95f, 30f, 100f, 95f, 60f, 40f, 100f);

            return Enumerable.Range(0, count).Select(i =>
            {
                float ratio = (float)i / Math.Max(count - 1, 1);
                float tV = vStart + (vEnd - vStart) * ratio;
                float tS = sStart + (sEnd - sStart) * ratio;

                // 色相微偏移，讓每條線略有區分
                float hShift = (i % 2 == 0 ? 1 : -1) * (i / 2) * 20f;
                float newH = (h + hShift + 360f) % 360f;

                return SKColor.FromHsv(newH, Math.Clamp(tS, sMin, sMax), Math.Clamp(tV, vMin, vMax));
            }).ToArray();
        }

        public Func<double, string> YAxisLabeler { get; set; } = value => $"{value:N0} kg";
        public Func<double, string> LineYAxisLabeler { get; set; } = _lineIsWeight ?
            value => $"{value:N0} kg" : _lineIsSet ? value => $"{value:N0} 組" : value => $"{value:N0} 次";

        protected override void ClearSelectionLegend()
        {
            _bodyPartVisibleSeries.Clear();
            foreach (var item in BodyPartLegendItems)
            {
                item.IsVisible = _bodyPartVisibleSeries.Contains(item.Name);
            }
        }

        protected override void OnDateRangeChanged()
        {
            XAxes = ChartTheme.BuildDateTimeXAxes(AxisColor, minDate: _startDate, maxDate: _endDate);
            LineXAxes = ChartTheme.BuildDateTimeXAxes(AxisColor, minDate: _startDate, maxDate: _endDate);
            ClearSelectionLegend();
            GetData();
        }

        protected override void OnRefresh()
        {
            ClearSelectionLegend();
            GetData();
        }

        protected override void InitChart()
        {
            DrawMarginFrame = ChartTheme.BuildMarginFrame(AxisColor);
            XAxes = ChartTheme.BuildDateTimeXAxes(AxisColor, minDate: _startDate, maxDate: _endDate);
            YAxes = ChartTheme.BuildYAxes(YAxisLabeler, AxisColor);

            LineDrawMarginFrame = ChartTheme.BuildMarginFrame(AxisColor);
            LineXAxes = ChartTheme.BuildDateTimeXAxes(AxisColor, minDate: _startDate, maxDate: _endDate);
            LineYAxes = ChartTheme.BuildYAxes(LineYAxisLabeler, AxisColor);
            UpdateBarChart(_lastAllBodyParts);
            UpdateLineChart(_lastStrengthRecords, NowBodyPart);
        }
    }
}
