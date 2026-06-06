namespace Titanic.Common.Session
{
    /// <summary>
    /// Контекст подключения.
    /// </summary>
    public sealed class UserConnection
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
