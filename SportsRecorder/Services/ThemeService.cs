using SkiaSharp;
using System.Windows;
using TitlebarAdaptive.WPF;

namespace SportsRecorder.Services
{
    public static class ThemeService
    {
        private const string Dark = "/Styles/DarkStyle.xaml";
        private const string Light = "/Styles/LightStyle.xaml";

        public static bool IsDark { get; private set; } = true;

        public static SKColor AxisColor { get; private set; } = SKColor.Parse("#FFFFFFFF");

        // 主題切換後通知訂閱者
        public static event Action? ThemeChanged;

        public static void Switch()
        {
            IsDark = !IsDark;

            Apply(IsDark ? Dark : Light);
            ApplyColor(IsDark);

            ThemeChanged?.Invoke();
        }

        private static void Apply(string path)
        {
            var resources = Application.Current.Resources.MergedDictionaries;

            var newTheme = new ResourceDictionary() { Source = new Uri(path, UriKind.Relative) };

            // 移除舊的主題
            var existing = resources.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("Style") == true);
            if (existing != null)
            {
                resources[resources.IndexOf(existing)] = newTheme;
            }
            else
            {
                resources.Insert(0, newTheme); // 第一次找不到就插到最前面
            }
        }

        private static void ApplyColor(bool isDark)
        {
            var mainWindow = Application.Current.MainWindow;
            // TitleBarIsLight = true 代表淺色（深色文字），isDark 時要 false
            OriginalWindowHelper.SetTitleBarIsLight(mainWindow, !isDark);

            AxisColor = isDark ? SKColor.Parse("#FFFFFFFF") : SKColor.Parse("#FF000000");
        }
    }
}
