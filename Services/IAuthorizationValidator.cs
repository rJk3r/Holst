namespace Holst.Services
{
    /// <summary>
    /// Интерфейс для проверки авторизации и прав доступа.
    /// Использует Strategy Pattern для различных правил проверки.
    /// </summary>
    public interface IAuthorizationValidator
    {
        /// <summary>
        /// Проверяет, может ли пользователь с заданной ролью выполнить операцию удаления.
        /// </summary>
        bool CanDeleteUser(string? currentRole);

        /// <summary>
        /// Проверяет, может ли пользователь с заданной ролью сбросить пароль другого пользователя.
        /// </summary>
        bool CanResetPassword(string? currentRole);

        /// <summary>
        /// Проверяет, может ли пользователь с заданной ролью повысить другого пользователя до администратора.
        /// </summary>
        bool CanPromoteToAdmin(string? currentRole);

        /// <summary>
        /// Проверяет, может ли пользователь с заданной ролью выполнять операции администратора.
        /// </summary>
        bool IsAdmin(string? currentRole);
    }
}
