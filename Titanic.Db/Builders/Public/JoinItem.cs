using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер для построения JOIN-соединения.
    /// Возвращается методами InnerJoin, LeftJoin и т.д.
    /// </summary>
    public class JoinItem
    {
        private readonly Select _select;
        private readonly string _tableName;
        private readonly Enums.JoinType _joinType;
        private string? _alias;

        /// <summary>
        /// Конструктор JOIN-билдера (внутренний).
        /// </summary>
        /// <param name="select">Родительский SELECT.</param>
        /// <param name="tableName">Имя присоединяемой таблицы.</param>
        /// <param name="joinType">Тип JOIN.</param>
        internal JoinItem(Select select, string tableName, Enums.JoinType joinType)
        {
            _select = select;
            _tableName = tableName;
            _joinType = joinType;
        }

        /// <summary>
        /// Задать алиас присоединённой таблицы.
        /// </summary>
        public JoinItem As(string alias)
        {
            _alias = alias;
            return this;
        }

        /// <summary>
        /// Установить колонку ON — возвращает WhereItem для цепочки IsEqual и т.д.
        /// </summary>
        public WhereItem<Select> On(string leftAlias, string leftColumn)
        {
            return new WhereItem<Select>(_select, leftAlias, leftColumn,
                expr => { _select.AddJoin(_tableName, expr, _alias, _joinType); return _select; });
        }

        /// <summary>
        /// Установить условие соединения и завершить контекст JOIN.
        /// </summary>
        public Select On(QueryExpression on)
        {
            _select.AddJoin(_tableName, on, _alias, _joinType);
            return _select;
        }
    }
}