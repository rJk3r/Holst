using System;
using System.Data;
// using System.Data.SqlClient; не подойдлёт

// driver import
using System.Data.Odbc;


namespace Holst.Services
{
    public class DatabaseService : IDatabaseService
    {
        // Мусорная строка
        //private readonly string _connectionString= "Driver={PostgreSQL Unicode};Server=localhost;Port=5432;Database=HolstApplication;Uid=admin;Pwd=admin;";
        
        
        private readonly string _connectionString= "Driver={PostgreSQL ODBC Driver(UNICODE)};Server=localhost;Port=5432;Database=HolstApplication;UID=postgres;PWD=admin;\r\n";


        // Current User data n' role (equals null before the user write it by itself)
        public string? CurrentUser { get; private set; } = null;
        public string? CurrentRole { get; private set; } = null;

        // class ctor

        public DatabaseService()
        {
        }

        public async System.Threading.Tasks.Task<string> RegisterNewUserAsync(string login, string password)
        {
            // Используем RETURNING для получения даты, созданной на стороне базы данных
            string query = "INSERT INTO users (username, password, role) VALUES (?, ?, 'User') RETURNING created_at;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                await conn.OpenAsync();
                object result = await cmd.ExecuteScalarAsync();
                return result != null ? result.ToString() : "Ошибка при регистрации";
            }
        }

        public async System.Threading.Tasks.Task<bool> AuthorizeUserAsync(string login, string password)
        {
            string query = "SELECT role FROM users WHERE username = ? AND password = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                try
                {

                    await conn.OpenAsync();
                } catch
                {
                    return false;
                }

                object roleResult = await cmd.ExecuteScalarAsync();

                if (roleResult != null)
                {
                    CurrentUser = login;
                    CurrentRole = roleResult.ToString();
                    return true;
                }
            return false;
            } 
        }

        public async System.Threading.Tasks.Task<string> DeleteUserAsync(string targetName)
        {
            string query = "DELETE FROM users WHERE username = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = targetName;

                await conn.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0 ? $"Пользователь {targetName} успешно удален." : "Пользователь не найден.";
            }
        }

        public async System.Threading.Tasks.Task<bool> CheckPasswordAsync(string password)
        {
            if (string.IsNullOrEmpty(CurrentUser)) return false;

            string query = "SELECT COUNT(1) FROM users WHERE username = ? AND password = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = CurrentUser;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                await conn.OpenAsync();
                object scalar = await cmd.ExecuteScalarAsync();
                long count = Convert.ToInt64(scalar);
                return count > 0;
            }
        }

        public async System.Threading.Tasks.Task<string> GetUserNameAsync(string name)
        {
            string query = "SELECT username FROM users WHERE username = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = name;
                await conn.OpenAsync();
                object res = await cmd.ExecuteScalarAsync();
                return res?.ToString() ?? "Не найден";
            }
        }

        public async System.Threading.Tasks.Task<string> GetAccountCreationDateAsync(string name)
        {
            string query = "SELECT created_at FROM users WHERE username = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = name;
                await conn.OpenAsync();
                object res = await cmd.ExecuteScalarAsync();
                return res?.ToString() ?? "Не найден";
            }
        }

    }
}
