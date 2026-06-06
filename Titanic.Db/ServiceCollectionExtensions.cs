using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Titanic.Db.Abstractions;
using Titanic.Db.Configuration;

namespace Titanic.Db
{
    /// <summary>
    /// Методы расширения для регистрации БД в DI-контейнере.
    /// Используйте для WebApplicationBuilder — <see cref="WebApplication.WebAppDbExtensions"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Зарегистрировать БД из конфигурации приложения.
        /// Инициализирует статический <see cref="DbManager"/> (singleton на уровне AppDomain)
        /// и регистрирует <see cref="IDbReader"/> как singleton.
        /// Подходит для тестов и консольных приложений.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <param name="configuration"> Конфигурация приложения. </param>
        /// <param name="configSectionName"> Имя секции конфигурации. По умолчанию "TitanicDb". </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicDb(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "TitanicDb")
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var section = configuration.GetSection(configSectionName);
            var config = section.Get<DbConfig>() ?? new DbConfig();

            services.Configure<DbConfig>(section);
            DbManager.Initialize(config);
            services.TryAddSingleton<IDbReader, DbReader>();

            return services;
        }

        /// <summary>
        /// Зарегистрировать БД с настройкой через делегат <see cref="DbConfig"/>.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <param name="configure"> Делегат настройки конфигурации. </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicDb(
            this IServiceCollection services,
            Action<DbConfig> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var config = new DbConfig();
            configure(config);

            DbManager.Initialize(config);
            services.TryAddSingleton<IDbReader, DbReader>();

            return services;
        }

        /// <summary>
        /// Инициализировать статический <see cref="DbManager"/> из конфигурации.
        /// Базовый метод для тестов и консольных приложений (без DI).
        /// </summary>
        /// <param name="configuration"> Конфигурация. </param>
        /// <param name="configSectionName"> Имя секции. По умолчанию "TitanicDb". </param>
        public static void InitializeDbManager(
            IConfiguration configuration,
            string configSectionName = "TitanicDb")
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var config = configuration.GetSection(configSectionName).Get<DbConfig>() ?? new DbConfig();
            DbManager.Initialize(config);
        }
    }
}
