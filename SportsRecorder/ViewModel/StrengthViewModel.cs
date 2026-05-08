using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class StrengthViewModel : BaseViewModel
    {
        private string FileName { get; set; } = "StrengthTypes.json";

        private readonly IDialogService _dialogService;

        public ICollectionView FilteredItems { get; set; } // 篩選用

        private ObservableCollection<StrengthItem> _strengthTypes; // 全部的資料
        public ObservableCollection<StrengthItem> StrengthTypes
        {
            get => _strengthTypes;
            private set => SetProperty(ref _strengthTypes, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);

                // 更新篩選
                FilteredItems.Refresh();
            }
        }

        // Commands
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }

        public StrengthViewModel()
        {
            _dialogService = new DialogService();

            _strengthTypes = JsonStorageService.Load<ObservableCollection<StrengthItem>>(FileName); // 讀存檔

            _strengthTypes = new ObservableCollection<StrengthItem>(_strengthTypes.OrderBy(x => x.BodyPart)); // 把一樣的排在一起

            FilteredItems = CollectionViewSource.GetDefaultView(_strengthTypes);

            FilteredItems.Filter = FilterItems;

            // Commands
            AddEntryCommand = new RelayCommand(() =>
            {
                StrengthTypes.Insert(0, new StrengthItem { BodyPart = SearchText, Name = "" });
            });

            DeleteEntryCommand = new RelayCommand<StrengthItem>(entry =>
            {
                if (_dialogService.Confirm($"確定要刪除「{entry.Name}」嗎？", "確認刪除"))
                {
                    StrengthTypes.Remove(entry);

                    if (JsonStorageService.Save(_strengthTypes, FileName))
                    {
                        _dialogService.ShowMessage("儲存成功！");
                    }

                    else
                    {
                        _dialogService.ShowMessage("儲存失敗！！！");
                    }
                }
            });

            SaveCommand = new RelayCommand(() =>
            {
                if (JsonStorageService.Save(_strengthTypes, FileName))
                {
                    _dialogService.ShowMessage("儲存成功！");
                }

                else
                {
                    _dialogService.ShowMessage("儲存失敗！！！");
                }
            });
        }

        private bool FilterItems(object obj)
        {
            if (obj is not StrengthItem item)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            return item.BodyPart.Contains(SearchText)
                || item.Name.Contains(SearchText);
        }
    }
}
