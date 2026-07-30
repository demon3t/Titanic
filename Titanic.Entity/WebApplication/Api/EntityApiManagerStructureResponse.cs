namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель структуры Entity API менеджера.
    /// </summary>
    public sealed class EntityApiManagerStructureResponse
    {
        /// <summary>
        /// Доступные сущности менеджера.
        /// </summary>
        public List<EntityApiStructureEntityResponse> Entities { get; set; } = [];
    }
}
