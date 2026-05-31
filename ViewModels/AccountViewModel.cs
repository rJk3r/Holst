using Holst.Stores;
using Holst.Commands;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class AccountViewModel : ViewModelBase
    {
        public string Name => "TestAccount";

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateProjectEditCommand { get; }

        public AccountViewModel(NavigationStore navigationStore, ProjectStore projectStore)
        {
            NavigateHomeCommand = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore, projectStore));
            NavigateProjectEditCommand = new NavigateCommand<ProjectEditViewModel>(navigationStore, () => new ProjectEditViewModel(navigationStore, projectStore));
        }
    }
}
