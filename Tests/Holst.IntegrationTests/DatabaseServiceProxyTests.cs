using Holst.Services;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Интеграционные тесты для DatabaseServiceProxy.
    /// Проверяет корректность работы прокси-слоя, включая проверку прав доступа.
    /// </summary>
    public class DatabaseServiceProxyTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public DatabaseServiceProxyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task DeleteUserAsync_AsAdmin_AllowsDeletion()
        {
            var service = new DatabaseServiceProxy();
            var adminName = $"admin_{Guid.NewGuid():N}";
            var userName = $"user_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(adminName, password);
            await service.RegisterNewUserAsync(userName, password);

            await service.AuthorizeUserAsync(adminName, password);
            service.CurrentRole = "Admin";

            var result = await service.DeleteUserAsync(userName);

            _output.WriteLine($"Delete as admin result: {result}");
            Assert.DoesNotContain("не админ", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Admin", result);
        }

        [Fact]
        public async Task DeleteUserAsync_AsUser_ReturnsNotAdminError()
        {
            var service = new DatabaseServiceProxy();
            var adminName = $"admin2_{Guid.NewGuid():N}";
            var userName = $"user2_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(adminName, password);
            await service.RegisterNewUserAsync(userName, password);

            await service.AuthorizeUserAsync(userName, password);

            var result = await service.DeleteUserAsync(adminName);

            _output.WriteLine($"Delete as user result: {result}");
            Assert.Contains("не админ", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResetPasswordAsync_AsAdmin_AllowsReset()
        {
            var service = new DatabaseServiceProxy();
            var adminName = $"admin3_{Guid.NewGuid():N}";
            var userName = $"user3_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(adminName, password);
            await service.RegisterNewUserAsync(userName, password);

            await service.AuthorizeUserAsync(adminName, password);
            service.CurrentRole = "Admin";

            var result = await service.ResetPasswordAsync(userName, "NewPass123!");

            _output.WriteLine($"Reset as admin result: {result}");
            Assert.DoesNotContain("не админ", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResetPasswordAsync_AsUser_ReturnsNotAdminError()
        {
            var service = new DatabaseServiceProxy();
            var userName1 = $"user4_{Guid.NewGuid():N}";
            var userName2 = $"user5_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(userName1, password);
            await service.RegisterNewUserAsync(userName2, password);

            await service.AuthorizeUserAsync(userName1, password);

            var result = await service.ResetPasswordAsync(userName2, "NewPass123!");

            _output.WriteLine($"Reset as user result: {result}");
            Assert.Contains("не админ", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PromoteToAdminAsync_UpdatesRole()
        {
            var service = new DatabaseServiceProxy();
            var username = $"promote_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(username, password);
            await service.AuthorizeUserAsync(username, password);

            Assert.Equal("User", service.CurrentRole);

            var result = await service.PromoteToAdminAsync(username);

            _output.WriteLine($"Promote result: {result}");
            Assert.DoesNotContain("не найден", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StaticRole_IsSharedAcrossInstances()
        {
            var service1 = new DatabaseServiceProxy();
            var service2 = new DatabaseServiceProxy();
            var username = $"shared_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service1.RegisterNewUserAsync(username, password);
            await service1.AuthorizeUserAsync(username, password);

            service1.CurrentRole = "Admin";

            Assert.Equal("Admin", service2.CurrentRole);
            _output.WriteLine("Static role sharing works correctly");
        }

        [Fact]
        public async Task CurrentUser_IsSharedAcrossInstances()
        {
            var service1 = new DatabaseServiceProxy();
            var service2 = new DatabaseServiceProxy();
            var username = $"shareduser_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service1.RegisterNewUserAsync(username, password);
            await service1.AuthorizeUserAsync(username, password);

            Assert.Equal(username, service2.CurrentUser);
            _output.WriteLine("Static user sharing works correctly");
        }
    }
}
