using SportsRecorder.Model;
using SportsRecorder.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SportsRecorder
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected readonly IDialogService _dialogService;

        protected BaseViewModel()
        {
            _dialogService = new DialogService();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // 把每個屬性都會重複寫的三行邏輯抽出來
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private bool _hasChange;
        public bool HasChange
        {
            get => _hasChange;
            set
            {
                if (_hasChange == value)
                {
                    return;
                }

                _hasChange = value;
                OnPropertyChanged(nameof(HasChange));

                // 通知 WPF 重新檢查 SaveCommand 的狀態
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
