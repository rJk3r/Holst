using System;
using Holst.Stores;
using Holst.Commands;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class AccountViewModel : ViewModelBase
    {
        public string Name => "TestAccount";

        public ICommand NavigateHomeCommand { get;  }

        public AccountViewModel(NavigationStore navigationStore)
        {
            NavigateHomeCommand = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore));
        }
    }
}
