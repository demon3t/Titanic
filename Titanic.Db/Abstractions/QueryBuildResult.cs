namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Результат построения SQL запроса.
    /// </summary>
    /// <param name="Sql"> SQL текст. </param>
    /// <param name="Parameters"> Параметры запроса. </param>
    public sealed record QueryBuildResult(string Sql, IReadOnlyList<QueryParameter> Parameters);
}