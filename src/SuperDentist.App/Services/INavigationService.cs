using SuperDentist.App.ViewModels;

namespace SuperDentist.App.Services
{
    public interface INavigationService
    {
        ViewModelBase? CurrentViewModel { get; }
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    }
}
