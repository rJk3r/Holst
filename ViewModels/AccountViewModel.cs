using Holst.Commands;
using Holst.Models;
using Holst.Services;
using Holst.Stores;
using System;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class AccountViewModel : ViewModelBase
    {
        private readonly NavigationStore _navigationStore;
        private readonly ProjectStore _projectStore;
        private readonly AccountStore _accountStore;
        private readonly IDatabaseService _databaseService;

        public string UserName => _accountStore.CurrentAccount?.Name ?? "Гость";

        public DateTime LastLoginDate => _accountStore.CurrentAccount?.LastLoginDate ?? DateTime.MinValue;

        public string LastLoginDateText => LastLoginDate == DateTime.MinValue
            ? "—"
            : LastLoginDate.ToString("dd.MM.yyyy HH:mm");

        private string _role;
        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged(nameof(Role));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(AdminPanelVisibility));
                OnPropertyChanged(nameof(BecomeAdminButtonVisibility));
            }
        }

        public bool IsAdmin => Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

        public System.Windows.Visibility AdminPanelVisibility => IsAdmin ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public System.Windows.Visibility BecomeAdminButtonVisibility => IsAdmin ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        private string _targetUserName;
        public string TargetUserName
        {
            get => _targetUserName;
            set
            {
                _targetUserName = value;
                OnPropertyChanged(nameof(TargetUserName));
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateProjectEditCommand { get; }
        public ICommand CreateProjectCommand { get; }
        public ICommand BecomeAdminCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ResetPasswordCommand { get; }

        public AccountViewModel(NavigationStore navigationStore, ProjectStore projectStore, AccountStore accountStore, IDatabaseService databaseService)
        {
            _navigationStore = navigationStore;
            _projectStore = projectStore;
            _accountStore = accountStore;
            _databaseService = databaseService;

            _role = accountStore.CurrentAccount?.Role ?? "default";
            _targetUserName = string.Empty;
            _statusMessage = string.Empty;

            NavigateHomeCommand = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore, projectStore, accountStore, databaseService));
            NavigateProjectEditCommand = new NavigateCommand<ProjectEditViewModel>(navigationStore, () => new ProjectEditViewModel(navigationStore, projectStore, accountStore, databaseService));
            CreateProjectCommand = new RelayCommand(() =>
            {
                // Используем ProjectFactoryExtensions.Create() вместо ProjectFactory.CreateProject()
                var project = Holst.Models.ProjectFactoryExtensions.Create(Holst.Models.ProjectType.Text, "Новый проект", accountStore.CurrentAccount?.Name ?? "User");
                var dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");
                System.IO.Directory.CreateDirectory(dir);
                project.FilePath = System.IO.Path.Combine(dir, $"{project.Name}_{project.Id:N}.holst");
                projectStore.CurrentProject = project;
                projectStore.AddProject(project);
                navigationStore.CurrentViewModel = new ProjectEditViewModel(navigationStore, projectStore, accountStore, databaseService);
            });
            BecomeAdminCommand = new RelayCommand(OnBecomeAdmin);
            DeleteUserCommand = new RelayCommand(OnDeleteUser);
            ResetPasswordCommand = new RelayCommand(OnResetPassword);
        }

        private async void OnBecomeAdmin()
        {
            if (_accountStore.CurrentAccount == null)
            {
                StatusMessage = "Не авторизован.";
                return;
            }

            var result = await _databaseService.PromoteToAdminAsync(_accountStore.CurrentAccount.Name);
            if (result.Contains("успешно", StringComparison.OrdinalIgnoreCase) || result.Contains("success", StringComparison.OrdinalIgnoreCase))
            {
                Role = "Admin";
                if (_accountStore.CurrentAccount != null)
                    _accountStore.CurrentAccount.Role = "Admin";
                StatusMessage = "Права администратора получены.";
            }
            else
            {
                StatusMessage = result;
            }
        }

        private async void OnDeleteUser()
        {
            if (string.IsNullOrWhiteSpace(TargetUserName))
            {
                StatusMessage = "Введите имя пользователя.";
                return;
            }

            var result = await _databaseService.DeleteUserAsync(TargetUserName);
            StatusMessage = result;
        }

        private async void OnResetPassword()
        {
            if (string.IsNullOrWhiteSpace(TargetUserName))
            {
                StatusMessage = "Введите имя пользователя.";
                return;
            }

            var result = await _databaseService.ResetPasswordAsync(TargetUserName, "1234");
            StatusMessage = result;
        }
    }
}
