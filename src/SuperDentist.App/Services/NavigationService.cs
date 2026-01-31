using CommunityToolkit.Mvvm.ComponentModel;
using SuperDentist.App.ViewModels;
using System;

namespace SuperDentist.App.Services
{
    public sealed class NavigationService : ObservableObject, INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private ViewModelBase? _currentViewModel;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        {
            CurrentViewModel = (ViewModelBase)_serviceProvider.GetService(typeof(TViewModel))!;
        }
    }
}
