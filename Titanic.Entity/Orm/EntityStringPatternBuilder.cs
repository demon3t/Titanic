namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Построитель шаблонов строковых фильтров Entity ORM.
    /// </summary>
    internal static class EntityStringPatternBuilder
    {
        /// <summary>
        /// Построить шаблон поиска по вхождению.
        /// </summary>
        public static string Contains(object? value) => $"%{value}%";

        /// <summary>
        /// Построить шаблон поиска по началу строки.
        /// </summary>
        public static string StartsWith(object? value) => $"{value}%";

        /// <summary>
        /// Построить шаблон поиска по концу строки.
        /// </summary>
        public static string EndsWith(object? value) => $"%{value}";
    }
}
