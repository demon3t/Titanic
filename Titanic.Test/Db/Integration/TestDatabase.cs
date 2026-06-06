using Titanic.Db.Abstractions;
using Titanic.Db.Attributes;

namespace Titanic.Test.Db.Integration
{
    /// <summary>
    /// Обёртка подключения к тестовой PostgreSQL БД.
    /// Регистрируется в <see cref="DbManager"/> по имени "test" через атрибут
    /// <see cref="DatabaseConnectionAttribute"/>.
    /// </summary>
    [DatabaseConnection("test")]
    public sealed class TestDatabase : BaseDatabase
    {
    }
}
