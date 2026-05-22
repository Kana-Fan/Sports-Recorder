using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Wordprocessing;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class CardioViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private const string FileName = "CardioTypes.json";

        private ObservableCollection<CardioItem> _cardioTypes; // 全部的資料
        public ObservableCollection<CardioItem> CardioTypes
        {
            get => _cardioTypes;
            set
            {
                _cardioTypes = value;
                OnPropertyChanged(nameof(CardioTypes));
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

        public CardioViewModel()
        {
            _dialogService = new DialogService();

            HasChange = false;

            CardioTypes = JsonStorageService.Load<ObservableCollection<CardioItem>>(FileName); // 讀存檔

            // 初始化時，為現有的所有項目訂閱事件
            foreach (var item in CardioTypes)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }

            // 監聽集合的變化（新增、刪除、排序）
            CardioTypes.CollectionChanged += (s, e) => UpdateDirtyState();

            AddEntryCommand = new RelayCommand(() =>
            {
                CardioTypes.Insert(0, new CardioItem());
            });

            DeleteEntryCommand = new RelayCommand<CardioItem>(entry =>
            {
                if (_dialogService.Confirm($"確定要刪除「{entry.Name}」嗎？", "確認刪除"))
                {
                    CardioTypes.Remove(entry);
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

        public override bool CheckHasChanged()
        {
            return HasChange;
        }
    }
}
