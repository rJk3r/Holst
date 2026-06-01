using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Services
{
    /// <summary>
    /// Proxy Pattern: оборачивает IDatabaseService для логирования операций.
    /// Делегирует все вызовы реальному сервису без изменения поведения.
    /// </summary>
    public class DatabaseServiceProxy : IDatabaseService
    {
        private readonly IDatabaseService _realService;

        /// <summary>
        /// Инжектируем реальный сервис через конструктор (вместо создания внутри).
        /// </summary>
        public DatabaseServiceProxy(IDatabaseService realService)
        {
            _realService = realService ?? throw new ArgumentNullException(nameof(realService));
        }

        public string? CurrentUser => _realService.CurrentUser;
        public string? CurrentRole => _realService.CurrentRole;

        public async Task<string> RegisterNewUserAsync(string login, string password)
        {
            Debug.WriteLine($"[LOG] User registration attempt: {login}");
            try
            {
                var result = await _realService.RegisterNewUserAsync(login, password);
                Debug.WriteLine($"[LOG] User registration result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG] User registration error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AuthorizeUserAsync(string login, string password)
        {
            Debug.WriteLine($"[LOG] Authorization attempt for user: {login}");
            try
            {
                bool result = await _realService.AuthorizeUserAsync(login, password);
                Debug.WriteLine($"[LOG] Authorization result for '{login}': {(result ? "success" : "failed")}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG] Authorization error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> DeleteUserAsync(string targetName)
        {
            Debug.WriteLine($"[LOG] Delete user attempt: {targetName}");
            try
            {
                var result = await _realService.DeleteUserAsync(targetName);
                Debug.WriteLine($"[LOG] Delete user result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG] Delete user error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CheckPasswordAsync(string password)
        {
            Debug.WriteLine($"[LOG] Password check for user: {CurrentUser}");
            try
            {
                bool result = await _realService.CheckPasswordAsync(password);
                Debug.WriteLine($"[LOG] Password check result: {(result ? "match" : "no match")}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG] Password check error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetUserNameAsync(string name)
        {
            Debug.WriteLine($"[LOG] Getting user name: {name}");
            return await _realService.GetUserNameAsync(name);
        }

        public async Task<string> GetAccountCreationDateAsync(string name)
        {
            Debug.WriteLine($"[LOG] Getting account creation date for: {name}");
            return await _realService.GetAccountCreationDateAsync(name);
        }

        public async Task<string> PromoteToAdminAsync(string login)
        {
            Debug.WriteLine($"[LOG] Promoting user to admin: {login}");
            try
            {
                var result = await _realService.PromoteToAdminAsync(login);
                Debug.WriteLine($"[LOG] Promotion result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG] Promotion error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ResetPasswordAsync(string login, string newPassword)
        {
            Debug.WriteLine($"[LOG] Resetting password for: {login}");
            try
            {
                var result = await _realService.ResetPasswordAsync(login, newPassword);
                Debug.WriteLine($"[LOG] Password reset result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOG] Password reset error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetLastActivityAsync(string login)
        {
            Debug.WriteLine($"[LOG] Getting last activity for: {login}");
            return await _realService.GetLastActivityAsync(login);
        }

        public async Task<string> GetUserRoleAsync(string login)
        {
            Debug.WriteLine($"[LOG] Getting user role for: {login}");
            return await _realService.GetUserRoleAsync(login);
        }
    }
}
