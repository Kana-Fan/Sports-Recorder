using System.Windows.Controls;
using System.Windows.Input;

namespace SportsRecorder.Views
{
    /// <summary>
    /// TrainingJournalView.xaml 的互動邏輯
    /// </summary>
    public partial class TrainingJournalView : UserControl
    {
        public TrainingJournalView()
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
