using Holst.Commands;
using Holst.Models;
using Holst.Services;
using Holst.Stores;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;
        private readonly ProjectStore _projectStore;
        private readonly AccountStore _accountStore;
        private readonly IDatabaseService _databaseService;

        public ObservableCollection<BaseProject> Projects => _projectStore.Projects;

        public ICommand NavigateAccountCommand { get; }
        public ICommand NavigateProjectEditCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand CreateProjectCommand { get; }

        public HomeViewModel(NavigationStore navigationStore, ProjectStore projectStore, AccountStore accountStore, IDatabaseService databaseService)
        {
            _navigationStore = navigationStore;
            _projectStore = projectStore;
            _accountStore = accountStore;
            _databaseService = databaseService;

            NavigateAccountCommand = new NavigateCommand<AccountViewModel>(navigationStore, () => new AccountViewModel(navigationStore, projectStore, accountStore, databaseService));
            NavigateProjectEditCommand = new NavigateCommand<ProjectEditViewModel>(navigationStore, () => new ProjectEditViewModel(navigationStore, projectStore, accountStore, databaseService));
            OpenProjectCommand = new RelayCommand<BaseProject>(project =>
            {
                _projectStore.CurrentProject = project;
                _navigationStore.CurrentViewModel = new ProjectEditViewModel(navigationStore, projectStore, accountStore, databaseService);
            });
            CreateProjectCommand = new RelayCommand(() =>
            {
                var project = ProjectFactory.CreateProject(ProjectType.Text, "Новый проект", accountStore.CurrentAccount?.Name ?? "User");
                var dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");
                System.IO.Directory.CreateDirectory(dir);
                project.FilePath = System.IO.Path.Combine(dir, $"{project.Name}_{project.Id:N}.holst");
                _projectStore.CurrentProject = project;
                _projectStore.AddProject(project);
                _navigationStore.CurrentViewModel = new ProjectEditViewModel(navigationStore, projectStore, accountStore, databaseService);
            });
        }
    }
}
