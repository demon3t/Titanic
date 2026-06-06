using Titanic.Db;

namespace Titanic.Test.Db
{
    /// <summary>
    /// xUnit-фикстура для SQL builder тестов.
    /// Инициализирует статический <see cref="DbManager"/> из тестовой конфигурации.
    /// </summary>
    public sealed class DbManagerFixture
    {
        public DbManagerFixture()
        {
            var config = TestConfigurationLoader.LoadDbConfig();
            DbManager.Initialize(config);
        }
    }
}
