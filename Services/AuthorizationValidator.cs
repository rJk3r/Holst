using System;

namespace Holst.Services
{
    /// <summary>
    /// Стандартная реализация IAuthorizationValidator.
    /// Проверяет права доступа на основе ролей пользователя.
    /// </summary>
    public class AuthorizationValidator : IAuthorizationValidator
    {
        private const string AdminRole = "Admin";
        private const string UserRole = "User";

        /// <summary>
        /// Проверяет, может ли пользователь удалять других пользователей.
        /// Только администраторы имеют право удаления.
        /// </summary>
        public bool CanDeleteUser(string? currentRole)
        {
            return IsAdmin(currentRole);
        }

        /// <summary>
        /// Проверяет, может ли пользователь сбрасывать пароли.
        /// Только администраторы имеют право сброса паролей.
        /// </summary>
        public bool CanResetPassword(string? currentRole)
        {
            return IsAdmin(currentRole);
        }

        /// <summary>
        /// Проверяет, может ли пользователь повышать других до администратора.
        /// Только администраторы имеют право повышения.
        /// </summary>
        public bool CanPromoteToAdmin(string? currentRole)
        {
            return IsAdmin(currentRole);
        }

        /// <summary>
        /// Проверяет, является ли пользователь администратором.
        /// </summary>
        public bool IsAdmin(string? currentRole)
        {
            return currentRole?.Equals(AdminRole, StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
