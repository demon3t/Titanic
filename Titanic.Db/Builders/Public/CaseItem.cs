using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Флюент-билдер для CASE выражений.
    /// Пример:
    /// Func.Case()
    ///     .When(Column.Name("status")).IsEqual(Column.Parameter(1)).Then(Column.Const("one"))
    ///     .Else(Column.Const("other"));
    /// </summary>
    public class CaseItem
    {
        private readonly List<(QueryExpression When, QueryExpression Then)> _branches = new();

        /// <summary>
        /// Начать WHEN-ветку с левого выражения сравнения.
        /// </summary>
        public CaseWhenItem When(QueryExpression left)
        {
            return new CaseWhenItem(this, left);
        }

        internal CaseItem AddBranch(QueryExpression whenExpression, QueryExpression thenExpression)
        {
            _branches.Add((whenExpression, thenExpression));
            return this;
        }

        /// <summary>
        /// Завершить CASE выражение веткой ELSE.
        /// </summary>
        public QueryExpression Else(QueryExpression elseExpression)
        {
            if (_branches.Count == 0)
            {
                throw new InvalidOperationException("CASE must contain at least one WHEN ... THEN branch");
            }

            return QueryExpression.Case(_branches, elseExpression);
        }
    }
}
