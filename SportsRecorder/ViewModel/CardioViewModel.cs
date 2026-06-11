using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class CardioViewModel : BaseViewModel
    {
        private const string FileName = "CardioTypes.json";

        private ObservableCollection<CardioItem> _cardioTypes; // 全部的資料
        public ObservableCollection<CardioItem> CardioTypes
        {
            get => _cardioTypes;
            set
            {
                _cardioTypes = value;
                OnPropertyChanged(nameof(CardioTypes));
                OnPropertyChanged(nameof(IsDataEmpty));
            }
        }

        // 判斷集合是否為空或數量為 0
        public bool IsDataEmpty
        {
            get
            {
                return CardioTypes.Count() == 0;
            }
        }

        // Commands
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }

        public CardioViewModel()
        {
            HasChange = false;

            CardioTypes = JsonStorageService.Load<ObservableCollection<CardioItem>>(FileName); // 讀存檔

            // 初始化時，為現有的所有項目訂閱事件
            AddPropertyChanged();

            // 監聽集合的變化（新增、刪除、排序）
            CardioTypes.CollectionChanged += (s, e) => UpdateDirtyState();

            AddEntryCommand = new RelayCommand(() =>
            {
                CardioTypes.Insert(0, new CardioItem());
                AddPropertyChanged();
                UpdateDirtyState();
                OnPropertyChanged(nameof(IsDataEmpty));
            });

            DeleteEntryCommand = new RelayCommand<CardioItem>(entry =>
            {
                if (_dialogService.Confirm($"確定要刪除「{entry.Name}」嗎？", "確認刪除"))
                {
                    CardioTypes.Remove(entry);
                    UpdateDirtyState();
                    OnPropertyChanged(nameof(IsDataEmpty));
                }
            });

            SaveCommand = new RelayCommand(execute: ExecuteSave, canExecute: () => HasChange);
        }

        private void ExecuteSave()
        {
            CardioTypes = new ObservableCollection<CardioItem>(
                    CardioTypes
                    .Where(x => x.Name != "" && x.Description != ""));

            if (JsonStorageService.Save(CardioTypes, FileName))
            {
                _dialogService.ShowMessage("儲存成功並刪除空的運動！");
                HasChange = false;
            }

            else
            {
                _dialogService.ShowMessage("儲存失敗！！！");
            }
        }

        private void AddPropertyChanged()
        {
            foreach (var item in CardioTypes)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        // 當任何一個 CardioItem 的數值改變時，都會觸發這裡
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 任何欄位改變，就執行 JSON 比對
            UpdateDirtyState();
        }

        private void UpdateDirtyState()
        {
            ObservableCollection<CardioItem> oldCardioTypes = JsonStorageService.Load<ObservableCollection<CardioItem>>(FileName);

            string currentJson = JsonSerializer.Serialize(CardioTypes);
            string oldJson = JsonSerializer.Serialize(oldCardioTypes);

            // 更新狀態，這會觸發 UI 按鈕的啟用/停用
            HasChange = currentJson != oldJson;
        }
    }
}
