using System;
using System.Data;
using System.Diagnostics;
// using System.Data.SqlClient; не подойдлёт

// driver import
using System.Data.Odbc;

namespace Holst.Services
{
    /// <summary>
    /// Сеанс пользователя для отслеживания контекста в DatabaseService.
    /// Инкапсулирует информацию о текущем пользователе и его роли.
    /// </summary>
    public class UserSession
    {
        public string? CurrentUser { get; set; }
        public string? CurrentRole { get; set; }
    }

    /// <summary>
    /// Сервис базы данных с поддержкой сеансов.
    /// Удалены статические поля — контекст пользователя передаётся через UserSession.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        // Если установлен psqlODBC — попробуйте варианты:
        //   Driver={PostgreSQL Unicode};
        //   Driver={PostgreSQL ANSI};
        //   Driver={PostgreSQL ODBC Driver(UNICODE)};
        // Для x64-драйвера обычно используется "PostgreSQL Unicode".
        private readonly string _connectionString = "Driver={PostgreSQL Unicode};Server=127.0.0.1;Port=5432;Database=HolstApplication;UID=postgres;PWD=admin;";

        /// <summary>
        /// Текущий сеанс пользователя. Может быть null, если пользователь не авторизован.
        /// </summary>
        private UserSession? _currentSession;

        public string? CurrentUser { get => _currentSession?.CurrentUser; }
        public string? CurrentRole { get => _currentSession?.CurrentRole; }

        public DatabaseService()
        {
            _currentSession = new UserSession();
        }

        /// <summary>
        /// Устанавливает контекст сеанса пользователя.
        /// </summary>
        public void SetUserSession(UserSession session)
        {
            _currentSession = session ?? new UserSession();
        }

        /// <summary>
        /// Получает текущий сеанс пользователя.
        /// </summary>
        public UserSession GetUserSession()
        {
            return _currentSession ?? new UserSession();
        }

        /// <summary>
        /// Устанавливает роль текущего пользователя (для тестирования и управления сеансом).
        /// </summary>
        public void SetCurrentRole(string? role)
        {
            if (_currentSession != null)
            {
                _currentSession.CurrentRole = role;
            }
        }

        public async System.Threading.Tasks.Task<string> RegisterNewUserAsync(string login, string password)
        {
            // Генерируем userid на стороне приложения, чтобы не нарушить NOT NULL
            string newUserId = Guid.NewGuid().ToString();
            string query = "INSERT INTO users (userid, login, password, role, registrationdate) VALUES (?, ?, ?, 'User', CURRENT_TIMESTAMP) RETURNING registrationdate;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = newUserId;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] RegisterNewUserAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    object result = await cmd.ExecuteScalarAsync();
                    return result != null ? result.ToString() : "Ошибка при регистрации";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] RegisterNewUserAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<bool> AuthorizeUserAsync(string login, string password)
        {
            string query = "SELECT role FROM users WHERE login = ? AND password = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] AuthorizeUserAsync connection error: {ex.Message}");
                    return false;
                }

                try
                {
                    object roleResult = await cmd.ExecuteScalarAsync();
                    if (roleResult != null)
                    {
                        // Обновляем сеанс пользователя
                        if (_currentSession != null)
                        {
                            _currentSession.CurrentUser = login;
                            _currentSession.CurrentRole = roleResult.ToString();
                        }

                        // Update last activity timestamp
                        string updateQuery = "UPDATE users SET lastactivity = CURRENT_TIMESTAMP WHERE login = ?;";
                        using (OdbcConnection updateConn = new OdbcConnection(_connectionString))
                        using (OdbcCommand updateCmd = new OdbcCommand(updateQuery, updateConn))
                        {
                            updateCmd.Parameters.Add("?", OdbcType.VarChar).Value = login;
                            await updateConn.OpenAsync();
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] AuthorizeUserAsync execution error: {ex.Message}");
                }
                return false;
            }
        }

        public async System.Threading.Tasks.Task<string> DeleteUserAsync(string targetName)
        {
            string query = "DELETE FROM users WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = targetName;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] DeleteUserAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0 ? $"Пользователь {targetName} успешно удален." : "Пользователь не найден.";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] DeleteUserAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<bool> CheckPasswordAsync(string password)
        {
            if (string.IsNullOrEmpty(CurrentUser)) return false;

            string query = "SELECT COUNT(1) FROM users WHERE login = ? AND password = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = CurrentUser;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = password;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] CheckPasswordAsync connection error: {ex.Message}");
                    return false;
                }

                try
                {
                    object scalar = await cmd.ExecuteScalarAsync();
                    long count = Convert.ToInt64(scalar);
                    return count > 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] CheckPasswordAsync execution error: {ex.Message}");
                    return false;
                }
            }
        }

        public async System.Threading.Tasks.Task<string> GetUserNameAsync(string login)
        {
            string query = "SELECT login FROM users WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetUserNameAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    object res = await cmd.ExecuteScalarAsync();
                    return res?.ToString() ?? "Не найден";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetUserNameAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<string> GetAccountCreationDateAsync(string name)
        {
            string query = "SELECT registrationdate FROM users WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = name;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetAccountCreationDateAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    object res = await cmd.ExecuteScalarAsync();
                    return res?.ToString() ?? "Не найден";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetAccountCreationDateAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<string> PromoteToAdminAsync(string login)
        {
            string query = "UPDATE users SET role = 'Admin' WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] PromoteToAdminAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0
                        ? $"Пользователь {login} успешно назначен администратором."
                        : "Пользователь не найден.";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] PromoteToAdminAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<string> ResetPasswordAsync(string login, string newPassword)
        {
            string query = "UPDATE users SET password = ? WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = newPassword;
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] ResetPasswordAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0
                        ? $"Пароль пользователя {login} сброшен."
                        : "Пользователь не найден.";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] ResetPasswordAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<string> GetLastActivityAsync(string login)
        {
            string query = "SELECT lastactivity FROM users WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetLastActivityAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    object res = await cmd.ExecuteScalarAsync();
                    return res?.ToString() ?? "Не найден";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetLastActivityAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

        public async System.Threading.Tasks.Task<string> GetUserRoleAsync(string login)
        {
            string query = "SELECT role FROM users WHERE login = ?;";

            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            using (OdbcCommand cmd = new OdbcCommand(query, conn))
            {
                cmd.Parameters.Add("?", OdbcType.VarChar).Value = login;

                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetUserRoleAsync connection error: {ex.Message}");
                    return $"Ошибка подключения: {ex.Message}";
                }

                try
                {
                    object res = await cmd.ExecuteScalarAsync();
                    return res?.ToString() ?? "Не найден";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DB] GetUserRoleAsync execution error: {ex.Message}");
                    return $"Ошибка выполнения: {ex.Message}";
                }
            }
        }

    }
}
