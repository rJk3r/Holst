using System;
using System.Data;

namespace Holst.DatabaseProxy
{
    public class DatabaseService : IDatabase
    {
        bool isDbConnected = true;
        // TODO: Logic

        private bool databaseConnect()
        {
            if (isDbConnected == true)
                return true;
            else return false;
        }

        public void GetUserName(string name)
        {
            
        }

    }
}
