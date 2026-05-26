using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class StrengthViewModel : BaseViewModel
    {
        private const string FileName = "StrengthTypes.json";

        private readonly IDialogService _dialogService;

        public ICollectionView FilteredItems { get; set; } // 篩選用

        private ObservableCollection<StrengthItem> _strengthTypes; // 全部的資料
        public ObservableCollection<StrengthItem> StrengthTypes
        {
            get => _strengthTypes;
            set
            {
                _strengthTypes = value;
                OnPropertyChanged(nameof(StrengthTypes));
            }
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

        private bool _hasChange;
        public bool HasChange
        {
            get => _hasChange;
            set
            {
                _hasChange = value;
                OnPropertyChanged(nameof(HasChange));

                // 通知 WPF 重新檢查 SaveCommand 的狀態
                CommandManager.InvalidateRequerySuggested();
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

            // 初始化時，為現有的所有項目訂閱事件
            foreach (var item in StrengthTypes)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }

            // 監聽集合的變化（新增、刪除、排序）
            StrengthTypes.CollectionChanged += (s, e) => UpdateDirtyState();

            // Commands
            AddEntryCommand = new RelayCommand(() =>
            {
                StrengthTypes.Insert(0, new StrengthItem { BodyPart = SearchText, Name = "" });
                UpdateDirtyState();
            });

            DeleteEntryCommand = new RelayCommand<StrengthItem>(entry =>
            {
                if (_dialogService.Confirm($"確定要刪除「{entry.Name}」嗎？", "確認刪除"))
                {
                    StrengthTypes.Remove(entry);
                    UpdateDirtyState();
                }
            });

            SaveCommand = new RelayCommand(execute: ExecuteSave, canExecute: () => HasChange);
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

        private void ExecuteSave()
        {
            StrengthTypes = new ObservableCollection<StrengthItem>(
                    StrengthTypes
                    .Where(x => x.Name != "" && x.BodyPart != ""));

            FilteredItems = CollectionViewSource.GetDefaultView(StrengthTypes);

            if (JsonStorageService.Save(StrengthTypes, FileName))
            {
                _dialogService.ShowMessage("儲存成功並刪除空的運動！");
                HasChange = false;
            }

            else
            {
                _dialogService.ShowMessage("儲存失敗！！！");
            }
        }

        // 當任何一個 StrengthItem 的數值改變時，都會觸發這裡
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 任何欄位改變，就執行 JSON 比對
            UpdateDirtyState();
        }

        private void UpdateDirtyState()
        {
            ObservableCollection<StrengthItem> oldCardioTypes = JsonStorageService.Load<ObservableCollection<StrengthItem>>(FileName);

            string currentJson = JsonSerializer.Serialize(StrengthTypes);
            string oldJson = JsonSerializer.Serialize(oldCardioTypes);

            // 更新狀態，這會觸發 UI 按鈕的啟用/停用
            HasChange = currentJson != oldJson;
        }

        public override bool CheckHasChanged()
        {
            return HasChange;
        }
    }
}
