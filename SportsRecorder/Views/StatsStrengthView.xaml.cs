using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SportsRecorder.Services;

namespace SportsRecorder.Views
{
    /// <summary>
    /// StatsStrengthView.xaml 的互動邏輯
    /// </summary>
    public partial class StatsStrengthView : UserControl
    {
        private readonly IDialogService _dialogService;

        public StatsStrengthView()
        {
            InitializeComponent();

            _dialogService = new DialogService();
        }

        public void SaveChartWithLegend(FrameworkElement element, string filePath)
        {
            // 取得元素的實際寬度與高度
            double width = element.ActualWidth;
            double height = element.ActualHeight;

            // 建立 RenderTargetBitmap
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)width, (int)height, 96d, 96d, PixelFormats.Pbgra32);

            // 重新繪製元素（包含圖例與圖表）
            element.Measure(new Size((int)width, (int)height));
            element.Arrange(new Rect(new Size((int)width, (int)height)));
            renderBitmap.Render(element);

            // 編碼為 PNG 檔案
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

            // 如果使用者選擇了檔案並按下「確定」
            if (saveFileDialog.ShowDialog() == true)
            {
                // 取得選擇的檔案路徑
                fileName = saveFileDialog.FileName;
                SaveChartWithLegend(ChartContainer, fileName);

                _dialogService.ShowMessage("圖表儲存成功");
            }
        }
    }
}
