namespace SuperDentist.App.Services
{
    public interface IMessageService
    {
        void ShowInfo(string message, string? title = null);
        void ShowError(string message, string? title = null);
        bool ShowConfirmation(string message, string? title = null);
    }
}
