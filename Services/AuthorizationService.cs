using Holst.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IDatabaseService _db;

        public AuthorizationService(string connectionString)
        {
            _db = new DatabaseServiceProxy();
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
            var result = await _db.RegisterNewUserAsync(username, password);

            // If result contains error text, treat as failure (simple heuristic)
            if (string.IsNullOrEmpty(result) || result.Contains("Ошибка"))
            {
                return RegistrationResult.EmailAlreadyExists; // best-effort mapping
            }

            return RegistrationResult.Success;
        }

        public async Task<Account> Login(string username, string password)
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
            };

            return account;
        }

        // Helper: get account info by username
        public async Task<Account?> GetAccountByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var name = await _db.GetUserNameAsync(username);
            if (string.IsNullOrEmpty(name) || name == "Не найден") return null;

            var created = await _db.GetAccountCreationDateAsync(username);
            DateTime dt;
            if (!DateTime.TryParse(created, out dt)) dt = DateTime.MinValue;

            return new Account
            {
                AccountID = Guid.NewGuid(),
                Name = name,
                AccountCreationDate = dt,
                Role = (_db.CurrentRole ?? "User")
            };
        }

    }
}
