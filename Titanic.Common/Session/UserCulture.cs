namespace Titanic.Common.Session
{
    /// <summary>
    /// Культура пользователя.
    /// </summary>
    public sealed class UserCulture
    {
        /// <summary>
        /// Идентификатор культуры.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название культуры.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
