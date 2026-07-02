using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using SportsRecorder.Services;

namespace SportsRecorder.ViewModel
{
    internal class HeatMapViewModel : TrainingRecordsViewModel, IDisposable
    {
        private readonly Dictionary<(int, int), DateTime> _cardioDateMap = [];
        private readonly Dictionary<(int, int), DateTime> _strengthDateMap = [];
        private string[] _xLabels = [];

        // 綠色色階（Cardio）
        private static readonly LvcColor[] CardioHeatMap =
        [
            ThemeService.IsDark ? SKColors.Gray.AsLvcColor() : SKColors.LightGray.AsLvcColor(), // 空格
            new SKColor(214, 237, 218).AsLvcColor(), // 1   極低
            new SKColor(155, 212, 168).AsLvcColor(), // 2
            new SKColor(83,  181, 121).AsLvcColor(), // 3
            new SKColor(30,  152,  80).AsLvcColor(), // 4
            new SKColor(14,  120,  55).AsLvcColor(), // 5
            new SKColor(0,    90,  40).AsLvcColor(), // 6
            new SKColor(0,    60,  25).AsLvcColor(), // 7   極高
        ];

        // 紅色色階（Strength）
        private static readonly LvcColor[] StrengthHeatMap =
        [
            ThemeService.IsDark ? SKColors.Gray.AsLvcColor() : SKColors.LightGray.AsLvcColor(), // 空格
            new SKColor(250, 210, 210).AsLvcColor(), // 1   極低
            new SKColor(240, 160, 160).AsLvcColor(), // 2
            new SKColor(220, 100, 100).AsLvcColor(), // 3
            new SKColor(200,  55,  55).AsLvcColor(), // 4
            new SKColor(170,  25,  25).AsLvcColor(), // 5
            new SKColor(130,  10,  10).AsLvcColor(), // 6
            new SKColor( 90,   0,   0).AsLvcColor(), // 7   極高
        ];

        // 年份選擇
        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    RebuildSeries();
                }
            }
        }

        // Cardio
        private ISeries[] _cardioSeries = [];
        public ISeries[] CardioSeries
        {
            get => _cardioSeries;
            private set => SetProperty(ref _cardioSeries, value);
        }

        // Strength
        private ISeries[] _strengthSeries = [];
        public ISeries[] StrengthSeries
        {
            get => _strengthSeries;
            private set => SetProperty(ref _strengthSeries, value);
        }

        // 共用
        private ICartesianAxis[] _xAxes = [];
        public ICartesianAxis[] XAxes
        {
            get => _xAxes;
            private set => SetProperty(ref _xAxes, value);
        }

        private ICartesianAxis[] _yAxes = [];
        public ICartesianAxis[] YAxes
        {
            get => _yAxes;
            private set => SetProperty(ref _yAxes, value);
        }

        public bool IsDataEmpty
        {
            get
            {
                return AvailableYears.Count() == 0;
            }
        }

        public HeatMapViewModel()
        {
            SelectedYear = AvailableYears.Count() == 0 ? DateTime.Today.Year : AvailableYears[0];

            ThemeService.ThemeChanged += OnThemeChanged;
        }

        // 重建兩張圖
        private void RebuildSeries()
        {
            var year = _selectedYear;
            var startDate = new DateTime(year, 1, 1);
            // 對齊到該年第一天是星期幾，往前補到週日
            int startDow = (int)startDate.DayOfWeek;
            var gridStart = startDate.AddDays(-startDow);
            var endDate = new DateTime(year, 12, 31);

            int totalWeeks = (int)Math.Ceiling((endDate - gridStart).TotalDays / 7.0) + 1;

            // 聚合資料
            var cardioMap = new Dictionary<(int, int), double>();
            var strengthMap = new Dictionary<(int, int), double>();

            _cardioDateMap.Clear();
            _strengthDateMap.Clear();

            foreach (var record in Records)
            {
                if (record.Date.Year != year)
                {
                    continue;
                }

                int dayIndex = (record.Date - gridStart).Days;
                int week = dayIndex / 7;
                int dow = dayIndex % 7;

                _cardioDateMap.TryAdd((week, dow), record.Date);
                _strengthDateMap.TryAdd((week, dow), record.Date);

                // 有氧 -> 總運動時間
                double cardioVal = record.Cardios?.Sum(c => c.DurationMin) ?? 0;
                if (cardioVal > 0)
                {
                    cardioMap[(week, dow)] = cardioMap.GetValueOrDefault((week, dow)) + cardioVal;
                }

                // 重訓 -> 組數*次數*重量
                double strengthVal = record.Strengths?.Sum(s => s.Sets * s.Reps * s.WeightKg) ?? 0;
                if (strengthVal > 0)
                {
                    strengthMap[(week, dow)] = strengthMap.GetValueOrDefault((week, dow)) + strengthVal;
                }
            }

            // 建立所有格子（含空格）
            var cardioPoints = new List<WeightedPoint>();
            var strengthPoints = new List<WeightedPoint>();

            for (int w = 0; w < totalWeeks; w++)
            {
                for (int d = 0; d < 7; d++)
                {
                    var date = gridStart.AddDays(w * 7 + d);

                    // 只畫當年的格子
                    if (date.Year != year)
                    {
                        continue;
                    }

                    _cardioDateMap.TryAdd((w, d), date);
                    _strengthDateMap.TryAdd((w, d), date);

                    cardioPoints.Add(new WeightedPoint(w, d,
                        cardioMap.GetValueOrDefault((w, d))));

                    strengthPoints.Add(new WeightedPoint(w, d,
                        strengthMap.GetValueOrDefault((w, d))));
                }
            }

            _xLabels = BuildMonthLabels(gridStart, totalWeeks, year);

            // Cardio series
            CardioSeries = ChartTheme.BuildHeatmapSeries(cardioPoints, CardioHeatMap, _cardioDateMap, true);

            // Strength series
            StrengthSeries = ChartTheme.BuildHeatmapSeries(strengthPoints, StrengthHeatMap, _strengthDateMap, false);

            XAxes = ChartTheme.BuildHeatmapXAxes(_xLabels, ThemeService.AxisColor);

            YAxes = ChartTheme.BuildHeatmapYAxes(ThemeService.AxisColor);
        }

        private static string[] BuildMonthLabels(DateTime gridStart, int totalWeeks, int year)
        {
            var labels = new string[totalWeeks];
            int lastMonth = -1;

            for (int w = 0; w < totalWeeks; w++)
            {
                var d = gridStart.AddDays(w * 7);
                // 只顯示當年的月份標籤
                if (d.Year == year && d.Month != lastMonth)
                {
                    labels[w] = d.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);
                    lastMonth = d.Month;
                }
                else
                {
                    labels[w] = "";
                }
            }

            return labels;
        }

        private void OnThemeChanged()
        {
            CardioHeatMap[0] = ThemeService.IsDark ? SKColors.Gray.AsLvcColor() : SKColors.LightGray.AsLvcColor();
            StrengthHeatMap[0] = ThemeService.IsDark ? SKColors.Gray.AsLvcColor() : SKColors.LightGray.AsLvcColor();
            RebuildSeries();
        }

        public void Dispose()
        {
            ThemeService.ThemeChanged -= OnThemeChanged;
        }
    }
}
