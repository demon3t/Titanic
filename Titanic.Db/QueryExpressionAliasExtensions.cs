using Titanic.Db.Abstractions;

namespace Titanic.Db
{
    internal static class QueryExpressionAliasExtensions
    {
        /// <summary>
        /// Applies an alias when the alias is not empty.
        /// </summary>
        /// <param name="expression">The expression to alias.</param>
        /// <param name="alias">The optional alias.</param>
        /// <returns>The original expression or an aliased expression.</returns>
        public static QueryExpression AsIfNotEmpty(this QueryExpression expression, string? alias)
        {
            return string.IsNullOrWhiteSpace(alias) ? expression : expression.As(alias);
        }
    }
}
