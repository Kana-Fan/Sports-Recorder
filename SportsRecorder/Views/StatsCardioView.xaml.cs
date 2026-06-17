using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SportsRecorder.Services;

namespace SportsRecorder.Views
{
    /// <summary>
    /// StatsCardioView.xaml 的互動邏輯
    /// </summary>
    public partial class StatsCardioView : UserControl
    {
        private readonly IDialogService _dialogService;

        public StatsCardioView()
        {
            InitializeComponent();

            _dialogService = new DialogService();
        }

        public void SaveChartWithLegend(FrameworkElement element, string filePath)
        {
            double width = scroll_viewer.ExtentWidth;
            double height = scroll_viewer.ExtentHeight;

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(width), (int)Math.Ceiling(height),
                96d, 96d, PixelFormats.Pbgra32);

            // 暫時以完整大小排列，截圖後還原
            double originalHeight = element.ActualHeight;
            double originalWidth = element.ActualWidth;

            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
            renderBitmap.Render(element);

            // 還原佈局，避免 UI 跑版
            element.Arrange(new Rect(0, 0, originalWidth, originalHeight));

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string fileName;
            // 建立一個 OpenFileDialog 實例
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // 設定篩選器 (Filter) - 格式為: "名稱|*.副檔名"
            saveFileDialog.Filter = "PNG檔案 (*.png)|*.png|JPG檔案 (*.jpg)|*.jpg";

            saveFileDialog.FileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            // 如果使用者選擇了檔案並按下「確定」
            if (saveFileDialog.ShowDialog() == true)
            {
                // 取得選擇的檔案路徑
                fileName = saveFileDialog.FileName;
                SaveChartWithLegend(ChartContainer, fileName);

                _dialogService.ShowMessage("圖表儲存成功");
            }
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
