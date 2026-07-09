using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер условий HAVING.
    /// </summary>
    /// <typeparam name="TParent">Тип родительского билдера.</typeparam>
    internal class HavingBuilder<TParent>
    {
        #region Поля

        private readonly TParent _parent;
        private readonly Func<QueryExpression, TParent> _apply;
        private QueryExpression? _current;
        private string _nextOperator = "AND";

        #endregion Поля

        #region Конструкторы

        /// <summary>
        /// Создать билдер HAVING и callback для применения готового выражения.
        /// </summary>
        /// <param name="parent">Родительский билдер или запрос, к которому будет применено HAVING-условие.</param>
        /// <param name="apply">Функция, добавляющая построенное выражение в родительский объект.</param>
        public HavingBuilder(TParent parent, Func<QueryExpression, TParent> apply)
        {
            _parent = parent;
            _apply = apply;
        }

        #endregion Конструкторы

        #region Операторы

        /// <summary>
        /// Добавить условие равенства для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> Equal(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.Equal, value));
        }

        /// <summary>
        /// Добавить условие равенства для HAVING-колонки с алиасом.
        /// </summary>
        /// <param name="alias">Алиас источника данных, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> Equal(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.Equal, value));
        }

        /// <summary>
        /// Добавить условие неравенства для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> NotEqual(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.NotEqual, value));
        }

        /// <summary>
        /// Добавить условие "больше" для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> GreaterThan(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.GreaterThan, value));
        }

        /// <summary>
        /// Добавить условие "больше" для HAVING-колонки с алиасом.
        /// </summary>
        /// <param name="alias">Алиас источника данных, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> GreaterThan(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.GreaterThan, value));
        }

        /// <summary>
        /// Добавить условие "больше или равно" для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> GreaterThanOrEqual(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.GreaterThanOrEqual, value));
        }

        /// <summary>
        /// Добавить условие "меньше" для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> LessThan(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.LessThan, value));
        }

        /// <summary>
        /// Добавить условие "меньше или равно" для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">Выражение, с которым сравнивается колонка.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> LessThanOrEqual(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.LessThanOrEqual, value));
        }

        /// <summary>
        /// Добавить условие LIKE для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <param name="value">LIKE-выражение или параметр с шаблоном сравнения.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> Like(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.Like, value));
        }

        /// <summary>
        /// Добавить условие IS NULL для HAVING-колонки.
        /// </summary>
        /// <param name="columnName">Имя колонки или агрегатного алиаса в HAVING.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> IsNull(string columnName)
        {
            return Add(QueryExpression.IsNull(columnName));
        }

        /// <summary>
        /// Добавить условие EXISTS с подзапросом.
        /// </summary>
        /// <param name="subQuery">Подзапрос, существование результата которого проверяется.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> Exists(BaseQuery subQuery)
        {
            return Add(QueryExpression.Exists(subQuery));
        }

        /// <summary>
        /// Добавить условие NOT EXISTS с подзапросом.
        /// </summary>
        /// <param name="subQuery">Подзапрос, отсутствие результата которого проверяется.</param>
        /// <returns>Текущий билдер HAVING для добавления следующих условий.</returns>
        public HavingBuilder<TParent> NotExists(BaseQuery subQuery)
        {
            return Add(QueryExpression.Not(QueryExpression.Exists(subQuery)));
        }

        #endregion Операторы

        #region Связки

        /// <summary>
        /// Задать AND для следующего HAVING-условия.
        /// </summary>
        /// <returns>Текущий билдер HAVING для добавления следующего условия.</returns>
        public HavingBuilder<TParent> And()
        {
            _nextOperator = "AND";
            return this;
        }

        /// <summary>
        /// Задать OR для следующего HAVING-условия.
        /// </summary>
        /// <returns>Текущий билдер HAVING для добавления следующего условия.</returns>
        public HavingBuilder<TParent> Or()
        {
            _nextOperator = "OR";
            return this;
        }

        #endregion Связки

        #region Методы

        /// <summary>
        /// Завершить построение HAVING и вернуть родительский билдер.
        /// </summary>
        /// <returns>Родительский билдер с применённым HAVING-условием.</returns>
        public TParent End()
        {
            return _current == null ? _parent : _apply(_current);
        }

        private static QueryExpression Compare(string columnName, ConditionOperator op, QueryExpression value)
        {
            return QueryExpression.Binary(QueryExpression.Column(columnName), op, value);
        }

        private static QueryExpression Compare(string alias, string columnName, ConditionOperator op, QueryExpression value)
        {
            return QueryExpression.Binary(QueryExpression.Column(alias, columnName), op, value);
        }

        private HavingBuilder<TParent> Add(QueryExpression expression)
        {
            var op = _nextOperator;
            _current = _current == null
                ? expression
                : op == "OR" ? QueryExpression.Or(_current, expression) : QueryExpression.And(_current, expression);

            _nextOperator = "AND";
            return this;
        }

        #endregion Методы
    }
}
