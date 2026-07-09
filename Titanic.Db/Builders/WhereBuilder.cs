using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер условий WHERE, HAVING и ON.
    /// </summary>
    /// <typeparam name="TParent">Тип родительского билдера.</typeparam>
    public class WhereBuilder<TParent>
    {
        #region Поля

        private readonly TParent _parent;
        private readonly Func<QueryExpression, TParent> _apply;
        private readonly Stack<(LogicalOperator Operator, LogicalOperator ParentOperator, List<QueryExpression> Items)> _groups = new();
        private QueryExpression? _current;
        private LogicalOperator _nextOperator = LogicalOperator.And;

        #endregion Поля

        #region Конструкторы

        /// <summary>
        /// Создать билдер условий и привязать callback, который применит готовое
        /// выражение к родительскому билдеру.
        /// </summary>
        /// <param name="parent">Родительский билдер, в который возвращается управление.</param>
        /// <param name="apply">Функция применения собранного выражения условий.</param>
        public WhereBuilder(TParent parent, Func<QueryExpression, TParent> apply)
        {
            _parent = parent;
            _apply = apply;
        }

        #endregion Конструкторы

        #region Операторы

        /// <summary>
        /// Добавить условие равенства для колонки без табличного алиаса.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение значения или колонки справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Equal(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.Equal, value));
        }

        /// <summary>
        /// Добавить условие равенства для колонки, заданной через табличный алиас.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение значения или колонки справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Equal(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.Equal, value));
        }

        /// <summary>
        /// Добавить условие неравенства для колонки без табличного алиаса.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение значения или колонки справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotEqual(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.NotEqual, value));
        }

        /// <summary>
        /// Добавить условие неравенства для колонки, заданной через табличный алиас.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение значения или колонки справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotEqual(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.NotEqual, value));
        }

        /// <summary>
        /// Добавить условие, в котором значение колонки должно быть больше заданного выражения.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение нижней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> GreaterThan(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.GreaterThan, value));
        }

        /// <summary>
        /// Добавить условие, в котором колонка с алиасом должна быть больше заданного выражения.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение нижней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> GreaterThan(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.GreaterThan, value));
        }

        /// <summary>
        /// Добавить условие, в котором значение колонки должно быть больше или равно заданному выражению.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение нижней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> GreaterThanOrEqual(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.GreaterThanOrEqual, value));
        }

        /// <summary>
        /// Добавить условие, в котором колонка с алиасом должна быть больше или равна заданному выражению.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение нижней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> GreaterThanOrEqual(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.GreaterThanOrEqual, value));
        }

        /// <summary>
        /// Добавить условие, в котором значение колонки должно быть меньше заданного выражения.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение верхней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> LessThan(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.LessThan, value));
        }

        /// <summary>
        /// Добавить условие, в котором колонка с алиасом должна быть меньше заданного выражения.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение верхней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> LessThan(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.LessThan, value));
        }

        /// <summary>
        /// Добавить условие, в котором значение колонки должно быть меньше или равно заданному выражению.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение верхней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> LessThanOrEqual(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.LessThanOrEqual, value));
        }

        /// <summary>
        /// Добавить условие, в котором колонка с алиасом должна быть меньше или равна заданному выражению.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, которая сравнивается со значением.</param>
        /// <param name="value">Выражение верхней границы справа от оператора.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> LessThanOrEqual(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.LessThanOrEqual, value));
        }

        /// <summary>
        /// Добавить условие попадания значения колонки в закрытый диапазон.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая должна попасть в диапазон.</param>
        /// <param name="from">Выражение нижней границы диапазона включительно.</param>
        /// <param name="to">Выражение верхней границы диапазона включительно.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Between(string columnName, QueryExpression from, QueryExpression to)
        {
            return Add(QueryExpression.And(
                Compare(columnName, ConditionOperator.GreaterThanOrEqual, from),
                Compare(columnName, ConditionOperator.LessThanOrEqual, to)));
        }

        /// <summary>
        /// Добавить условие выхода значения колонки за пределы закрытого диапазона.
        /// </summary>
        /// <param name="columnName">Имя колонки, которая проверяется на выход за пределы диапазона.</param>
        /// <param name="from">Выражение нижней границы диапазона.</param>
        /// <param name="to">Выражение верхней границы диапазона.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotBetween(string columnName, QueryExpression from, QueryExpression to)
        {
            return Add(QueryExpression.Or(
                Compare(columnName, ConditionOperator.LessThan, from),
                Compare(columnName, ConditionOperator.GreaterThan, to)));
        }

        /// <summary>
        /// Добавить условие вхождения значения колонки в список выражений.
        /// </summary>
        /// <param name="columnName">Имя колонки, значение которой проверяется по списку.</param>
        /// <param name="values">Список выражений, допустимых для оператора IN.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> In(string columnName, params QueryExpression[] values)
        {
            return Add(Compare(columnName, ConditionOperator.In, QueryExpression.List(values)));
        }

        /// <summary>
        /// Добавить условие вхождения значения колонки в результат подзапроса.
        /// </summary>
        /// <param name="columnName">Имя колонки, значение которой проверяется по подзапросу.</param>
        /// <param name="subQuery">Подзапрос, возвращающий допустимые значения.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> In(string columnName, BaseQuery subQuery)
        {
            return Add(QueryExpression.In(columnName, subQuery));
        }

        /// <summary>
        /// Добавить условие вхождения колонки с алиасом в результат подзапроса.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, значение которой проверяется по подзапросу.</param>
        /// <param name="subQuery">Подзапрос, возвращающий допустимые значения.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> In(string alias, string columnName, BaseQuery subQuery)
        {
            return Add(Compare(alias, columnName, ConditionOperator.In, QueryExpression.SubQuery(subQuery)));
        }

        /// <summary>
        /// Добавить условие отсутствия значения колонки в списке выражений.
        /// </summary>
        /// <param name="columnName">Имя колонки, значение которой проверяется по списку.</param>
        /// <param name="values">Список выражений, запрещённых для оператора NOT IN.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotIn(string columnName, params QueryExpression[] values)
        {
            return Add(Compare(columnName, ConditionOperator.NotIn, QueryExpression.List(values)));
        }

        /// <summary>
        /// Добавить условие отсутствия значения колонки в результате подзапроса.
        /// </summary>
        /// <param name="columnName">Имя колонки, значение которой проверяется по подзапросу.</param>
        /// <param name="subQuery">Подзапрос, возвращающий запрещённые значения.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotIn(string columnName, BaseQuery subQuery)
        {
            return Add(QueryExpression.NotIn(columnName, subQuery));
        }

        /// <summary>
        /// Добавить условие соответствия значения колонки шаблону LIKE.
        /// </summary>
        /// <param name="columnName">Имя колонки, значение которой сравнивается с шаблоном.</param>
        /// <param name="value">Выражение шаблона LIKE, включая нужные символы подстановки.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Like(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.Like, value));
        }

        /// <summary>
        /// Добавить условие соответствия колонки с алиасом шаблону LIKE.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, значение которой сравнивается с шаблоном.</param>
        /// <param name="value">Выражение шаблона LIKE, включая нужные символы подстановки.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Like(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.Like, value));
        }

        /// <summary>
        /// Добавить условие несоответствия значения колонки шаблону LIKE.
        /// </summary>
        /// <param name="columnName">Имя колонки, значение которой сравнивается с шаблоном.</param>
        /// <param name="value">Выражение шаблона LIKE, включая нужные символы подстановки.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotLike(string columnName, QueryExpression value)
        {
            return Add(Compare(columnName, ConditionOperator.NotLike, value));
        }

        /// <summary>
        /// Добавить условие несоответствия колонки с алиасом шаблону LIKE.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, значение которой сравнивается с шаблоном.</param>
        /// <param name="value">Выражение шаблона LIKE, включая нужные символы подстановки.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotLike(string alias, string columnName, QueryExpression value)
        {
            return Add(Compare(alias, columnName, ConditionOperator.NotLike, value));
        }

        /// <summary>
        /// Добавить условие, проверяющее колонку без алиаса на значение NULL.
        /// </summary>
        /// <param name="columnName">Имя колонки, для которой строится проверка IS NULL.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> IsNull(string columnName)
        {
            return Add(QueryExpression.IsNull(columnName));
        }

        /// <summary>
        /// Добавить условие, проверяющее колонку с алиасом на значение NULL.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки, для которой строится проверка IS NULL.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> IsNull(string alias, string columnName)
        {
            return Add(QueryExpression.IsNull(alias, columnName));
        }

        /// <summary>
        /// Добавить условие EXISTS, проверяющее наличие строк в подзапросе.
        /// </summary>
        /// <param name="subQuery">Подзапрос, наличие результата которого проверяется.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Exists(BaseQuery subQuery)
        {
            return Add(QueryExpression.Exists(subQuery));
        }

        /// <summary>
        /// Добавить условие NOT EXISTS, проверяющее отсутствие строк в подзапросе.
        /// </summary>
        /// <param name="subQuery">Подзапрос, отсутствие результата которого проверяется.</param>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> NotExists(BaseQuery subQuery)
        {
            return Add(QueryExpression.Not(QueryExpression.Exists(subQuery)));
        }

        #endregion Операторы

        #region Связки и скобки

        /// <summary>
        /// Указать, что следующее добавленное условие будет соединено с текущим выражением
        /// логическим оператором AND.
        /// </summary>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> And()
        {
            _nextOperator = LogicalOperator.And;
            return this;
        }

        /// <summary>
        /// Указать, что следующее добавленное условие будет соединено с текущим выражением
        /// логическим оператором OR.
        /// </summary>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        public WhereBuilder<TParent> Or()
        {
            _nextOperator = LogicalOperator.Or;
            return this;
        }

        /// <summary>
        /// Открыть новую группу условий, которая после закрытия будет соединена с текущим
        /// выражением через логический оператор AND.
        /// </summary>
        /// <returns>Текущий билдер условий для заполнения открытой группы.</returns>
        public WhereBuilder<TParent> AndOpen()
        {
            _groups.Push((LogicalOperator.And, _nextOperator, new List<QueryExpression>()));
            _nextOperator = LogicalOperator.And;
            return this;
        }

        /// <summary>
        /// Открыть новую группу условий, которая после закрытия будет соединена с текущим
        /// выражением через логический оператор OR.
        /// </summary>
        /// <returns>Текущий билдер условий для заполнения открытой группы.</returns>
        public WhereBuilder<TParent> OrOpen()
        {
            _groups.Push((LogicalOperator.Or, _nextOperator, new List<QueryExpression>()));
            _nextOperator = LogicalOperator.And;
            return this;
        }

        /// <summary>
        /// Закрыть последнюю открытую группу условий и добавить сформированное групповое
        /// выражение к текущему набору условий.
        /// </summary>
        /// <returns>Текущий билдер условий для продолжения цепочки вызовов.</returns>
        /// <exception cref="InvalidOperationException">Группа условий не была открыта.</exception>
        public WhereBuilder<TParent> Close()
        {
            if (_groups.Count == 0)
            {
                throw new InvalidOperationException("No opened condition group");
            }

            var group = _groups.Pop();
            var expression = group.Operator == LogicalOperator.Or
                ? QueryExpression.Or(group.Items.ToArray())
                : QueryExpression.And(group.Items.ToArray());

            Add(expression, group.ParentOperator);
            return this;
        }

        #endregion Связки и скобки

        #region Методы

        /// <summary>
        /// Завершить построение условий и вернуть родительский билдер.
        /// </summary>
        /// <returns>
        /// Родительский билдер после применения накопленного выражения либо исходный родитель,
        /// если условия не были добавлены.
        /// </returns>
        public TParent End()
        {
            while (_groups.Count > 0)
            {
                Close();
            }

            return _current == null ? _parent : _apply(_current);
        }

        /// <summary>
        /// Применить накопленные условия и вернуть родительский билдер.
        /// </summary>
        /// <returns>
        /// Родительский билдер после применения накопленного выражения либо исходный родитель,
        /// если условия не были добавлены.
        /// </returns>
        internal TParent Apply()
        {
            while (_groups.Count > 0)
            {
                Close();
            }

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

        private WhereBuilder<TParent> Add(QueryExpression expression, LogicalOperator? connector = null)
        {
            if (_groups.Count > 0)
            {
                _groups.Peek().Items.Add(expression);
            }
            else
            {
                var op = connector ?? _nextOperator;
                _current = _current == null
                    ? expression
                    : op == LogicalOperator.Or ? QueryExpression.Or(_current, expression) : QueryExpression.And(_current, expression);
            }

            _nextOperator = LogicalOperator.And;
            return this;
        }

        #endregion Методы
    }
}
