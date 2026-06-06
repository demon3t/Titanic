namespace Titanic.Db.Enums
{
    /// <summary>
    /// Тип унарного SQL-оператора.
    /// </summary>
    public enum UnaryOperator
    {
        /// <summary>
        /// Оператор не задан.
        /// </summary>
        None = 0,

        /// <summary>
        /// Логическое отрицание.
        /// </summary>
        Not = 1,

        /// <summary>
        /// Проверка существования подзапроса.
        /// </summary>
        Exists = 2,
    }
}
