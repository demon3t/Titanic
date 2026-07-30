using Titanic.Entity.WebApplication.Api;

namespace Titanic.Entity.WebApplication.Configuration
{
    /// <summary>
    /// Настройки публикации HTTP API для Entity ORM менеджера.
    /// </summary>
    public sealed class EntityManagerApiSettings
    {
        #region Properties

        /// <summary>
        /// Признак автоматической публикации API endpoint для доступа к Entity ORM менеджеру.
        /// </summary>
        public bool AutoRegisterEndpoint { get; set; } = false;

        /// <summary>
        /// Базовый путь API для доступа к Entity ORM менеджеру.
        /// </summary>
        public string Path { get; set; } = "/entity";

        /// <summary>
        /// Имя HTTP-заголовка, из которого API будет читать ключ авторизации.
        /// </summary>
        public string AuthorizationHeaderName { get; set; } = "X-Entity-Key";

        /// <summary>
        /// Полное имя типа провайдера авторизации API.
        /// Тип должен реализовывать <see cref="IEntityApiAuthorizationProvider" />.
        /// </summary>
        public string? AuthorizationProviderType { get; set; }

        /// <summary>
        /// Режим обработки batch-запросов Entity API по умолчанию.
        /// </summary>
        public EntityApiBatchExecutionMode DefaultBatchExecutionMode { get; set; } =
            EntityApiBatchExecutionMode.Sequential;

        #endregion Properties
    }
}
