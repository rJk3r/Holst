using Holst.Commands;
using Holst.Models;
using Holst.Stores;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;
        private readonly ProjectStore _projectStore;

        public ObservableCollection<BaseProject> Projects => _projectStore.Projects;

        public ICommand NavigateAccountCommand { get; }
        public ICommand NavigateProjectEditCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand CreateProjectCommand { get; }

        public HomeViewModel(NavigationStore navigationStore, ProjectStore projectStore)
        {
            _navigationStore = navigationStore;
            _projectStore = projectStore;

            NavigateAccountCommand = new NavigateCommand<AccountViewModel>(navigationStore, () => new AccountViewModel(navigationStore, projectStore));
            NavigateProjectEditCommand = new NavigateCommand<ProjectEditViewModel>(navigationStore, () => new ProjectEditViewModel(navigationStore, projectStore));
            OpenProjectCommand = new RelayCommand<BaseProject>(project =>
            {
                _projectStore.CurrentProject = project;
                _navigationStore.CurrentViewModel = new ProjectEditViewModel(navigationStore, projectStore);
            });
            CreateProjectCommand = new RelayCommand(() =>
            {
                _projectStore.CurrentProject = null;
                _navigationStore.CurrentViewModel = new ProjectEditViewModel(navigationStore, projectStore);
            });
        }
    }
}