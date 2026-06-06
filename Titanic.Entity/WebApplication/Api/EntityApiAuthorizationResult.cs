using Titanic.Common.Session;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Результат проверки авторизации Entity API.
    /// </summary>
    /// <param name="IsAuthorized"> Признак успешной авторизации. </param>
    /// <param name="UserConnection"> Контекст пользователя. </param>
    /// <param name="ErrorMessage"> Текст ошибки авторизации. </param>
    public sealed record EntityApiAuthorizationResult(
        bool IsAuthorized,
        UserConnection? UserConnection,
        string? ErrorMessage = null)
    {
        /// <summary>
        /// Создать успешный результат авторизации.
        /// </summary>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Результат авторизации. </returns>
        public static EntityApiAuthorizationResult Success(UserConnection userConnection)
        {
            return new EntityApiAuthorizationResult(true, userConnection);
        }

        /// <summary>
        /// Создать неуспешный результат авторизации.
        /// </summary>
        /// <param name="message"> Текст ошибки. </param>
        /// <returns> Результат авторизации. </returns>
        public static EntityApiAuthorizationResult Fail(string message)
        {
            return new EntityApiAuthorizationResult(false, null, message);
        }
    }
}
