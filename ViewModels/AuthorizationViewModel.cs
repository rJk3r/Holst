using System;
using Holst.Stores;
using Holst.Commands;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class AuthorizationViewModel : ViewModelBase
    {

        public ICommand NavigateToHome { get; }

        public AuthorizationViewModel(NavigationStore navigationStore)
        {
            NavigateToHome = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore));
        }
    }
}
