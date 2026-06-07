using Microsoft.Extensions.DependencyInjection;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Фабрика провайдеров авторизации Entity API.
    /// </summary>
    internal sealed class EntityApiAuthorizationProviderFactory
    {
        /// <summary>
        /// Создать провайдер авторизации Entity API.
        /// </summary>
        /// <param name="services"> Провайдер сервисов. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="kind"> Тип провайдера. </param>
        /// <returns> Провайдер авторизации. </returns>
        public IEntityApiAuthorizationProvider CreateProvider(
            IServiceProvider services,
            BaseEntityManager manager,
            EntityApiAuthorizationProviderKind kind)
        {
            ArgumentNullException.ThrowIfNull(manager);

            return kind switch
            {
                EntityApiAuthorizationProviderKind.Default => CreateProvider(
                    services,
                    manager.Api.AuthorizationProviderType,
                    typeof(HeaderEntityApiAuthorizationProvider),
                    "Entity API authorization provider"),
                EntityApiAuthorizationProviderKind.Structure => CreateProvider(
                    services,
                    manager.Api.StructureAuthorizationProviderType,
                    typeof(AdminEntityStructureAuthorizationProvider),
                    "Entity structure authorization provider"),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported authorization provider kind.")
            };
        }

        /// <summary>
        /// Создать провайдер авторизации по имени типа или типу по умолчанию.
        /// </summary>
        private static IEntityApiAuthorizationProvider CreateProvider(
            IServiceProvider services,
            string? providerTypeName,
            Type defaultProviderType,
            string providerDescription)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(defaultProviderType);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerDescription);

            if (string.IsNullOrWhiteSpace(providerTypeName))
            {
                return (IEntityApiAuthorizationProvider)ActivatorUtilities.GetServiceOrCreateInstance(services, defaultProviderType);
            }

            var providerType = Type.GetType(providerTypeName)
                ?? throw new InvalidOperationException(
                    $"{providerDescription} '{providerTypeName}' not found.");
            if (!typeof(IEntityApiAuthorizationProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException(
                    $"{providerDescription} '{providerTypeName}' must implement {nameof(IEntityApiAuthorizationProvider)}.");
            }

            return (IEntityApiAuthorizationProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }
    }
}
