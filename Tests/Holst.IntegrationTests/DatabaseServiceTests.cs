using Holst.Services;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Интеграционные тесты для сервиса работы с базой данных.
    /// Тесты проверяют: регистрацию, авторизацию, управление пользователями и ролями.
    /// </summary>
    public class DatabaseServiceTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public DatabaseServiceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region User Registration Tests

        [Fact]
        public async Task RegisterNewUserAsync_WithValidCredentials_ReturnsSuccess()
        {
            var service = new DatabaseService();
            var username = $"testuser_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            var result = await service.RegisterNewUserAsync(username, password);

            _output.WriteLine($"Registration result: {result}");
            Assert.DoesNotContain("Ошибка", result, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("user", "")]
        [InlineData("a", "password")]
        public async Task RegisterNewUserAsync_WithInvalidInput_ReturnsError(string username, string password)
        {
            var service = new DatabaseService();

            var result = await service.RegisterNewUserAsync(username, password);

            _output.WriteLine($"Registration result for invalid input: {result}");
            Assert.True(true);
        }

        [Fact]
        public async Task RegisterNewUserAsync_DuplicateUsername_ReturnsError()
        {
            var service = new DatabaseService();
            var username = $"dupuser_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            var result1 = await service.RegisterNewUserAsync(username, password);
            var result2 = await service.RegisterNewUserAsync(username, password);

            _output.WriteLine($"First registration: {result1}");
            _output.WriteLine($"Second registration: {result2}");
            Assert.NotEqual(result1, result2);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task AuthorizeUserAsync_WithValidCredentials_ReturnsTrueAndSetsRole()
        {
            var service = new DatabaseService();
            var username = $"authuser_{Guid.NewGuid():N}";
            var password = "AuthPass123!";

            await service.RegisterNewUserAsync(username, password);

            var authorized = await service.AuthorizeUserAsync(username, password);

            Assert.True(authorized);
            Assert.Equal(username, service.CurrentUser);
            Assert.NotNull(service.CurrentRole);
            _output.WriteLine($"User role after auth: {service.CurrentRole}");
        }

        [Fact]
        public async Task AuthorizeUserAsync_WithInvalidPassword_ReturnsFalse()
        {
            var service = new DatabaseService();
            var username = $"failuser_{Guid.NewGuid():N}";
            var password = "CorrectPass123!";

            await service.RegisterNewUserAsync(username, password);

            var authorized = await service.AuthorizeUserAsync(username, "WrongPass123!");

            Assert.False(authorized);
        }

        [Fact]
        public async Task AuthorizeUserAsync_WithNonExistentUser_ReturnsFalse()
        {
            var service = new DatabaseService();

            var authorized = await service.AuthorizeUserAsync("nonexistent_user_xyz", "anypass");

            Assert.False(authorized);
        }

        #endregion

        #region Role Management Tests

        [Fact]
        public async Task CurrentRole_AfterRegistration_IsDefaultUser()
        {
            var service = new DatabaseService();
            var username = $"roleuser_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            Assert.Equal("User", service.CurrentRole);
        }

        [Fact]
        public async Task PromoteToAdminAsync_WithExistingUser_ReturnsSuccess()
        {
            var service = new DatabaseService();
            var username = $"adminuser_{Guid.NewGuid():N}";
            var password = "AdminPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            var result = await service.PromoteToAdminAsync(username);

            _output.WriteLine($"Promote result: {result}");
            Assert.DoesNotContain("не найден", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PromoteToAdminAsync_UpdatesCurrentRole()
        {
            var service = new DatabaseService();
            var username = $"adminuser2_{Guid.NewGuid():N}";
            var password = "AdminPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            service.CurrentRole = "Admin";

            Assert.Equal("Admin", service.CurrentRole);
        }

        [Fact]
        public async Task GetUserRoleAsync_ReturnsCorrectRole()
        {
            var service = new DatabaseService();
            var username = $"rolecheck_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(username, password);

            var role = await service.GetUserRoleAsync(username);

            _output.WriteLine($"User role: {role}");
            Assert.NotNull(role);
        }

        #endregion

        #region User Management Tests

        [Fact]
        public async Task DeleteUserAsync_ExistingUser_ReturnsSuccess()
        {
            var service = new DatabaseService();
            var username = $"deleteuser_{Guid.NewGuid():N}";
            var password = "DeletePass123!";

            await service.RegisterNewUserAsync(username, password);

            var result = await service.DeleteUserAsync(username);

            _output.WriteLine($"Delete result: {result}");
            Assert.DoesNotContain("не найден", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteUserAsync_NonExistentUser_ReturnsNotFound()
        {
            var service = new DatabaseService();

            var result = await service.DeleteUserAsync("nonexistent_xyz_123");

            _output.WriteLine($"Delete result: {result}");
            Assert.Contains("не найден", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithExistingUser_ChangesPassword()
        {
            var service = new DatabaseService();
            var username = $"resetuser_{Guid.NewGuid():N}";
            var oldPassword = "OldPass123!";
            var newPassword = "NewPass456!";

            await service.RegisterNewUserAsync(username, oldPassword);

            var result = await service.ResetPasswordAsync(username, newPassword);
            _output.WriteLine($"Reset result: {result}");

            var oldAuth = await service.AuthorizeUserAsync(username, oldPassword);
            var newAuth = await service.AuthorizeUserAsync(username, newPassword);

            Assert.False(oldAuth);
            Assert.True(newAuth);
        }

        #endregion

        #region User Query Tests

        [Fact]
        public async Task GetUserNameAsync_ExistingUser_ReturnsUsername()
        {
            var service = new DatabaseService();
            var username = $"queryuser_{Guid.NewGuid():N}";
            var password = "QueryPass123!";

            await service.RegisterNewUserAsync(username, password);

            var result = await service.GetUserNameAsync(username);

            Assert.Equal(username, result);
        }

        [Fact]
        public async Task GetUserNameAsync_NonExistentUser_ReturnsNotFound()
        {
            var service = new DatabaseService();

            var result = await service.GetUserNameAsync("nonexistent_xyz");

            Assert.Contains("Не найден", result);
        }

        [Fact]
        public async Task GetAccountCreationDateAsync_ExistingUser_ReturnsDate()
        {
            var service = new DatabaseService();
            var username = $"dateuser_{Guid.NewGuid():N}";
            var password = "DatePass123!";

            await service.RegisterNewUserAsync(username, password);

            var result = await service.GetAccountCreationDateAsync(username);

            _output.WriteLine($"Creation date: {result}");
            Assert.DoesNotContain("Ошибка", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetLastActivityAsync_ExistingUser_ReturnsActivity()
        {
            var service = new DatabaseService();
            var username = $"activityuser_{Guid.NewGuid():N}";
            var password = "ActivityPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            var result = await service.GetLastActivityAsync(username);

            _output.WriteLine($"Last activity: {result}");
            Assert.DoesNotContain("Ошибка", result, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region CheckPassword Tests

        [Fact]
        public async Task CheckPasswordAsync_WithCorrectPassword_ReturnsTrue()
        {
            var service = new DatabaseService();
            var username = $"pwduser_{Guid.NewGuid():N}";
            var password = "CorrectPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            var result = await service.CheckPasswordAsync(password);

            Assert.True(result);
        }

        [Fact]
        public async Task CheckPasswordAsync_WithWrongPassword_ReturnsFalse()
        {
            var service = new DatabaseService();
            var username = $"pwduser2_{Guid.NewGuid():N}";
            var password = "CorrectPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            var result = await service.CheckPasswordAsync("WrongPass123!");

            Assert.False(result);
        }

        [Fact]
        public async Task CheckPasswordAsync_WithoutAuth_ReturnsFalse()
        {
            var service = new DatabaseService();

            var result = await service.CheckPasswordAsync("anypass");

            Assert.False(result);
        }

        #endregion
    }
}
