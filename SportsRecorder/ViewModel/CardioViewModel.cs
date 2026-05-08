using CommunityToolkit.Mvvm.Input;
using SportsRecorder.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SportsRecorder.ViewModel
{
    internal class CardioViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;

        private ObservableCollection<CardioItem> _cardioTypes; // 全部的資料
        public ObservableCollection<CardioItem> CardioTypes
        {
            get => _cardioTypes;
            private set => SetProperty(ref _cardioTypes, value);
        }

        private string FileName { get; set; } = "CardioTypes.json";

        // Commands
        public ICommand AddEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand SaveCommand { get; }

        public CardioViewModel()
        {
            _dialogService = new DialogService();

            _cardioTypes = JsonStorageService.Load<ObservableCollection<CardioItem>>(FileName); // 讀存檔

            AddEntryCommand = new RelayCommand(() =>
            {
                CardioTypes.Insert(0, new CardioItem());
            });

            DeleteEntryCommand = new RelayCommand<CardioItem>(entry =>
            {
                if (_dialogService.Confirm($"確定要刪除「{entry.Name}」嗎？", "確認刪除"))
                {
                    CardioTypes.Remove(entry);

                    if (JsonStorageService.Save(_cardioTypes, FileName))
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
                if (JsonStorageService.Save(_cardioTypes, FileName))
                {
                    _dialogService.ShowMessage("儲存成功！");
                }

                else
                {
                    _dialogService.ShowMessage("儲存失敗！！！");
                }
            });
        }
    }
}
