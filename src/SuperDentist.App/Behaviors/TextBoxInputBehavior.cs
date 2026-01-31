using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SuperDentist.App.Behaviors
{
    public static class TextBoxInputBehavior
    {
        public static readonly DependencyProperty AllowedPatternProperty =
            DependencyProperty.RegisterAttached(
                "AllowedPattern",
                typeof(string),
                typeof(TextBoxInputBehavior),
                new PropertyMetadata(string.Empty, OnAllowedPatternChanged));

        public static string GetAllowedPattern(DependencyObject obj) => (string)obj.GetValue(AllowedPatternProperty);
        public static void SetAllowedPattern(DependencyObject obj, string value) => obj.SetValue(AllowedPatternProperty, value);

        private static void OnAllowedPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
            {
                return;
            }

            if (e.NewValue is string pattern && !string.IsNullOrWhiteSpace(pattern))
            {
                textBox.PreviewTextInput += TextBoxOnPreviewTextInput;
                DataObject.AddPastingHandler(textBox, TextBoxOnPaste);
            }
            else
            {
                textBox.PreviewTextInput -= TextBoxOnPreviewTextInput;
                DataObject.RemovePastingHandler(textBox, TextBoxOnPaste);
            }
        }

        private static void TextBoxOnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            string pattern = GetAllowedPattern(textBox);
            e.Handled = !IsInputAllowed(textBox, e.Text, pattern);
        }

        private static void TextBoxOnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
            {
                e.CancelCommand();
                return;
            }

            string pasteText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            string pattern = GetAllowedPattern(textBox);
            if (!IsInputAllowed(textBox, pasteText, pattern))
            {
                e.CancelCommand();
            }
        }

        private static bool IsInputAllowed(TextBox textBox, string input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return true;
            }

            string proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, input);

            return Regex.IsMatch(proposed, pattern);
        }
    }
}
