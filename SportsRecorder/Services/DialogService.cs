using SportsRecorder.Views;
using System.Windows;

namespace SportsRecorder.Services
{
    public class DialogService : IDialogService
    {
        public bool Confirm(string message, string title = "確認")
        {
            var owner = Application.Current.MainWindow;
            var dialog = new ConfirmDialog(message, owner);

            dialog.ShowDialog();

            return dialog.IsConfirmed;
        }

        public bool ShowMessage(string message, string title = "成功")
        {
            var owner = Application.Current.MainWindow;
            var dialog = new ShowSuccessDialog(message, owner);

            dialog.ShowDialog();

            return dialog.IsConfirmed;
        }
    }
}
