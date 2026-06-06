using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Внутренний generic билдер JOIN-соединения.
    /// </summary>
    /// <typeparam name="TParent"> Тип родительского билдера. </typeparam>
    internal class JoinBuilder<TParent>
    {
        private readonly TParent _parent;
        private readonly string _tableName;
        private string? _alias;
        private readonly string _joinType;
        private readonly Func<string, string?, QueryExpression?, string, TParent> _apply;

        internal JoinBuilder(TParent parent, string tableName, string joinType, Func<string, string?, QueryExpression?, string, TParent> apply)
        {
            _parent = parent;
            _tableName = tableName;
            _joinType = joinType;
            _apply = apply;
        }

        /// <summary>
        /// Задать алиас присоединённой таблицы.
        /// </summary>
        public JoinBuilder<TParent> As(string alias)
        {
            _alias = alias;
            return this;
        }

        /// <summary>
        /// Установить условие соединения и завершить контекст JOIN.
        /// </summary>
        public TParent On(QueryExpression on)
        {
            return _apply(_tableName, _alias, on, _joinType);
        }
    }
}