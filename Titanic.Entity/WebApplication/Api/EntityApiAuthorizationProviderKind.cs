namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Тип провайдера авторизации Entity API.
    /// </summary>
    internal enum EntityApiAuthorizationProviderKind
    {
        /// <summary>
        /// Провайдер для обычных Entity API endpoint-ов.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Провайдер для endpoint-а структуры менеджера.
        /// </summary>
        Structure = 1
    }
}
