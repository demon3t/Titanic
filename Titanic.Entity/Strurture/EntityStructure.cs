using Titanic.Entity.Exceptions;

namespace Titanic.Entity.Strurture
{
    /// <summary>
    /// Структура сущностей.
    /// </summary>
    internal class EntityStructure
    {
        /// <summary>
        /// CLR-тип сущности.
        /// </summary>
        public Type EntityType { get; set; } = null!;

        /// <summary>
        /// Название таблицы
        /// </summary>
        public string TableName { get; set; } = null!;

        /// <summary>
        /// Представление.
        /// </summary>
        public bool IsView { get; set; } = false;

        /// <summary>
        /// Принудительно отключить локализацию для всей сущности.
        /// </summary>
        public bool IsLocalizationDisabled { get; set; } = false;

        /// <summary>
        /// Структуры колонок.
        /// </summary>
        public List<ColumnStructure> ColumnsStructure { get; set; } = null!;

        /// <summary>
        /// Получить структуру колонки.
        /// </summary>
        /// <param name="columnName"> Название колонки. </param>
        /// <returns> Структура колонки. </returns>
        public ColumnStructure GetColumnStructure(string columnName)
        {
            var column = ColumnsStructure.FirstOrDefault(x => x.Matches(columnName));

            return column is null
                ? throw new NotExistColumnException(TableName, columnName)
                : column;
        }

        /// <summary>
        /// Получить структуру первичной колонки.
        /// </summary>
        /// <returns> Структура первичной колонки. </returns>
        public ColumnStructure GetPrimaryColumnStructure()
        {
            var column = ColumnsStructure.FirstOrDefault(x => x.IsPrimary);

            return column is null
                ? throw new NotExistColumnException(TableName, "Primary")
                : column;
        }

        /// <summary>
        /// Получить структуру отображаемой колонки.
        /// </summary>
        /// <returns> Структура отображаемой колонки. </returns>
        public ColumnStructure GetDisplayColumnStructure()
        {
            var column = ColumnsStructure.FirstOrDefault(x => x.IsDisplay);

            return column is null
                ? throw new NotExistColumnException(TableName, "Display")
                : column;
        }

        /// <summary>
        /// Получить структуры основных колонк.
        /// </summary>
        /// <returns> Структкры основных колонок. </returns>
        public IEnumerable<ColumnStructure> GetMainColumnStructure()
        {
            return ColumnsStructure.Where(x => x.IsPrimary || x.IsDisplay);
        }

        /// <summary>
        /// Проверить, есть ли в сущности колонка по CLR-имени или имени БД.
        /// </summary>
        public bool HasColumn(string columnName)
        {
            return ColumnsStructure.Any(x => x.Matches(columnName));
        }
    }
}
