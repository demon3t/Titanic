namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Универсальное описание параметра SQL запроса.
    /// </summary>
    /// <param name="Name"> Имя параметра без SQL-префикса. </param>
    /// <param name="Value"> Значение параметра. </param>
    public sealed record QueryParameter(string Name, object? Value);
}