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
        private DatabaseService _realService;

        // Incapsulated connect data string
        private readonly string _connectionString;

        // Duplicate the proterties for normal interface
        public string CurrentUser => _realService?.CurrentUser;
        public string CurrentRole => _realService?.CurrentRole;


        public DatabaseServiceProxy(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Вспомогательный метод ленивой инициализации
        private DatabaseService GetRealService()
        {
            if (_realService == null)
            {
                _realService = new DatabaseService(_connectionString);
            }
            return _realService;
        }


        //Ctor
        public string RegisterNewUser(string login, string password)
        {
            Debug.WriteLine($"[LOG] User reg try: {login}");
            return GetRealService().RegisterNewUser(login, password);
        }

        public bool AuthorizeUser(string login, string password)
        {
            bool isSuccess = GetRealService().AuthorizeUser(login, password);
            Debug.WriteLine($"[LOG] Authorize user '{login}': {(isSuccess ? "t" : "f")}");
            return isSuccess;
        }

        public string DeleteUser(string targetName)
        {
            Debug.WriteLine($"[LOG] Check perms to remove target: {targetName}...");

            var service = GetRealService();

            // Get perms to exec command
            if (service.CurrentRole != "Admin")
            {
                return "Error. You are not an Admin!";
            }

            return service.DeleteUser(targetName);
        }

        public bool CheckPassword(string password)
        {
            Debug.WriteLine($"[LOG] Get pwd for current target ({CurrentUser}).");
            return GetRealService().CheckPassword(password);
        }

        public string GetUserName(string name)
        {
            return GetRealService().GetUserName(name);
        }

        public string GetAccountCreationDate(string name)
        {
            return GetRealService().GetAccountCreationDate(name);
        }

    }
}
