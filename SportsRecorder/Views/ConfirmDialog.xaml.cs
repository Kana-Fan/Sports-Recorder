using System.Windows;

namespace SportsRecorder.Views
{
    // ConfirmDialog.xaml 的互動邏輯
    public partial class ConfirmDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        public ConfirmDialog(string message, Window owner = null)
        {
            InitializeComponent();

            MessageText.Text = message;
            Owner = owner ?? Application.Current.MainWindow;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }
    }
}
