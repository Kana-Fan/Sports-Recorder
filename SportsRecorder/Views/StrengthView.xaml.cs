using System.Windows.Controls;
using System.Windows.Input;

namespace SportsRecorder.Views
{
    /// <summary>
    /// StrengthView.xaml 的互動邏輯
    /// </summary>
    public partial class StrengthView : UserControl
    {
        public StrengthView()
        {
            InitializeComponent();
        }

        private void mousewheel(object sender, MouseWheelEventArgs e)
        {
            // 呼叫 ScrollViewer 的「MouseWheel」事件
            var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);

            args.RoutedEvent = ScrollViewer.MouseWheelEvent;

            scroll_viewer.RaiseEvent(args);
        }
    }
}
