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
    }
}
