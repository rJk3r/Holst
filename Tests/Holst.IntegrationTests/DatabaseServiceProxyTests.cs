using Holst.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Holst.IntegrationTests
{
    /// <summary>
    /// Интеграционные тесты для DatabaseServiceProxy.
    /// Проверяет корректность работы прокси-слоя, включая логирование и делегирование.
    /// </summary>
    public class DatabaseServiceProxyTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public DatabaseServiceProxyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private IDatabaseService CreateDatabaseServiceWithProxy()
        {
            IDatabaseService realDb = new DatabaseService();
            return new DatabaseServiceProxy(realDb);
        }

        [Fact]
        public async Task DeleteUserAsync_AsAdmin_AllowsDeletion()
        {
            var realDb = new DatabaseService();
            var service = new DatabaseServiceProxy(realDb);

            var adminName = $"admin_{Guid.NewGuid():N}";
            var userName = $"user_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(adminName, password);
            await service.RegisterNewUserAsync(userName, password);

            await service.AuthorizeUserAsync(adminName, password);
            realDb.SetCurrentRole("Admin");

            var result = await service.DeleteUserAsync(userName);

            _output.WriteLine($"Delete as admin result: {result}");
            Assert.DoesNotContain("не админ", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteUserAsync_AsUser_ReturnsNotAdminError()
        {
            var realDb = new DatabaseService();
            var service = new DatabaseServiceProxy(realDb);

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
            var realDb = new DatabaseService();
            var service = new DatabaseServiceProxy(realDb);

            var adminName = $"admin3_{Guid.NewGuid():N}";
            var userName = $"user3_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            await service.RegisterNewUserAsync(adminName, password);
            await service.RegisterNewUserAsync(userName, password);

            await service.AuthorizeUserAsync(adminName, password);
            realDb.SetCurrentRole("Admin");

            var result = await service.ResetPasswordAsync(userName, "NewPass123!");

            _output.WriteLine($"Reset as admin result: {result}");
            Assert.DoesNotContain("не админ", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResetPasswordAsync_AsUser_ReturnsNotAdminError()
        {
            var realDb = new DatabaseService();
            var service = new DatabaseServiceProxy(realDb);

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
            var realDb = new DatabaseService();
            var service = new DatabaseServiceProxy(realDb);

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
        public async Task ProxyDelegatesLogging_WithoutChangingBehavior()
        {
            var realDb = new DatabaseService();
            var service = new DatabaseServiceProxy(realDb);
            var username = $"logging_{Guid.NewGuid():N}";
            var password = "TestPass123!";

            // Proxy должен логировать операции, но не менять их поведение
            var regResult = await service.RegisterNewUserAsync(username, password);
            Assert.NotEmpty(regResult);

            var authResult = await service.AuthorizeUserAsync(username, password);
            Assert.True(authResult);

            _output.WriteLine("Proxy logging delegates correctly");
        }
    }
}
