using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Services
{
    public class DatabaseServiceProxy : IDatabaseService
    {
        // Incapsualte the DbService obj
        private DatabaseService _realService = null!;


        // Duplicate the proterties for normal interface
        public string? CurrentUser => _realService?.CurrentUser;
        public string? CurrentRole => _realService?.CurrentRole;

        public DatabaseServiceProxy() { }

        // Вспомогательный метод ленивой инициализации
        private DatabaseService GetRealService()
        {
            if (_realService == null)
            {
                _realService = new DatabaseService();
            }
            return _realService;
        }


        // Async API implementations
        public async System.Threading.Tasks.Task<string> RegisterNewUserAsync(string login, string password)
        {
            Debug.WriteLine($"[LOG] User reg try: {login}");
            var service = GetRealService();
            return await service.RegisterNewUserAsync(login, password);
        }

        public async System.Threading.Tasks.Task<bool> AuthorizeUserAsync(string login, string password)
        {
            var service = GetRealService();
            bool isSuccess = await service.AuthorizeUserAsync(login, password);
            Debug.WriteLine($"[LOG] Authorize user '{login}': {(isSuccess ? "t" : "f")}");
            return isSuccess;
        }

        public async System.Threading.Tasks.Task<string> DeleteUserAsync(string targetName)
        {
            Debug.WriteLine($"[LOG] Check perms to remove target: {targetName}...");
            var service = GetRealService();

            // Get perms to exec command
            if (service.CurrentRole != "Admin")
            {
                return "Error. You are not an Admin!";
            }

            return await service.DeleteUserAsync(targetName);
        }

        public async System.Threading.Tasks.Task<bool> CheckPasswordAsync(string password)
        {
            Debug.WriteLine($"[LOG] Get pwd for current target ({CurrentUser}).");
            var service = GetRealService();
            return await service.CheckPasswordAsync(password);
        }

        public async System.Threading.Tasks.Task<string> GetUserNameAsync(string name)
        {
            var service = GetRealService();
            return await service.GetUserNameAsync(name);
        }

        public async System.Threading.Tasks.Task<string> GetAccountCreationDateAsync(string name)
        {
            var service = GetRealService();
            return await service.GetAccountCreationDateAsync(name);
        }

        public async System.Threading.Tasks.Task<string> PromoteToAdminAsync(string login)
        {
            Debug.WriteLine($"[LOG] Promote to admin: {login}");
            var service = GetRealService();
            var result = await service.PromoteToAdminAsync(login);
            if (service.CurrentUser == login && (result.Contains("успешно", StringComparison.OrdinalIgnoreCase) || result.Contains("success", StringComparison.OrdinalIgnoreCase)))
            {
                service.CurrentRole = "Admin";
            }
            return result;
        }

        public async System.Threading.Tasks.Task<string> ResetPasswordAsync(string login, string newPassword)
        {
            Debug.WriteLine($"[LOG] Reset password for: {login}");
            var service = GetRealService();

            if (service.CurrentRole != "Admin")
            {
                return "Error. You are not an Admin!";
            }

            return await service.ResetPasswordAsync(login, newPassword);
        }

        public async System.Threading.Tasks.Task<string> GetLastActivityAsync(string login)
        {
            var service = GetRealService();
            return await service.GetLastActivityAsync(login);
        }

        public async System.Threading.Tasks.Task<string> GetUserRoleAsync(string login)
        {
            var service = GetRealService();
            return await service.GetUserRoleAsync(login);
        }

    }
}
