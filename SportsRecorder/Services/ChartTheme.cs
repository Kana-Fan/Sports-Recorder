using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace SportsRecorder.Services
{
    public static class ChartTheme
    {
        /// <summary>
        /// 把統計圖的設定改成用程式碼設定
        /// </summary>

        // 共用顏色
        public static readonly SKColor Gray = SKColor.Parse("#FFA1A7CC");
        public static readonly SKColor Gray1 = SKColor.Parse("#FF888888");
        public static readonly SKColor Gray2 = SKColor.Parse("#FF666666");
        public static readonly SKColor SepColor = SKColor.Parse("#FFC8C8C8");
        public static readonly SKColor ZeroColor = SKColor.Parse("#FF232323");

        private static readonly SKTypeface FontFamily = SKTypeface.FromFamilyName("Microsoft JhengHei");

        // 建立 DrawMarginFrame
        public static DrawMarginFrame BuildMarginFrame(SKColor AxisColor) => new() { Stroke = new SolidColorPaint(AxisColor) { StrokeThickness = 2 } };

        // 建立 XAxes（DateTimeAxis）
        public static ICartesianAxis[] BuildDateTimeXAxes(
            SKColor AxisColor,
            DateTime? minDate = null,
            DateTime? maxDate = null) =>
            [
                new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MM/dd"))
                {
                    LabelsPaint        = new SolidColorPaint(AxisColor){ SKTypeface = FontFamily },
                    SeparatorsPaint    = new SolidColorPaint(SepColor),
                    SeparatorsAtCenter = false,
                    TicksPaint         = new SolidColorPaint(ZeroColor),
                    TicksAtCenter      = true,
                    MinLimit           = (double)minDate?.AddHours(-16).Ticks,
                    MaxLimit           = (double)maxDate?.AddHours(16).Ticks,
                }
            ];

        // 建立 YAxes
        public static ICartesianAxis[] BuildYAxes(Func<double, string> labeler, SKColor AxisColor) =>
            [
                new Axis
                {
                    Labeler            = labeler,
                    LabelsPaint        = new SolidColorPaint(AxisColor)  { SKTypeface = FontFamily },
                    SeparatorsPaint    = new SolidColorPaint(Gray)  { StrokeThickness = 1 },
                    SubseparatorsPaint = new SolidColorPaint(Gray2) { StrokeThickness = 0.5f },
                    SubseparatorsCount = 9,
                    ZeroPaint          = new SolidColorPaint(Gray1) { StrokeThickness = 2 },
                    TicksPaint         = new SolidColorPaint(Gray)  { StrokeThickness = 1.5f },
                    SubticksPaint      = new SolidColorPaint(Gray)  { StrokeThickness = 1 },
                    MinLimit           = 0
                }
            ];

        // 建立 Heatmap XAxes（月份標籤）
        public static ICartesianAxis[] BuildHeatmapXAxes(string[] labels, SKColor axisColor) =>
        [
            new Axis
            {
                Labels           = labels,
                LabelsPaint      = new SolidColorPaint(axisColor) { SKTypeface = FontFamily },
                ShowSeparatorLines = false,
                Position         = LiveChartsCore.Measure.AxisPosition.End, // Top
                TicksPaint       = null,
                TextSize = 11,
                ForceStepToMin  = true,
                MinStep         = 1,
            }
        ];

        // 建立 Heatmap YAxes（星期標籤）
        public static ICartesianAxis[] BuildHeatmapYAxes(SKColor axisColor) =>
        [
            new Axis
            {
                Labels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
                LabelsPaint      = new SolidColorPaint(axisColor) { SKTypeface = FontFamily },
                ShowSeparatorLines = false,
                TicksPaint       = null,
                SeparatorsPaint = null,
                TextSize = 11,
                IsInverted      = true,
            }
        ];

        public static ISeries[] BuildHeatmapSeries(
            List<WeightedPoint> points,
            LvcColor[] heatMap,
            Dictionary<(int, int), DateTime> dateMap,
            bool IsCardio)
        {
            ISeries[] Series = [new HeatSeries<WeightedPoint>
            {
                Values = points,
                HeatMap = heatMap,
                ColorStops = [0, 0.001, 0.15, 0.3, 0.45, 0.62, 0.8, 1],
                PointPadding = new LiveChartsCore.Drawing.Padding(2),
                XToolTipLabelFormatter = point =>
                {
                    var wp = point.Model;
                    int w = (int)(wp?.X ?? 0);
                    int d = (int)(wp?.Y ?? 0);
                    double val = wp?.Weight ?? 0;
                    if (!dateMap.TryGetValue((w, d), out var date))
                    {
                        return string.Empty;
                    }
                    else if(IsCardio)
                    {
                        return val == 0
                            ? $"{date:MMM d, yyyy}  No cardio"
                            : $"{date:MMM d, yyyy}  {val:F0} min";
                    }
                    else
                    {
                        return val == 0
                            ? $"{date:MMM d, yyyy}  No strength"
                            : $"{date:MMM d, yyyy}  {val:F0} kg vol";
                    }
                },
                YToolTipLabelFormatter = _ => string.Empty,
            }];

            return Series;
        }
    }
}
