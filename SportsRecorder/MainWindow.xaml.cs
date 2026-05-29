using SportsRecorder.Model;
using SportsRecorder.ViewModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SportsRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool _isNavCollapsed = false;
        private const double NavExpandedWidth = 150;

        private void ToggleNavButton_Click(object sender, RoutedEventArgs e)
        {
            double fromWidth = _isNavCollapsed ? 60 : NavExpandedWidth;
            double toWidth = _isNavCollapsed ? NavExpandedWidth : 60;

            var anim = new DoubleAnimation
            {
                From = fromWidth,
                To = toWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            if (_isNavCollapsed)
            {
                // 展開：先顯示 TreeView，再播動畫
                NavTreeView.Visibility = Visibility.Visible;
            }
            else
            {
                // 收合：動畫結束後才隱藏
                anim.Completed += (s, _) => NavTreeView.Visibility = Visibility.Collapsed;
            }

            NavPanel.BeginAnimation(Border.WidthProperty, anim);

            _isNavCollapsed = !_isNavCollapsed;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm && e.NewValue is NavigationItem item)
            {
                vm.NavigateCommand.Execute(item.Key);
            }
        }
    }
}