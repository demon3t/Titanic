namespace Titanic.Common.Session
{
    /// <summary>
    /// Контракт пользовательского контекста.
    /// </summary>
    public sealed class UserConnection
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Идентификатор контакта пользователя.
        /// </summary>
        public Guid? ContactId { get; set; }

        /// <summary>
        /// Идентификатор таймзоны пользователя.
        /// </summary>
        public string? TimeZoneId { get; set; }

        /// <summary>
        /// Роли пользователя.
        /// </summary>
        public HashSet<string> Roles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Культура пользователя.
        /// </summary>
        public UserCulture Culture { get; set; } = new();
    }
}
