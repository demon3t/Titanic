namespace Titanic.Common.Session
{
    /// <summary>
    /// Базовый контракт пользовательского контекста.
    /// </summary>
    public class UserConnection
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Культура пользователя.
        /// </summary>
        public UserCulture Culture { get; set; } = new();
    }
}
