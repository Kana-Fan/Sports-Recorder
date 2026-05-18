
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
            new() { Title = "訓練日誌", Key = "journal", IconPath = "/Assets/Icons/contract_edit.png"},
            new() { Title = "運動類型", Key = "exercise", IconPath = "/Assets/Icons/database.png", Children = new()
            {
                new() { Title = "有氧", Key = "cardio", IconPath = "/Assets/Icons/vo2_max.png" },
                new() { Title = "重訓", Key = "strength", IconPath = "/Assets/Icons/fitness.png" },
            }},
            new() { Title = "統計分析", Key = "stats", IconPath = "/Assets/Icons/bar_chart.png", Children = new()
            {
                new() { Title = "有氧", Key = "stats-cardio", IconPath = "/Assets/Icons/vo2_max.png" },
                new() { Title = "重訓", Key = "stats-strength", IconPath = "/Assets/Icons/fitness.png" },
            }},
            new() { Title = "資料管理", Key = "data", IconPath = "/Assets/Icons/folder.png"},
        };

        // 切換內容
        private void NavigateTo(string key)
        {
            CurrentPage = key switch
            {
                "journal" => new TrainingJournalViewModel(),
                "cardio" => new CardioViewModel(),
                "strength" => new StrengthViewModel(),
                "stats-cardio" => new StatsCardioViewModel(),
                "stats-strength" => new StatsStrengthViewModel(),
                "data" => new DataManagementViewModel(),
                _ => CurrentPage
            };
        }
    }
}
