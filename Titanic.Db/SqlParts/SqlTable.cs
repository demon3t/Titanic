namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Таблица с алиасом.
    /// </summary>
    public sealed record SqlTable(string TableName, string? Alias);
}
