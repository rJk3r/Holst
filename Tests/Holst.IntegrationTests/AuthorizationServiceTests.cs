using Holst.Models;
using Holst.Services;
using Xunit;
using Xunit.Abstractions;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Интеграционные тесты для сервиса авторизации.
    /// Проверяет регистрацию, вход и управление учетными записями.
    /// </summary>
    public class AuthorizationServiceTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly IAuthorizationService _authService;

        public AuthorizationServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _authService = new AuthorizationService("test_connection");
        }

        #region Registration Tests

        [Fact]
        public async Task Register_WithValidCredentials_ReturnsSuccess()
        {
            var username = $"newuser_{Guid.NewGuid():N}";
            var password = "SecurePass123!";

            var result = await _authService.Register("test@email.com", username, password, password);

            Assert.Equal(RegistrationResult.Success, result);
        }

        [Fact]
        public async Task Register_WithMismatchedPasswords_ReturnsPasswordsDoNotMatch()
        {
            var username = $"mismatch_{Guid.NewGuid():N}";

            var result = await _authService.Register("test@email.com", username, "Password1", "Password2");

            Assert.Equal(RegistrationResult.PasswordsDoNotMatch, result);
        }

        [Fact]
        public async Task Register_WithEmptyUsername_ReturnsUsernameAlreadyExists()
        {
            var result = await _authService.Register("test@email.com", "", "Password123!", "Password123!");

            Assert.Equal(RegistrationResult.UsernameAlreadyExists, result);
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsUsernameAlreadyExists()
        {
            var username = $"duplicate_{Guid.NewGuid():N}";
            var password = "SecurePass123!";

            var result1 = await _authService.Register("test1@email.com", username, password, password);
            Assert.Equal(RegistrationResult.Success, result1);

            var result2 = await _authService.Register("test2@email.com", username, password, password);

            Assert.Equal(RegistrationResult.UsernameAlreadyExists, result2);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsAccount()
        {
            var username = $"loginuser_{Guid.NewGuid():N}";
            var password = "LoginPass123!";

            await _authService.Register("test@email.com", username, password, password);

            var account = await _authService.Login(username, password);

            Assert.NotNull(account);
            Assert.Equal(username, account.Name);
            _output.WriteLine($"Logged in as: {account.Name}, Role: {account.Role}");
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsNull()
        {
            var username = $"faillogin_{Guid.NewGuid():N}";
            var password = "CorrectPass123!";

            await _authService.Register("test@email.com", username, password, password);

            var account = await _authService.Login(username, "WrongPass123!");

            Assert.Null(account);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ReturnsNull()
        {
            var account = await _authService.Login("nonexistent_user_xyz_123", "anypass");

            Assert.Null(account);
        }

        #endregion

        #region Account Properties Tests

        [Fact]
        public async Task Login_ReturnsAccountWithProperties()
        {
            var username = $"propuser_{Guid.NewGuid():N}";
            var password = "PropPass123!";

            await _authService.Register("test@email.com", username, password, password);

            var account = await _authService.Login(username, password);

            Assert.NotEqual(Guid.Empty, account.AccountID);
            Assert.Equal(username, account.Name);
            Assert.NotNull(account.Role);
            Assert.NotEqual(DateTime.MinValue, account.AccountCreationDate);
            Assert.NotEqual(DateTime.MinValue, account.LastLoginDate);

            _output.WriteLine($"Account ID: {account.AccountID}");
            _output.WriteLine($"Role: {account.Role}");
            _output.WriteLine($"Created: {account.AccountCreationDate}");
            _output.WriteLine($"Last Login: {account.LastLoginDate}");
        }

        [Fact]
        public async Task Login_ReturnsAccountWithDefaultRole()
        {
            var username = $"roleuser_{Guid.NewGuid():N}";
            var password = "RolePass123!";

            await _authService.Register("test@email.com", username, password, password);

            var account = await _authService.Login(username, password);

            Assert.Equal("User", account.Role);
        }

        #endregion

        #region GetAccountByUsername Tests

        [Fact]
        public async Task GetAccountByUsername_WithExistingUser_ReturnsAccount()
        {
            var username = $"getuser_{Guid.NewGuid():N}";
            var password = "GetPass123!";

            await _authService.Register("test@email.com", username, password, password);

            var account = await ((AuthorizationService)_authService).GetAccountByUsernameAsync(username);

            Assert.NotNull(account);
            Assert.Equal(username, account.Name);
        }

        [Fact]
        public async Task GetAccountByUsername_WithNonExistentUser_ReturnsNull()
        {
            var account = await ((AuthorizationService)_authService).GetAccountByUsernameAsync("nonexistent_xyz");

            Assert.Null(account);
        }

        [Fact]
        public async Task GetAccountByUsername_WithEmptyUsername_ReturnsNull()
        {
            var account = await ((AuthorizationService)_authService).GetAccountByUsernameAsync("");

            Assert.Null(account);
        }

        [Fact]
        public async Task GetAccountByUsername_ReturnsAccountWithRole()
        {
            var username = $"getroleuser_{Guid.NewGuid():N}";
            var password = "GetRolePass123!";

            await _authService.Register("test@email.com", username, password, password);
            await _authService.Login(username, password);

            var account = await ((AuthorizationService)_authService).GetAccountByUsernameAsync(username);

            Assert.NotNull(account);
            Assert.NotNull(account.Role);
        }

        #endregion

        #region Integration Flow Tests

        [Fact]
        public async Task FullFlow_RegisterLoginCreateProject_Success()
        {
            var username = $"fullflow_{Guid.NewGuid():N}";
            var password = "FlowPass123!";

            var registerResult = await _authService.Register("test@email.com", username, password, password);
            Assert.Equal(RegistrationResult.Success, registerResult);

            var account = await _authService.Login(username, password);
            Assert.NotNull(account);

            var projectStore = new Stores.ProjectStore();
            var project = ProjectFactory.CreateProject(ProjectType.Text, "Test Project", account.Name);
            projectStore.AddProject(project);
            projectStore.CurrentProject = project;

            Assert.Single(projectStore.Projects);
            Assert.Equal(project, projectStore.CurrentProject);
            Assert.Equal(account.Name, project.Author);

            _output.WriteLine($"User {account.Name} created project {project.Name}");
        }

        [Fact]
        public async Task MultipleLogins_UpdateLastLoginDate()
        {
            var username = $"datetest_{Guid.NewGuid():N}";
            var password = "DatePass123!";

            await _authService.Register("test@email.com", username, password, password);

            var account1 = await _authService.Login(username, password);
            var firstLogin = account1.LastLoginDate;

            await Task.Delay(100);

            var account2 = await _authService.Login(username, password);
            var secondLogin = account2.LastLoginDate;

            _output.WriteLine($"First login: {firstLogin}");
            _output.WriteLine($"Second login: {secondLogin}");

            Assert.True(secondLogin >= firstLogin);
        }

        [Fact]
        public async Task RegisterMultipleUsers_DifferentAccounts()
        {
            var username1 = $"multi1_{Guid.NewGuid():N}";
            var username2 = $"multi2_{Guid.NewGuid():N}";
            var password = "MultiPass123!";

            var result1 = await _authService.Register("test1@email.com", username1, password, password);
            var result2 = await _authService.Register("test2@email.com", username2, password, password);

            Assert.Equal(RegistrationResult.Success, result1);
            Assert.Equal(RegistrationResult.Success, result2);

            var account1 = await _authService.Login(username1, password);
            var account2 = await _authService.Login(username2, password);

            Assert.NotEqual(account1.AccountID, account2.AccountID);
            Assert.NotEqual(account1.Name, account2.Name);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Login_AfterPasswordChange_WithOldPassword_Fails()
        {
            var username = $"pwdchange_{Guid.NewGuid():N}";
            var oldPassword = "OldPass123!";
            var newPassword = "NewPass456!";

            await _authService.Register("test@email.com", username, oldPassword, oldPassword);
            await _authService.Login(username, oldPassword);

            var dbService = new DatabaseServiceProxy();
            await dbService.AuthorizeUserAsync(username, oldPassword);
            dbService.CurrentRole = "Admin";
            await dbService.ResetPasswordAsync(username, newPassword);

            var withOld = await _authService.Login(username, oldPassword);
            var withNew = await _authService.Login(username, newPassword);

            Assert.Null(withOld);
            Assert.NotNull(withNew);
        }

        #endregion
    }
}
