using Holst.Stores;
using Holst.ViewModels;
using System;

namespace Holst.Models
{
    public class Account
    {
        // Идентификатор аккаунта
        public Guid AccountID { get; set; }

        // Отображаемое имя пользователя
        public string Name { get; set; } = string.Empty;

        // Дата создания аккаунта
        public DateTime AccountCreationDate { get; set; }

        // Роль пользователя ("Admin" / "User")
        public string Role { get; set; } = "default";

        // Дата последнего входа
        public DateTime LastLoginDate { get; set; }

        //TODO: Сделать ссылку на User
        //Ссылка на объект User (если используется в приложении)
        //public User AccountHolder { get; set; }
    }
}
