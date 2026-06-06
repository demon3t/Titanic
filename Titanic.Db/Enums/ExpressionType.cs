namespace Titanic.Db.Enums
{
    /// <summary>
    /// Тип выражения.
    /// </summary>
    public enum ExpressionType
    {
        /// <summary>
        /// Колонка источника.
        /// </summary>
        SourceColumn = 0,

        /// <summary>
        /// Все колонки.
        /// </summary>
        Asterisk = 1,

        /// <summary>
        /// Параметр.
        /// </summary>
        Parameter = 2,

        /// <summary>
        /// Константа.
        /// </summary>
        Const = 3,

        /// <summary>
        /// SQL текст.
        /// </summary>
        SqlText = 4,

        /// <summary>
        /// Бинарное выражение.
        /// </summary>
        Binary = 5,

        /// <summary>
        /// Унарное выражение.
        /// </summary>
        Unary = 6,

        /// <summary>
        /// Группа выражений.
        /// </summary>
        Group = 7,

        /// <summary>
        /// Список выражений.
        /// </summary>
        List = 8,

        /// <summary>
        /// SQL функция.
        /// </summary>
        Function = 9,

        /// <summary>
        /// Подзапрос.
        /// </summary>
        SubQuery = 10,

        /// <summary>
        /// CASE выражение.
        /// </summary>
        Case = 11,
    }
}
