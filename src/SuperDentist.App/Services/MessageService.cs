using System.Windows;

namespace SuperDentist.App.Services
{
    public sealed class MessageService : IMessageService
    {
        public void ShowInfo(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "SuperDentist", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "SuperDentist", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ShowConfirmation(string message, string? title = null)
        {
            return MessageBox.Show(message, title ?? "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
