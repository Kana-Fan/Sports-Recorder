
using SportsRecorder.Model;
using SportsRecorder.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SportsRecorder.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        private ObservableCollection<NavigationItem> _navItems;
        public ObservableCollection<NavigationItem> NavItems
        {
            get => _navItems;
            set
            {
                _navItems = value;
                OnPropertyChanged(nameof(NavItems));
            }
        }

        private BaseViewModel _currentPage; // 目前選的頁面 類似tag
        public BaseViewModel CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            _dialogService = new DialogService();

            NavItems = BuildNavItems();
            CurrentPage = new TrainingJournalViewModel();

            NavigateCommand = new NavRelayCommand(p => NavigateTo(p as string));
        }

        public ObservableCollection<NavigationItem> BuildNavItems() => new()
        {
            new() { Title = "訓練日誌", Key = "journal", IconKey = "Journal"},
            new() { Title = "運動類型", Key = "exercise", IconKey = "Exercise", Children = new()
            {
                new() { Title = "有氧", Key = "cardio", IconKey = "Cardio" },
                new() { Title = "重訓", Key = "strength", IconKey = "Strength" },
            }},
            new() { Title = "統計圖表", Key = "stats", IconKey = "Stats", Children = new()
            {
                new() { Title = "有氧", Key = "stats-cardio", IconKey = "Cardio" },
                new() { Title = "重訓", Key = "stats-strength", IconKey = "Strength" },
            }},
            new() { Title = "資料管理", Key = "data", IconKey = "Data"},
        };

        // 切換內容
        private void NavigateTo(string key)
        {
            if (CurrentPage.CheckHasChanged() == true)
            {
                _dialogService.ShowMessage("有未儲存的變更，請儲存後再切換頁面");
            }
            else
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

        public override bool CheckHasChanged()
        {
            return CurrentPage.CheckHasChanged();
        }
    }
}
