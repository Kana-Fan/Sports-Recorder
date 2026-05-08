using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SportsRecorder
{
    /// <summary>
    /// 繼承 DataGridTextColumn，限制儲存格只能輸入數字的自訂欄位。
    /// </summary>
    public class DataGridNumericColumn : DataGridTextColumn
    {
        /// <summary>
        /// 是否允許輸入小數，預設為 false（僅允許整數）。
        /// 可在 XAML 中設定此屬性。
        /// </summary>
        public bool AllowDecimal { get; set; } = false;

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            if (editingElement is TextBox edit)
            {
                edit.PreviewTextInput += Edit_PreviewTextInput;
                edit.PreviewKeyDown += Edit_PreviewKeyDown;
                DataObject.AddPastingHandler(edit, OnPaste);
                InputMethod.SetIsInputMethodEnabled(edit, false);
            }
            return base.PrepareCellForEdit(editingElement, editingEventArgs);
        }

        /// <summary>
        /// 攔截鍵盤字元輸入，驗證加入新字元後的完整字串是否合法。
        /// </summary>
        private void Edit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = GetResultText(textBox, e.Text);

            e.Handled = !IsValidInput(newText);
        }

        /// <summary>
        /// 攔截特殊按鍵，禁止輸入空白鍵。
        /// </summary>
        private void Edit_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 攔截貼上操作，驗證貼上後的完整字串是否合法，不合法則取消貼上。
        /// </summary>s
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            var textBox = sender as TextBox;
            string newText = GetResultText(textBox, text);

            if (!IsValidInput(newText))
            {
                e.CancelCommand();
            }
        }

        private string GetResultText(TextBox textBox, string input)
        {
            int start = textBox.SelectionStart;
            int length = textBox.SelectionLength;

            return textBox.Text.Remove(start, length).Insert(start, input);
        }

        /// <summary>
        /// 驗證字串是否為合法數字格式。
        /// 空字串視為合法（允許清空欄位）。
        /// </summary>
        private bool IsValidInput(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            // 依 AllowDecimal 決定使用小數或整數的驗證規則
            string pattern = AllowDecimal ? @"^\d*\.?\d*$" : @"^\d+$";

            return Regex.IsMatch(text, pattern);
        }
    }
}
