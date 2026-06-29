using System.Configuration;
using System.Data;
using System.Windows;

namespace SportsRecorder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. 建立並顯示開機畫面視窗
            SplashScreenWindow splashScreen = new SplashScreenWindow();
            splashScreen.Show();

            // 2. 建立並準備載入主視窗
            MainWindow mainWindow = new MainWindow();

            // 3. 模擬應用程式啟動延遲 (可選，例如載入資料)
            Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t =>
            {
                // 確保 UI 更新在主執行緒上執行
                Dispatcher.Invoke(() =>
                {
                    // 關閉開機畫面
                    splashScreen.Close();
                    // 顯示主視窗
                    this.MainWindow = mainWindow;
                    mainWindow.Show();
                });
            });
        }
    }

}
