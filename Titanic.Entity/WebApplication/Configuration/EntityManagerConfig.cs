namespace Titanic.Entity.WebApplication.Configuration
{
    /// <summary>
    /// Конфигурация реестра Entity ORM менеджеров.
    /// </summary>
    public sealed class EntityManagerConfig
    {
        /// <summary>
        /// Список Entity ORM менеджеров.
        /// </summary>
        public List<EntityManagerSettings> Managers { get; set; } = [];
    }
}