using Titanic.Db.Abstractions;

namespace Titanic.Db.Interfaces
{
    /// <summary>
    /// Инткрфейс запроса.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Движок взаимодействия с БД.
        /// </summary>
        BaseDbEngine Engine { get; }

        /// <summary>
        /// Параметры последнего построенного SQL запроса.
        /// </summary>
        IReadOnlyList<QueryParameter> Parameters { get; }

        /// <summary>
        /// Сформировать SQL и параметры.
        /// </summary>
        QueryBuildResult Build();

        /// <summary>
        /// Получить SQL текст.
        /// </summary>
        string ToSql();
    }
}
