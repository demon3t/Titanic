using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Authorization.Interfaces;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Фабрика провайдеров поиска пользовательского контекста для Entity API.
    /// </summary>
    internal sealed class EntityApiAuthorizationProviderFactory
    {
        /// <summary>
        /// Создать провайдер поиска пользовательского контекста.
        /// </summary>
        /// <param name="services"> Провайдер сервисов. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="kind"> Тип провайдера. </param>
        /// <returns> Провайдер поиска пользовательского контекста. </returns>
        public IUserConnectionTokenProvider CreateProvider(
            IServiceProvider services,
            BaseEntityManager manager,
            EntityApiAuthorizationProviderKind kind)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(manager);

            var providerTypeName = kind switch
            {
                EntityApiAuthorizationProviderKind.Default => manager.Api.AuthorizationProviderType,
                EntityApiAuthorizationProviderKind.Structure =>
                    string.IsNullOrWhiteSpace(manager.Api.StructureAuthorizationProviderType)
                        ? manager.Api.AuthorizationProviderType
                        : manager.Api.StructureAuthorizationProviderType,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported authorization provider kind.")
            };

            if (string.IsNullOrWhiteSpace(providerTypeName))
            {
                throw new InvalidOperationException(
                    $"Entity API authorization provider is not configured for manager '{manager.Name}'.");
            }

            var providerType = Type.GetType(providerTypeName)
                ?? throw new InvalidOperationException(
                    $"Entity API authorization provider '{providerTypeName}' not found.");
            if (!typeof(IUserConnectionTokenProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException(
                    $"Entity API authorization provider '{providerTypeName}' must implement {nameof(IUserConnectionTokenProvider)}.");
            }

            return (IUserConnectionTokenProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }
    }
}
