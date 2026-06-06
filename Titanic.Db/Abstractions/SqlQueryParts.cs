namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Колонка SELECT.
    /// </summary>
    public sealed record SqlSelectColumn(QueryExpression Expression);

    /// <summary>
    /// JOIN часть запроса.
    /// </summary>
    public sealed record SqlJoin(JoinType JoinType, string TableName, string? Alias, QueryExpression? On);

    /// <summary>
    /// ORDER BY часть запроса.
    /// </summary>
    public sealed record SqlOrderBy(QueryExpression Expression, bool Desc);

    /// <summary>
    /// UNION часть запроса.
    /// </summary>
    public sealed record SqlUnion(BaseQuery Query, bool All);

    /// <summary>
    /// Данные SELECT запроса для SQL-движка.
    /// </summary>
    public sealed record SqlSelectParts(
        IReadOnlyList<SqlSelectColumn> Columns,
        string? From,
        string? FromAlias,
        IReadOnlyList<SqlJoin> Joins,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> GroupBy,
        QueryExpression? Having,
        IReadOnlyList<SqlOrderBy> OrderBy,
        int? Limit,
        int? Offset,
        bool Distinct,
        IReadOnlyList<SqlUnion> Unions);

    /// <summary>
    /// SET часть UPDATE запроса.
    /// </summary>
    public sealed record SqlSet(QueryExpression Column, QueryExpression Value);

    /// <summary>
    /// Таблица с алиасом.
    /// </summary>
    public sealed record SqlTable(string TableName, string? Alias);

    /// <summary>
    /// Данные UPDATE запроса для SQL-движка.
    /// </summary>
    public sealed record SqlUpdateParts(
        string TableName,
        string? Alias,
        IReadOnlyList<SqlSet> Set,
        IReadOnlyList<SqlTable> From,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> Returning);

    /// <summary>
    /// Данные DELETE запроса для SQL-движка.
    /// </summary>
    public sealed record SqlDeleteParts(
        string TableName,
        string? Alias,
        IReadOnlyList<SqlTable> Using,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> Returning);

    /// <summary>
    /// Данные INSERT запроса для SQL-движка.
    /// </summary>
    public sealed record SqlInsertParts(
        string TableName,
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<QueryExpression>> Rows,
        BaseQuery? Select,
        IReadOnlyList<QueryExpression> Returning,
        bool OnConflictDoNothing,
        IReadOnlyList<string> ConflictColumns,
        IReadOnlyList<SqlSet> ConflictUpdateSet,
        string? ConflictSql);
}