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
        /// Sets the result expression for the current CASE branch.
        /// </summary>
        /// <param name="thenExpression">The expression returned when the branch condition matches.</param>
        /// <returns>The parent CASE expression builder.</returns>
        public CaseItem Then(QueryExpression thenExpression)
        {
            return _caseItem.AddBranch(_whenExpression, thenExpression);
        }
    }
}
