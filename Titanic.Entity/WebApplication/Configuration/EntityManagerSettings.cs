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
        /// Namespace-patterns CLR Entity-моделей, которые должен видеть этот менеджер.
        /// Если коллекция пустая, менеджер использует все найденные Entity-модели.
        /// </summary>
        public List<string> EntityModelNamespaces { get; set; } = [];

        /// <summary>
        /// Настройки публикации HTTP API для этого Entity ORM менеджера.
        /// </summary>
        public EntityManagerApiSettings Api { get; set; } = new();

        /// <summary>
        /// Runtime-опции Entity ORM менеджера.
        /// </summary>
        public EntityManagerOptions Options { get; set; } = new();

        /// <summary>
        /// Настройка обработчика событий для этого менеджера.
        /// Если значение пустое, событийный слой считается локальным и выполняется в том же приложении.
        /// Если значение задано, менеджер должен считать событийный слой внешним и использовать это значение как locator слушателя.
        /// </summary>
        public string? EventListener { get; set; }

        /// <summary>
        /// Настройки публикации API событийного слоя для этого менеджера.
        /// </summary>
        public EntityManagerEventListenerApiSettings EventListenerApi { get; set; } = new();

        /// <summary>
        /// Признак проверки структуры БД на наличие таблиц и колонок Entity-моделей при инициализации.
        /// </summary>
        public bool ValidateDatabaseSchemaOnCompile { get; set; } = false;

        #endregion Properties
    }
}
