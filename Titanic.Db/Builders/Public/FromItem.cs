namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер FROM. Позволяет задать алиас через As().
    /// </summary>
    public class FromItem
    {
        private readonly Select _select;
        private readonly string _tableName;

        internal FromItem(Select select, string tableName)
        {
            _select = select;
            _tableName = tableName;
        }

        /// <summary>
        /// Задать алиас для таблицы.
        /// </summary>
        public Select As(string alias)
        {
            return _select.SetFrom(_tableName, alias);
        }
    }
}