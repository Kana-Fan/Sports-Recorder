using SportsRecorder.Model;
using SportsRecorder.Services;
using SportsRecorder.ViewModel;
using System.ComponentModel;
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
        private NavigationItem _selectedNavItem;

        private int _syncingDepth = 0;
        private bool IsSyncing => _syncingDepth > 0;

        private bool _isNavAnimating = false;   // 動畫進行中旗標

        private const double NavExpandedWidth = 150;
        private MainViewModel Vm => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _dialogService = new DialogService();

            ThemeService.ThemeChanged -= RebuildNavForTheme;
            ThemeService.ThemeChanged += RebuildNavForTheme;

            Loaded += MainWindow_Loaded;
        }

        private void SyncSelection(NavigationItem item)
        {
            if (item == null)
            {
                return;
            }

            _syncingDepth++;
            try
            {
                SelectTreeViewItem(NavTreeView, item);
            }
            finally
            {
                _syncingDepth--;
            }
        }

        private bool SelectTreeViewItem(ItemsControl parent, object target)
        {
            foreach (var item in parent.Items)
            {
                var container = parent.ItemContainerGenerator
                                      .ContainerFromItem(item) as TreeViewItem;
                if (container == null)
                {
                    continue;
                }

                if (item == target)
                {
                    container.IsSelected = true;
                    container.BringIntoView();
                    return true;
                }

                if (SelectTreeViewItem(container, target))
                {
                    return true;
                }
            }

            return false;
        }

        private void ToggleNavButton_Click(object sender, RoutedEventArgs e)
        {
            if (Vm == null || _isNavAnimating)
            {
                return;
            }

            ExpandAnimation();
        }

        private void ExpandAnimation(Action onCompleted = null)
        {
            if (_isNavAnimating)
            {
                return;
            }

            _isNavAnimating = true;
            bool expanding = Vm.IsNavCollapsed; // 即將展開

            // 收起時 TreeView 可能自動觸發 SelectedItemChanged，先封鎖
            _syncingDepth++;

            double fromWidth = expanding ? 52 : NavExpandedWidth;
            double toWidth = expanding ? NavExpandedWidth : 52;

            var anim = new DoubleAnimation
            {
                From = fromWidth,
                To = toWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // 展開完成後恢復選取視覺狀態
            anim.Completed += (_, _) =>
            {
                _syncingDepth--;
                _isNavAnimating = false;
                if (expanding)
                {
                    SyncSelection(_selectedNavItem);
                }

                onCompleted?.Invoke();
            };

            NavPanel.BeginAnimation(Border.WidthProperty, anim);

            if (onCompleted == null)
            {
                Vm.IsNavCollapsed = !Vm.IsNavCollapsed;
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsSyncing || Vm == null)
            {
                return;
            }

            if (e.NewValue is not NavigationItem item)
            {
                return;
            }

            var parent = FindParent(item);

            // 收起狀態點了有子節點的父節點 → 展開側欄
            if (Vm.IsNavCollapsed && parent == null && item.Children?.Count > 0)
            {
                if (_isNavAnimating)
                {
                    return; // 動畫進行中，忽略這次選取觸發的展開
                }

                _selectedNavItem = item.Children[0];
                ExpandAnimation(onCompleted: () =>
                {
                    Vm.IsNavCollapsed = !Vm.IsNavCollapsed;
                });

                Vm.NavigateCommand.Execute(_selectedNavItem.Key);
                return;
            }

            _selectedNavItem = parent ?? item;
            Vm.NavigateCommand.Execute(item.Key);
        }

        private void RebuildNavForTheme()
        {
            if (Vm == null)
            {
                return;
            }

            _syncingDepth++;
            try
            {
                var selectedKey = _selectedNavItem?.Key;
                Vm.NavItems = Vm.BuildNavItems();

                if (selectedKey == null)
                {
                    return;
                }

                var newItem = FindNavItemByKey(Vm.NavItems, selectedKey);
                if (newItem != null)
                {
                    _selectedNavItem = newItem;
                    SyncSelection(_selectedNavItem); // 這裡面會自己管 _isSyncingSelection
                }
            }
            finally
            {
                _syncingDepth--;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Vm == null)
            {
                return;
            }

            var defaultItem = FindNavItemByKey(Vm.NavItems, "journal");
            if (defaultItem == null)
            {
                return;
            }

            _selectedNavItem = FindParent(defaultItem) ?? defaultItem;
            SyncSelection(defaultItem);
            Vm.NavigateCommand.Execute(defaultItem.Key);
        }

        private NavigationItem FindParent(NavigationItem child)
        {
            if (NavTreeView.ItemsSource is not IEnumerable<NavigationItem> navItems)
            {
                return null;
            }

            return navItems.FirstOrDefault(item => item.Children?.Contains(child) == true);
        }

        private NavigationItem FindNavItemByKey(IEnumerable<NavigationItem> items, string key)
        {
            foreach (var item in items)
            {
                if (item.Key == key)
                {
                    return item;
                }

                if (item.Children?.Count > 0)
                {
                    var found = FindNavItemByKey(item.Children, key);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Vm?.CheckHasChanged() == true)
            {
                _dialogService.ShowMessage("有未儲存的變更，請儲存後再關閉");
                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ThemeService.ThemeChanged -= RebuildNavForTheme;
        }

        private void ChangeModeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.Switch();
        }
    }
}