using Microsoft.Extensions.Configuration;
using Titanic.Db.Configuration;

namespace Titanic.Test
{
    /// <summary>
    /// Загружает тестовую конфигурацию с поддержкой локального override и переменных окружения.
    /// </summary>
    internal static class TestConfigurationLoader
    {
        private const string TestConnectionStringEnvironmentVariable = "TITANIC_TEST_CONNECTION_STRING";

        /// <summary>
        /// Загрузить секцию `TitanicDb` для тестов.
        /// </summary>
        public static DbConfig LoadDbConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false)
                .AddJsonFile("appsettings.Test.local.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var config = configuration.GetSection("TitanicDb").Get<DbConfig>()
                ?? throw new InvalidOperationException("TitanicDb section not found in test configuration.");

            var connectionString = Environment.GetEnvironmentVariable(TestConnectionStringEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return config;
            }

            foreach (var provider in config.Providers)
            {
                provider.ConnectionString = connectionString;
            }

            return config;
        }
    }
}
