namespace Titanic.Entity.WebApplication.Configuration
{
    /// <summary>
    /// Настройки инициализации Entity ORM менеджера.
    /// </summary>
    public class EntityManagerSettings
    {
        #region Properties

        /// <summary>
        /// Имя Entity ORM менеджера в конфигурации.
        /// Реестр менеджеров использует runtime-тип менеджера, а не это имя.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Имя провайдера в <c>DbManager</c>, который будет использовать Entity ORM менеджер.
        /// </summary>
        public string DbProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Полное имя типа обертки менеджера. Если не задано, используется стандартный <c>EntityDbManager</c>.
        /// </summary>
        public string? ManagerType { get; set; }

        /// <summary>
        /// Настройки публикации HTTP API для этого Entity ORM менеджера.
        /// </summary>
        public EntityManagerApiSettings Api { get; set; } = new();

        /// <summary>
        /// Runtime-опции Entity ORM менеджера.
        /// </summary>
        public EntityManagerOptions Options { get; set; } = new();

        /// <summary>
        /// Признак проверки структуры БД на наличие таблиц и колонок Entity-моделей при инициализации.
        /// </summary>
        public bool ValidateDatabaseSchemaOnCompile { get; set; } = false;

        #endregion Properties
    }
}
