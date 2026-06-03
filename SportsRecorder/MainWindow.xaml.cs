using SportsRecorder.Model;
using SportsRecorder.ViewModel;
using SportsRecorder.Services;
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
        private readonly IDialogService _dialogService;

        public MainWindow()
        {
            InitializeComponent();
            _dialogService = new DialogService();

            ThemeService.ThemeChanged += RebuildNavForTheme;
        }

        private bool _isNavCollapsed = false;
        private const double NavExpandedWidth = 150;
        private NavigationItem _selectedNavItem;
        private bool _isSyncingSelection = false; // 防止事件重複觸發

        private void ToggleNavButton_Click(object sender, RoutedEventArgs e)
        {
            double fromWidth = _isNavCollapsed ? 52 : NavExpandedWidth;
            double toWidth = _isNavCollapsed ? NavExpandedWidth : 52;

            var anim = new DoubleAnimation
            {
                From = fromWidth,
                To = toWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(280)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            if (_isNavCollapsed)
            {
                NavTreeViewIcon.Visibility = Visibility.Collapsed;
                NavTreeView.Visibility = Visibility.Visible;

                anim.Completed += (s, _) =>
                {
                    if (_selectedNavItem != null)
                    {
                        SelectTreeViewItem(NavTreeView, _selectedNavItem);
                    }
                };
            }
            else
            {
                anim.Completed += (s, _) =>
                {
                    NavTreeView.Visibility = Visibility.Collapsed;
                    NavTreeViewIcon.Visibility = Visibility.Visible;
                    if (_selectedNavItem != null)
                    {
                        SelectTreeViewItem(NavTreeViewIcon, _selectedNavItem);
                    }
                };
            }

            NavPanel.BeginAnimation(Border.WidthProperty, anim);

            _isNavCollapsed = !_isNavCollapsed;
        }

        private bool SelectTreeViewItem(ItemsControl parent, object itemToSelect)
        {
            _isSyncingSelection = true;
            try
            {
                foreach (var item in parent.Items)
                {
                    var container = parent.ItemContainerGenerator
                                          .ContainerFromItem(item) as TreeViewItem;
                    if (container == null)
                    {
                        continue;
                    }

                    if (item == itemToSelect)
                    {
                        container.IsSelected = true;
                        container.BringIntoView();

                        return true;
                    }
                    if (SelectTreeViewItem(container, itemToSelect))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_isSyncingSelection)
            {
                return;
            }

            if (DataContext is MainViewModel vm && e.NewValue is NavigationItem item)
            {
                var parent = FindParent(item);
                _selectedNavItem = parent ?? item;
                vm.NavigateCommand.Execute(item.Key);
            }
        }

        private NavigationItem FindParent(NavigationItem child)
        {
            if (NavTreeView.ItemsSource is not IEnumerable<NavigationItem> navItems)
            {
                return null;
            }

            foreach (var item in navItems)
            {
                if (item.Children.Contains(child))
                {
                    return item;
                }
            }
            return null;
        }

        public void RebuildNavForTheme()
        {
            _isSyncingSelection = true;
            try
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.NavItems = vm.BuildNavItems();

                    // 重建後還原選取狀態
                    if (_selectedNavItem != null)
                    {
                        var treeView = _isNavCollapsed ? NavTreeViewIcon : NavTreeView;
                        SelectTreeViewItem(treeView, _selectedNavItem);
                    }
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.CheckHasChanged())
                {
                    _dialogService.ShowMessage("有未儲存的變更，請儲存後再關閉");
                    e.Cancel = true; // 取消關閉程式
                }
            }
        }

        private void ChangeModeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.Switch();
        }
    }
}