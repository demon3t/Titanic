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
        /// Создать провайдер авторизации для обычных Entity API endpoint-ов.
        /// </summary>
        public IEntityApiAuthorizationProvider CreateApiProvider(IServiceProvider services, BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(manager);

            if (string.IsNullOrWhiteSpace(manager.Api.AuthorizationProviderType))
            {
                return services.GetRequiredService<HeaderEntityApiAuthorizationProvider>();
            }

            var providerType = Type.GetType(manager.Api.AuthorizationProviderType)
                ?? throw new InvalidOperationException(
                    $"Entity API authorization provider '{manager.Api.AuthorizationProviderType}' not found.");
            if (!typeof(IEntityApiAuthorizationProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException(
                    $"Entity API authorization provider '{manager.Api.AuthorizationProviderType}' must implement IEntityApiAuthorizationProvider.");
            }

            return (IEntityApiAuthorizationProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }

        /// <summary>
        /// Создать провайдер авторизации для endpoint-а структуры менеджера.
        /// </summary>
        public IEntityStructureAuthorizationProvider CreateStructureProvider(IServiceProvider services, BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(manager);

            if (string.IsNullOrWhiteSpace(manager.Api.StructureAuthorizationProviderType))
            {
                return services.GetRequiredService<AdminEntityStructureAuthorizationProvider>();
            }

            var providerType = Type.GetType(manager.Api.StructureAuthorizationProviderType)
                ?? throw new InvalidOperationException(
                    $"Entity structure authorization provider '{manager.Api.StructureAuthorizationProviderType}' not found.");
            if (!typeof(IEntityStructureAuthorizationProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException(
                    $"Entity structure authorization provider '{manager.Api.StructureAuthorizationProviderType}' must implement IEntityStructureAuthorizationProvider.");
            }

            return (IEntityStructureAuthorizationProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }
    }
}
