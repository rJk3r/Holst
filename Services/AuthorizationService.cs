using Holst.Models;
using Holst.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Holst.ViewModels;

namespace Holst.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IDatabaseService _db;
        private readonly IAuthorizationValidator _validator;

        /// <summary>
        /// Инжектируем сервисы через конструктор (Dependency Injection).
        /// DatabaseService приходит уже обёрнутый в Proxy.
        /// </summary>
        public AuthorizationService(IDatabaseService databaseService, IAuthorizationValidator validator)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<RegistrationResult> Register(string email, string username, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                return RegistrationResult.PasswordsDoNotMatch;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                return RegistrationResult.UsernameAlreadyExists;
            }

            // Check existing username
            var existing = await _db.GetUserNameAsync(username);
            if (!string.IsNullOrEmpty(existing) && existing != "Не найден")
            {
                return RegistrationResult.UsernameAlreadyExists;
            }

            // Perform registration
            await _db.RegisterNewUserAsync(username, password);

            return RegistrationResult.Success;
        }

        public async Task<Account?> Login(string username, string password)
        {
            bool ok = await _db.AuthorizeUserAsync(username, password);
            if (!ok) return null;

            // Build account
            var name = await _db.GetUserNameAsync(username);
            var created = await _db.GetAccountCreationDateAsync(username);

            DateTime dt;
            if (!DateTime.TryParse(created, out dt)) dt = DateTime.MinValue;

            var account = new Account
            {
                AccountID = Guid.NewGuid(),
                Name = name,
                AccountCreationDate = dt,
                Role = (_db.CurrentRole ?? "User"),
                LastLoginDate = DateTime.Now,
            };

            return account;
        }

        /// <summary>
        /// Получить информацию об аккаунте по имени пользователя.
        /// </summary>
        public async Task<Account?> GetAccountByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var name = await _db.GetUserNameAsync(username);
            if (string.IsNullOrEmpty(name) || name == "Не найден") return null;

            var created = await _db.GetAccountCreationDateAsync(username);
            DateTime dt;
            if (!DateTime.TryParse(created, out dt)) dt = DateTime.MinValue;

            var role = await _db.GetUserRoleAsync(username);

            return new Account
            {
                AccountID = Guid.NewGuid(),
                Name = name,
                AccountCreationDate = dt,
                Role = (role ?? "User"),
                LastLoginDate = DateTime.MinValue
            };
        }

        /// <summary>
        /// Удалить пользователя (с проверкой прав).
        /// </summary>
        public async Task<string> DeleteUserAsync(string targetUsername)
        {
            if (!_validator.CanDeleteUser(_db.CurrentRole))
            {
                return "Ошибка: у вас недостаточно прав для удаления пользователя.";
            }

            return await _db.DeleteUserAsync(targetUsername);
        }

        /// <summary>
        /// Повысить пользователя до администратора (с проверкой прав).
        /// </summary>
        public async Task<string> PromoteToAdminAsync(string username)
        {
            if (!_validator.CanPromoteToAdmin(_db.CurrentRole))
            {
                return "Ошибка: у вас недостаточно прав для повышения пользователя.";
            }

            return await _db.PromoteToAdminAsync(username);
        }

        /// <summary>
        /// Сбросить пароль пользователя (с проверкой прав).
        /// </summary>
        public async Task<string> ResetPasswordAsync(string username, string newPassword)
        {
            if (!_validator.CanResetPassword(_db.CurrentRole))
            {
                return "Ошибка: у вас недостаточно прав для сброса паролей.";
            }

            return await _db.ResetPasswordAsync(username, newPassword);
        }
    }
}
