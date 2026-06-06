using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Titanic.Db.Abstractions;
using Titanic.Db.Configuration;

namespace Titanic.Db.WebApplication
{
    /// <summary>
    /// Методы расширения для регистрации БД в WebApplicationBuilder.
    /// </summary>
    public static class WebAppDbExtensions
    {
        /// <summary>
        /// Зарегистрировать БД из конфигурации приложения (appsettings.json).
        /// Инициализирует статический <see cref="DbManager"/> (singleton на уровне AppDomain)
        /// и регистрирует <see cref="IDbReader"/> как singleton.
        /// </summary>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="configSectionName"> Имя секции конфигурации. По умолчанию "TitanicDb". </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
        public static WebApplicationBuilder AddTitanicDb(
            this WebApplicationBuilder builder,
            string configSectionName = "TitanicDb")
        {
            ArgumentNullException.ThrowIfNull(builder);

            var section = builder.Configuration.GetSection(configSectionName);
            var config = section.Get<DbConfig>() ?? new DbConfig();

            builder.Services.Configure<DbConfig>(section);
            DbManager.Initialize(config);
            builder.Services.TryAddSingleton<IDbReader, DbReader>();

            return builder;
        }

        /// <summary>
        /// Зарегистрировать БД с настройкой через делегат.
        /// </summary>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="configure"> Делегат настройки. </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
        public static WebApplicationBuilder AddTitanicDb(
            this WebApplicationBuilder builder,
            Action<DbConfig> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var config = new DbConfig();
            configure(config);

            builder.Services.Configure<DbConfig>(options =>
            {
                options.DefaultProviderName = config.DefaultProviderName;
                options.Providers = config.Providers;
            });

            DbManager.Initialize(config);
            builder.Services.TryAddSingleton<IDbReader, DbReader>();

            return builder;
        }
    }
}
