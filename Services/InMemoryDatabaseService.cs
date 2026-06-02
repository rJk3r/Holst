using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Holst.Services
{
    /// <summary>
    /// In-memory заглушка (stub) для IDatabaseService.
    /// Хранит пользователей в ConcurrentDictionary, не требует реальной БД.
    /// Удобна для разработки и демонстрации без поднятого PostgreSQL.
    /// </summary>
    public class InMemoryDatabaseService : IDatabaseService
    {
        private record UserInfo(string Password, string Role, DateTime CreatedAt);

        private readonly ConcurrentDictionary<string, UserInfo> _users = new(StringComparer.OrdinalIgnoreCase);

        public string? CurrentUser { get; private set; }
        public string? CurrentRole { get; private set; }

        public InMemoryDatabaseService()
        {
            // Seed: один демо-пользователь для быстрого старта
            _users.TryAdd("admin", new UserInfo("admin", "Admin", DateTime.UtcNow.AddDays(-7)));
        }

        public Task<bool> AuthorizeUserAsync(string login, string password)
        {
            if (_users.TryGetValue(login, out var user) && user.Password == password)
            {
                CurrentUser = login;
                CurrentRole = user.Role;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<string> RegisterNewUserAsync(string login, string password)
        {
            if (_users.ContainsKey(login))
                return Task.FromResult("Пользователь с таким именем уже существует.");

            bool added = _users.TryAdd(login, new UserInfo(password, "User", DateTime.UtcNow));
            return added
                ? Task.FromResult("Пользователь зарегистрирован.")
                : Task.FromResult("Ошибка регистрации.");
        }

        public Task<string> DeleteUserAsync(string targetName)
        {
            bool removed = _users.TryRemove(targetName, out _);
            return removed
                ? Task.FromResult($"Пользователь {targetName} удалён.")
                : Task.FromResult("Пользователь не найден.");
        }

        public Task<bool> CheckPasswordAsync(string password)
        {
            if (string.IsNullOrEmpty(CurrentUser))
                return Task.FromResult(false);

            return _users.TryGetValue(CurrentUser, out var user)
                ? Task.FromResult(user.Password == password)
                : Task.FromResult(false);
        }

        public Task<string> GetUserNameAsync(string name)
        {
            return _users.ContainsKey(name)
                ? Task.FromResult(name)
                : Task.FromResult("Не найден");
        }

        public Task<string> GetAccountCreationDateAsync(string name)
        {
            if (_users.TryGetValue(name, out var user))
                return Task.FromResult(user.CreatedAt.ToString("O"));

            return Task.FromResult("Не найден");
        }

        public Task<string> PromoteToAdminAsync(string login)
        {
            if (!_users.TryGetValue(login, out var user))
                return Task.FromResult("Пользователь не найден.");

            _users[login] = user with { Role = "Admin" };
            return Task.FromResult($"Пользователь {login} успешно назначен администратором.");
        }

        public Task<string> ResetPasswordAsync(string login, string newPassword)
        {
            if (!_users.TryGetValue(login, out var user))
                return Task.FromResult("Пользователь не найден.");

            _users[login] = user with { Password = newPassword };
            return Task.FromResult($"Пароль пользователя {login} сброшен.");
        }

        public Task<string> GetLastActivityAsync(string login)
        {
            return Task.FromResult(DateTime.UtcNow.ToString("O"));
        }

        public Task<string> GetUserRoleAsync(string login)
        {
            if (_users.TryGetValue(login, out var user))
                return Task.FromResult(user.Role);

            return Task.FromResult("User");
        }
    }
}
