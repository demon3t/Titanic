using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication.Api;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Entity
{
    /// <summary>
    /// Методы расширения для регистрации Entity ORM в DI-контейнере.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Registration

        /// <summary>
        /// Инициализировать EntityManager из конфигурации приложения.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <param name="configuration"> Конфигурация приложения. </param>
        /// <param name="configSectionName"> Имя секции конфигурации. По умолчанию TitanicEntity. </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicEntity(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "TitanicEntity")
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var section = configuration.GetSection(configSectionName);
            var config = section.Get<EntityManagerConfig>() ?? new EntityManagerConfig();

            services.Configure<EntityManagerConfig>(section);
            EntityManager.Initialize(config);
            RegisterEntityApiServices(services);
            RegisterManagersInServices(services);

            return services;
        }

        /// <summary>
        /// Инициализировать EntityManager через делегат настройки.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <param name="configure"> Делегат настройки конфигурации. </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicEntity(
            this IServiceCollection services,
            Action<EntityManagerConfig> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var config = new EntityManagerConfig();
            configure(config);

            services.Configure<EntityManagerConfig>(options =>
            {
                options.Managers = config.Managers;
            });

            EntityManager.Initialize(config);
            RegisterEntityApiServices(services);
            RegisterManagersInServices(services);

            return services;
        }

        /// <summary>
        /// Инициализировать Entity ORM и зарегистрировать базовые сервисы Entity API.
        /// Endpoint-ы публикуются отдельно через MapTitanicEntityApi.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <param name="configuration"> Конфигурация приложения. </param>
        /// <param name="configSectionName"> Имя секции конфигурации. По умолчанию TitanicEntity. </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicEntityApi(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "TitanicEntity")
        {
            return services.AddTitanicEntity(configuration, configSectionName);
        }

        /// <summary>
        /// Инициализировать Entity ORM и зарегистрировать базовые сервисы Entity API через делегат настройки.
        /// Endpoint-ы публикуются отдельно через MapTitanicEntityApi.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <param name="configure"> Делегат настройки конфигурации. </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicEntityApi(
            this IServiceCollection services,
            Action<EntityManagerConfig> configure)
        {
            return services.AddTitanicEntity(configure);
        }

        #endregion Registration

        #region Private Methods

        /// <summary>
        /// Зарегистрировать базовые сервисы Entity API.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        private static void RegisterEntityApiServices(IServiceCollection services)
        {
            services.AddSingleton<HeaderEntityApiAuthorizationProvider>();
            services.AddSingleton<BaseEntityEventProvider, LocalEntityEventProvider>();
            services.AddSingleton<BaseEntityEventProvider, HttpEntityEventProvider>();
            services.AddSingleton<BaseEntityEventProvider, GrpcEntityEventProvider>();
            services.AddSingleton<IEntityEventHttpClientFactory, DefaultEntityEventHttpClientFactory>();
            services.AddSingleton<IEntityEventGrpcClientFactory, DefaultEntityEventGrpcClientFactory>();
        }

        /// <summary>
        /// Зарегистрировать текущие менеджеры EntityManager в DI-контейнере.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        private static void RegisterManagersInServices(IServiceCollection services)
        {
            foreach (var manager in EntityManager.GetManagers())
            {
                services.AddSingleton(manager.GetType(), manager);
                services.AddSingleton(typeof(BaseEntityManager), manager);
            }
        }

        #endregion Private Methods
    }
}
