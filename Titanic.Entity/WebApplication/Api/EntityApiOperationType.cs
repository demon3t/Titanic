namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Тип операции Entity API.
    /// </summary>
    public enum EntityApiOperationType
    {
        /// <summary>
        /// Операция не задана.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Прочитать сущности через EntitySchemaQuery.
        /// </summary>
        Select = 1,

        /// <summary>
        /// Создать или обновить сущность через Entity.Save().
        /// </summary>
        Save = 2,

        /// <summary>
        /// Удалить сущность через Entity.Delete().
        /// </summary>
        Delete = 3
    }
}
