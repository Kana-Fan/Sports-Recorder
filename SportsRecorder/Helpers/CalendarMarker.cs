using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SportsRecorder.Helpers
{
    // 訓練類型
    public enum TrainingType
    {
        Cardio,
        Strength
    }

    public static class CalendarMarker
    {
        // ── 綁定整個 Dictionary 到 Calendar ──

        // 綁定到 Calendar，傳入完整的日期→類型字典用的
        public static readonly DependencyProperty HasRecordDatesProperty =
            DependencyProperty.RegisterAttached(
                "HasRecordDates",
                typeof(Dictionary<DateTime, List<TrainingType>>),
                typeof(CalendarMarker),
                new PropertyMetadata(null, OnHasRecordDatesChanged));

        public static void SetHasRecordDates(DependencyObject obj, Dictionary<DateTime, List<TrainingType>> value)
            => obj.SetValue(HasRecordDatesProperty, value);

        public static Dictionary<DateTime, List<TrainingType>> GetHasRecordDates(DependencyObject obj)
            => (Dictionary<DateTime, List<TrainingType>>)obj.GetValue(HasRecordDatesProperty);

        // 綁定到 CalendarButton（月/年視圖）
        public static readonly DependencyProperty HasAnyRecordProperty =
            DependencyProperty.RegisterAttached(
                "HasAnyRecord",
                typeof(bool),
                typeof(CalendarMarker),
                new PropertyMetadata(false));

        public static void SetHasAnyRecord(DependencyObject obj, bool value)
            => obj.SetValue(HasAnyRecordProperty, value);

        public static bool GetHasAnyRecord(DependencyObject obj)
            => (bool)obj.GetValue(HasAnyRecordProperty);

        // 綁定到 CalendarDayButton（日視圖）
        public static readonly DependencyProperty HasCardioProperty =
            DependencyProperty.RegisterAttached(
                "HasCardio",
                typeof(bool),
                typeof(CalendarMarker),
                new PropertyMetadata(false));

        public static void SetHasCardio(DependencyObject obj, bool value)
            => obj.SetValue(HasCardioProperty, value);

        public static bool GetHasCardio(DependencyObject obj)
            => (bool)obj.GetValue(HasCardioProperty);

        // 綁定到 CalendarDayButton（日視圖）
        public static readonly DependencyProperty HasStrengthProperty =
            DependencyProperty.RegisterAttached(
                "HasStrength",
                typeof(bool),
                typeof(CalendarMarker),
                new PropertyMetadata(false));

        public static void SetHasStrength(DependencyObject obj, bool value)
            => obj.SetValue(HasStrengthProperty, value);

        public static bool GetHasStrength(DependencyObject obj)
            => (bool)obj.GetValue(HasStrengthProperty);

        // 綁定到 Calendar，標記第二個顯示日期
        public static readonly DependencyProperty SecondaryDateProperty =
            DependencyProperty.RegisterAttached(
                "SecondaryDate",
                typeof(DateTime?),
                typeof(CalendarMarker),
                new PropertyMetadata(null, OnSecondaryDateChanged));

        public static void SetSecondaryDate(DependencyObject obj, DateTime? value)
            => obj.SetValue(SecondaryDateProperty, value);

        public static DateTime? GetSecondaryDate(DependencyObject obj)
            => (DateTime?)obj.GetValue(SecondaryDateProperty);

        private static void OnSecondaryDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Calendar calendar)
            {
                return;
            }

            calendar.Loaded -= Calendar_Loaded;
            calendar.Loaded += Calendar_Loaded;

            calendar.DisplayDateChanged -= Calendar_DisplayDateChanged;
            calendar.DisplayDateChanged += Calendar_DisplayDateChanged;

            calendar.DisplayModeChanged -= Calendar_DisplayModeChanged;
            calendar.DisplayModeChanged += Calendar_DisplayModeChanged;

            if (calendar.IsLoaded)
            {
                UpdateDayButtons(calendar);
            }

        }

        public static readonly DependencyProperty IsSecondarySelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSecondarySelected",
                typeof(bool),
                typeof(CalendarMarker),
                new PropertyMetadata(false));

        public static void SetIsSecondarySelected(DependencyObject obj, bool value)
            => obj.SetValue(IsSecondarySelectedProperty, value);

        public static bool GetIsSecondarySelected(DependencyObject obj)
            => (bool)obj.GetValue(IsSecondarySelectedProperty);

        // ── 事件處理 ──
        // 資料來源變更時重新訂閱事件
        private static void OnHasRecordDatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Calendar calendar)
            {
                return;
            }

            // 避免重複訂閱
            calendar.Loaded -= Calendar_Loaded;
            calendar.Loaded += Calendar_Loaded;

            calendar.DisplayDateChanged -= Calendar_DisplayDateChanged;
            calendar.DisplayDateChanged += Calendar_DisplayDateChanged;

            calendar.DisplayModeChanged -= Calendar_DisplayModeChanged; // 新增
            calendar.DisplayModeChanged += Calendar_DisplayModeChanged; // 新增

            if (calendar.IsLoaded)
            {
                UpdateDayButtons(calendar);
            }
        }

        // 控制項載入後更新
        private static void Calendar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Calendar calendar)
                RefreshAll(calendar);
        }

        // 切換月份後更新
        private static void Calendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            if (sender is not Calendar calendar)
            {
                return;
            }

            calendar.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() => RefreshAll(calendar)));
        }

        // 切換視圖模式後更新
        private static void Calendar_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            if (sender is not Calendar calendar)
            {
                return;
            }

            // 視圖切換時視覺樹還在重建，需要等更久
            calendar.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() => RefreshAll(calendar)));
        }

        private static void RefreshAll(Calendar calendar)
        {
            UpdateDayButtons(calendar);
            UpdateCalendarButtons(calendar);
        }

        // ── 更新所有 CalendarDayButton ──

        private static void UpdateDayButtons(Calendar calendar)
        {
            var dict = GetHasRecordDates(calendar);
            var secondary = GetSecondaryDate(calendar);

            foreach (var button in GetVisualDescendants<CalendarDayButton>(calendar))
            {
                if (button.DataContext is not DateTime date)
                {
                    button.SetValue(HasCardioProperty, false);
                    button.SetValue(HasStrengthProperty, false);
                    button.SetValue(IsSecondarySelectedProperty, false);
                    continue;
                }

                if (dict != null && dict.TryGetValue(date.Date, out var types))
                {
                    button.SetValue(HasCardioProperty, types.Contains(TrainingType.Cardio));
                    button.SetValue(HasStrengthProperty, types.Contains(TrainingType.Strength));
                }
                else
                {
                    button.SetValue(HasCardioProperty, false);
                    button.SetValue(HasStrengthProperty, false);
                }

                button.SetValue(IsSecondarySelectedProperty,
                    secondary.HasValue && date.Date == secondary.Value.Date);
            }
        }

        private static void UpdateCalendarButtons(Calendar calendar)
        {
            var dict = GetHasRecordDates(calendar);

            foreach (var button in GetVisualDescendants<CalendarButton>(calendar))
            {
                // CalendarButton 的 DataContext 是 DateTime，
                // Month 視圖是該月第一天，Decade 視圖是該年第一天
                if (button.DataContext is not DateTime date)
                {
                    button.SetValue(HasAnyRecordProperty, false);
                    continue;
                }

                bool hasAny = false;
                if (dict != null)
                {
                    // 判斷該月或該年是否有任何資料
                    hasAny = dict.Keys.Any(d =>
                        d.Year == date.Year &&
                        (calendar.DisplayMode == CalendarMode.Decade || d.Month == date.Month));
                }

                button.SetValue(HasAnyRecordProperty, hasAny);
            }
        }

        // ── 工具：遍歷視覺樹後代 ──
        private static IEnumerable<T> GetVisualDescendants<T>(DependencyObject parent)
            where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var desc in GetVisualDescendants<T>(child))
                {
                    yield return desc;
                }
            }
        }
    }
}
