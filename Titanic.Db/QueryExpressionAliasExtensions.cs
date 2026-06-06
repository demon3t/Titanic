using Titanic.Db.Abstractions;

namespace Titanic.Db
{
    internal static class QueryExpressionAliasExtensions
    {
        public static QueryExpression AsIfNotEmpty(this QueryExpression expression, string? alias)
        {
            return string.IsNullOrWhiteSpace(alias) ? expression : expression.As(alias);
        }
    }
}
