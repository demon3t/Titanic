using Microsoft.Extensions.Configuration;
using Titanic.Db;
using Titanic.Db.Configuration;

namespace Titanic.Test.Db
{
    /// <summary>
    /// xUnit-фикстура: инициализирует статический <see cref="DbManager"/> из <c>appsettings.Test.json</c>
    /// для тестов SQL builder. Используется через <c>IClassFixture<DbManagerFixture></c>.
    /// После инициализации обёртки доступны через <c>DbManager.Get<T>()</c>.
    /// </summary>
    public sealed class DbManagerFixture
    {
        public DbManagerFixture()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();

            var config = configuration.GetSection("TitanicDb").Get<DbConfig>()
                ?? throw new InvalidOperationException("TitanicDb section not found in appsettings.Test.json");

            DbManager.Initialize(config);
        }
    }
}
