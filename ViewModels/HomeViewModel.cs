using Holst.Commands;
using Holst.Stores;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {

        public ICommand NavigateAccountCommand { get; }

        public HomeViewModel(NavigationStore navigationStore)
        {
            NavigateAccountCommand = new NavigateCommand<AccountViewModel>(navigationStore, () => new AccountViewModel(navigationStore));
        }
    }
}