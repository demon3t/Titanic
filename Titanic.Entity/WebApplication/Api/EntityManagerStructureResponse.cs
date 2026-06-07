namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель структуры Entity ORM менеджера.
    /// </summary>
    public sealed class EntityManagerStructureResponse
    {
        /// <summary>
        /// Имя менеджера.
        /// </summary>
        public string ManagerName { get; set; } = string.Empty;

        /// <summary>
        /// Namespace-patterns, которые ограничивают структуру менеджера.
        /// </summary>
        public List<string> NamespacePatterns { get; set; } = [];

        /// <summary>
        /// Доступные сущности менеджера.
        /// </summary>
        public List<EntityStructureResponse> Entities { get; set; } = [];
    }
}
