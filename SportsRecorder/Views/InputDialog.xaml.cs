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
    /// InputDialog.xaml 的互動邏輯
    /// </summary>
    public partial class InputDialog : Window
    {
        public string? ResultText { get; private set; }
        public InputDialog(string message, string defaultValue, string title)
        {
            InitializeComponent();

            Title = title;
            MessageText.Text = message;
            InputBox.Text = defaultValue;

            Loaded += (s, e) =>
            {
                InputBox.Focus();
                InputBox.SelectAll();
            };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResultText = InputBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ResultText = InputBox.Text;
                DialogResult = true;
            }
        }
    }
}
