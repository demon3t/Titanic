using Titanic.Db.Abstractions;
using Titanic.Db.Attributes;

namespace Titanic.Test.Db
{
    /// <summary>
    /// Обёртка подключения к PostgreSQL для тестов SQL builder.
    /// Регистрируется в <see cref="Titanic.Db.DbManager"/> автоматически
    /// по атрибуту <see cref="DatabaseConnectionAttribute"/> при первом вызове
    /// <c>DbManager.Get<PostgresDatabase>()</c>.
    /// </summary>
    [DatabaseConnection("test")]
    public sealed class PostgresDatabase : BaseDatabase
    {
    }
}
