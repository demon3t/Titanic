using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication.Api;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Entity
{
    public static class ServiceCollectionExtensions
    {
        #region Registration

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

        public static IServiceCollection AddTitanicEntityApi(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "TitanicEntity")
        {
            return services.AddTitanicEntity(configuration, configSectionName);
        }

        public static IServiceCollection AddTitanicEntityApi(
            this IServiceCollection services,
            Action<EntityManagerConfig> configure)
        {
            return services.AddTitanicEntity(configure);
        }

        #endregion Registration

        #region Private Methods

        private static void RegisterEntityApiServices(IServiceCollection services)
        {
            services.AddSingleton<EntityApiAuthorizationProviderFactory>();
        }

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
