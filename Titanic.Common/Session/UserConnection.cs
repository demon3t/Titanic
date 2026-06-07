namespace Titanic.Common.Session
{
    /// <summary>
    /// Контекст подключения.
    /// </summary>
    public sealed class UserConnection
    {
        /// <summary>
        /// Имя роли администратора.
        /// </summary>
        public const string AdministratorRole = "Admin";

        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Контакт пользователя.
        /// </summary>
        public string? Contact { get; set; }

        /// <summary>
        /// Идентификатор таймзоны пользователя.
        /// </summary>
        public string? TimeZoneId { get; set; }

        /// <summary>
        /// Роли пользователя.
        /// </summary>
        public HashSet<string> Roles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Признак администратора.
        /// Администратор может вызывать служебные endpoint-ы Entity ORM.
        /// </summary>
        public bool IsAdmin => Roles.Contains(AdministratorRole);

        /// <summary>
        /// Культура пользователя.
        /// </summary>
        public UserCulture Culture { get; set; } = new();

        /// <summary>
        /// Проверить наличие роли у пользователя.
        /// </summary>
        /// <param name="roleName"> Имя роли. </param>
        /// <returns> <c>true</c>, если роль есть у пользователя. </returns>
        public bool HasRole(string roleName)
        {
            return !string.IsNullOrWhiteSpace(roleName) && Roles.Contains(roleName);
        }
    }
}
