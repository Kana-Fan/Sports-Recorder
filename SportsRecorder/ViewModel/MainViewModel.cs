
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentPage; // 目前選的頁面 類似tag
        private NavigationItem _selectedItem; // 暫時沒用到可以拿掉 可以用來控制選單項目的啟用狀態

        public ObservableCollection<NavigationItem> NavItems { get; }

        public BaseViewModel CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public NavigationItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    NavigateTo(value.Key);
            }
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(p => NavigateTo(p as string));
            NavItems = BuildNavItems();
            NavigateTo("journal");
        }

        private ObservableCollection<NavigationItem> BuildNavItems() => new()
        {
        new() { Title = "訓練日誌", Key = "journal" },
        new() { Title = "運動類型", Key = "exercise", Children = new()
        {
            new() { Title = "有氧", Key = "cardio" },
            new() { Title = "重訓", Key = "strength" },
        }},
        new() { Title = "統計分析", Key = "stats", Children = new()
        {
            new() { Title = "有氧", Key = "stats-cardio" },
            new() { Title = "重訓", Key = "stats-strength" },
        }},
        new() { Title = "資料管理", Key = "data", Children = new()
        {
            new() { Title = "匯出", Key = "export" },
            new() { Title = "匯入", Key = "import" },
        }},
        };

        // 切換內容
        private void NavigateTo(string key)
        {
            CurrentPage = key switch
            {
                "journal" => new TrainingJournalViewModel(),
                "cardio" => new CardioViewModel(),
                "strength" => new StrengthViewModel(),
                _ => CurrentPage
            };
        }
    }
}
