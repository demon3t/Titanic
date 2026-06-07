using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    internal sealed class EntityApiAuthorizationProviderFactory
    {
        public IUserConnectionTokenProvider CreateProvider(
            IServiceProvider services,
            string? providerTypeName,
            string providerDescription)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerDescription);

            if (string.IsNullOrWhiteSpace(providerTypeName))
            {
                throw new InvalidOperationException($"{providerDescription} is not configured.");
            }

            var providerType = Type.GetType(providerTypeName)
                ?? throw new InvalidOperationException($"{providerDescription} '{providerTypeName}' not found.");
            if (!typeof(IUserConnectionTokenProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException(
                    $"{providerDescription} '{providerTypeName}' must implement {nameof(IUserConnectionTokenProvider)}.");
            }

            return (IUserConnectionTokenProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }
    }
}
