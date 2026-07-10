using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Этап выбора оператора сравнения для CASE WHEN.
    /// </summary>
    public class CaseWhenItem
    {
        private readonly CaseItem _caseItem;
        private readonly QueryExpression _left;

        internal CaseWhenItem(CaseItem caseItem, QueryExpression left)
        {
            _caseItem = caseItem;
            _left = left;
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой равенства левого выражения переданному выражению.
        /// </summary>
        /// <param name="right">Правое выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsEqual(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.Equal, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой равенства левого выражения переданному значению.
        /// </summary>
        /// <param name="value">Значение, которое будет добавлено в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsEqual(object? value)
        {
            return IsEqual(QueryExpression.Param(value));
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой неравенства левого выражения переданному выражению.
        /// </summary>
        /// <param name="right">Правое выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsNotEqual(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.NotEqual, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой неравенства левого выражения переданному значению.
        /// </summary>
        /// <param name="value">Значение, которое будет добавлено в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsNotEqual(object? value)
        {
            return IsNotEqual(QueryExpression.Param(value));
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение больше переданного выражения.
        /// </summary>
        /// <param name="right">Правое выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsGreaterThan(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.GreaterThan, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение больше переданного значения.
        /// </summary>
        /// <param name="value">Значение, которое будет добавлено в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsGreaterThan(object? value)
        {
            return IsGreaterThan(QueryExpression.Param(value));
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение больше или равно переданному выражению.
        /// </summary>
        /// <param name="right">Правое выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsGreaterOrEqual(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.GreaterThanOrEqual, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение больше или равно переданному значению.
        /// </summary>
        /// <param name="value">Значение, которое будет добавлено в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsGreaterOrEqual(object? value)
        {
            return IsGreaterOrEqual(QueryExpression.Param(value));
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение меньше переданного выражения.
        /// </summary>
        /// <param name="right">Правое выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsLess(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.LessThan, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение меньше переданного значения.
        /// </summary>
        /// <param name="value">Значение, которое будет добавлено в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsLess(object? value)
        {
            return IsLess(QueryExpression.Param(value));
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение меньше или равно переданному выражению.
        /// </summary>
        /// <param name="right">Правое выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsLessOrEqual(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.LessThanOrEqual, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение меньше или равно переданному значению.
        /// </summary>
        /// <param name="value">Значение, которое будет добавлено в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsLessOrEqual(object? value)
        {
            return IsLessOrEqual(QueryExpression.Param(value));
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение соответствует LIKE-выражению.
        /// </summary>
        /// <param name="right">Правое LIKE-выражение, с которым сравнивается левая часть CASE WHEN.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsLike(QueryExpression right)
        {
            return Build(Enums.ConditionOperator.Like, right);
        }

        /// <summary>
        /// Создать ветку CASE WHEN с проверкой, что левое выражение соответствует LIKE-шаблону.
        /// </summary>
        /// <param name="value">LIKE-шаблон, который будет добавлен в запрос как параметр сравнения.</param>
        /// <returns>Билдер THEN-части для текущей ветки CASE.</returns>
        public CaseThenItem IsLike(object? value)
        {
            return IsLike(QueryExpression.Param(value));
        }

        private CaseThenItem Build(Enums.ConditionOperator op, QueryExpression right)
        {
            var whenExpression = QueryExpression.Binary(_left, op, right);
            return new CaseThenItem(_caseItem, whenExpression);
        }
    }
}
