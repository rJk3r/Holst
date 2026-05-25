using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Services
{
    public interface IDatabaseService
    {

        
        // Here we can get user and their role
        string CurrentUser { get; }
        string CurrentRole { get; }

        // Registration of new user by login/pass
        string RegisterNewUser(string login, string password);
        // We try to get user data from SQL and say ALL GOOD if data in SQL and on Client совпали
        bool AuthorizeUser(string login, string password);

        // Delete target user
        string DeleteUser(string targetName);

        //Check if password in DB match written on client
        bool CheckPassword(string password);

        // Get user name on SQL by name written on client, if match -> get result
        string GetUserName(string name);

        // Тут всё из названия понятно, получить дату создания аккаунта по имени
        string GetAccountCreationDate(string name);
    }
}
