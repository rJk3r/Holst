using Holst.Commands;
using Holst.Models;
using Holst.Services;
using Holst.Stores;
using System.Windows;
using System.Windows.Input;

namespace Holst.ViewModels
{
    public class AuthorizationViewModel : ViewModelBase
    {
        private readonly IAuthorizationService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly AccountStore _accountStore;
        private readonly NavigationStore _navigationStore;
        private readonly ProjectStore _projectStore;

        public ICommand NavigateToHome { get; }
        public ICommand LoginTopCommand { get; }
        public ICommand RegisterTopCommand { get; }
        public ICommand ProceedAuthCommand { get; }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); }
        }

        private bool _isRegistrationMode;
        public bool IsRegistrationMode
        {
            get => _isRegistrationMode;
            set
            {
                _isRegistrationMode = value;
                OnPropertyChanged(nameof(IsRegistrationMode));
                OnPropertyChanged(nameof(ConfirmPasswordVisibility));
                OnPropertyChanged(nameof(ProceedButtonText));
            }
        }

        public Visibility ConfirmPasswordVisibility => IsRegistrationMode ? Visibility.Visible : Visibility.Collapsed;

        public string ProceedButtonText => IsRegistrationMode ? "Зарегистрироваться" : "Войти";

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasErrorMessage)); }
        }

        public Visibility ErrorMessageVisibility => HasErrorMessage ? Visibility.Visible : Visibility.Collapsed;

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public AuthorizationViewModel(NavigationStore navigationStore, ProjectStore projectStore, AccountStore accountStore, IAuthorizationService authService, IDatabaseService databaseService)
        {
            _navigationStore = navigationStore;
            _projectStore = projectStore;
            _accountStore = accountStore;
            _authService = authService;
            _databaseService = databaseService;
            _username = string.Empty;
            _password = string.Empty;
            _confirmPassword = string.Empty;
            _errorMessage = string.Empty;

            NavigateToHome = new NavigateCommand<HomeViewModel>(navigationStore, () => new HomeViewModel(navigationStore, projectStore, accountStore, databaseService));
            LoginTopCommand = new RelayCommand(() => IsRegistrationMode = false);
            RegisterTopCommand = new RelayCommand(() => IsRegistrationMode = true);
            ProceedAuthCommand = new RelayCommand(OnProceedAuth);
        }

        private async void OnProceedAuth()
        {
            ErrorMessage = string.Empty;

            if (IsRegistrationMode)
            {
                var result = await _authService.Register(Username, Username, Password, ConfirmPassword);
                if (result == RegistrationResult.Success)
                {
                    var account = await _authService.Login(Username, Password);
                    if (account != null)
                    {
                        _accountStore.CurrentAccount = account;
                        NavigateToHome.Execute(null);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка автоматического входа после регистрации.";
                    }
                }
                else
                {
                    ErrorMessage = result.ToString();
                }
                return;
            }

            var loginAccount = await _authService.Login(Username, Password);
            if (loginAccount == null)
            {
                ErrorMessage = "Ошибка! Проверьте, правильно ли введены данные.";
                return;
            }

            _accountStore.CurrentAccount = loginAccount;
            NavigateToHome.Execute(null);
        }
    }
}
