namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Режим обработки batch-запроса Entity API.
    /// </summary>
    public enum EntityApiBatchExecutionMode
    {
        /// <summary>
        /// Выполнять операции по порядку.
        /// </summary>
        Sequential = 0,

        /// <summary>
        /// Выполнять операции параллельно.
        /// </summary>
        Parallel = 1
    }
}
