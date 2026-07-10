using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Этап указания THEN выражения.
    /// </summary>
    public class CaseThenItem
    {
        private readonly CaseItem _caseItem;
        private readonly QueryExpression _whenExpression;

        internal CaseThenItem(CaseItem caseItem, QueryExpression whenExpression)
        {
            _caseItem = caseItem;
            _whenExpression = whenExpression;
        }

        /// <summary>
        /// Указать выражение, которое будет возвращено текущей веткой CASE при выполнении условия WHEN.
        /// </summary>
        /// <param name="thenExpression">Выражение результата для текущей ветки CASE.</param>
        /// <returns>Родительский билдер CASE, в котором можно добавить следующую ветку или завершить выражение.</returns>
        public CaseItem Then(QueryExpression thenExpression)
        {
            return _caseItem.AddBranch(_whenExpression, thenExpression);
        }
    }
}
