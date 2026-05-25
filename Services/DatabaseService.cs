using System;
using System.Data;
// using System.Data.SqlClient; не подойдлёт

// driver import
using System.Data.Odbc;


namespace Holst.Services
{
    public class DatabaseService : IDatabaseService
    {

        private readonly string _connectionString;


        // Current User data n' role (equals null before the user write it by itself)
        public string CurrentUser { get; private set; } = null;
        public string CurrentRole { get; private set; } = null;

        // class ctor

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string RegisterNewUser(string login, string password)
        {
            // Используем RETURNING для получения даты, созданной на стороне базы данных
            string query = "INSERT INTO users (username, password, role) VALUES (?, ?, 'User') RETURNING created_at;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                conn.Open();
                object result = cmd.ExecuteScalar();
                return result != null ? result.ToString() : "Ошибка при регистрации";
            }
        }

        public bool AuthorizeUser(string login, string password)
        {
            string query = "SELECT role FROM users WHERE username = ? AND password = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                conn.Open();
                object roleResult = cmd.ExecuteScalar();

                if (roleResult != null)
                {
                    CurrentUser = login;
                    CurrentRole = roleResult.ToString();
                    return true;
                }
            }
            return false;
        }

        public string DeleteUser(string targetName)
        {
            string query = "DELETE FROM users WHERE username = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = targetName;

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0 ? $"Пользователь {targetName} успешно удален." : "Пользователь не найден.";
            }
        }

        public bool CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(CurrentUser)) return false;

            string query = "SELECT COUNT(1) FROM users WHERE username = ? AND password = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = CurrentUser;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                conn.Open();
                long count = Convert.ToInt64(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        public string GetUserName(string name)
        {
            string query = "SELECT username FROM users WHERE username = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = name;
                conn.Open();
                return cmd.ExecuteScalar()?.ToString() ?? "Не найден";
            }
        }

        public string GetAccountCreationDate(string name)
        {
            string query = "SELECT created_at FROM users WHERE username = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = name;
                conn.Open();
                return cmd.ExecuteScalar()?.ToString() ?? "Не найден";
            }
        }

    }
}
