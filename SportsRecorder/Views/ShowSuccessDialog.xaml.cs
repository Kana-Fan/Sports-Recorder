using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
