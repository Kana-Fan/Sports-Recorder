using System.Windows;

namespace SportsRecorder.Views
{
    /// <summary>
    /// ShowSuccessDialog.xaml 的互動邏輯
    /// </summary>
    public partial class ShowSuccessDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        public ShowSuccessDialog(string message, Window owner = null)
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
    }
}
