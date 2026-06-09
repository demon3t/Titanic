using Microsoft.Extensions.DependencyInjection;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Выбирает provider вызова событийного слоя для конкретного менеджера.
    /// </summary>
    internal static class EntityEventProviderResolver
    {
        #region Members

        private static readonly BaseEntityEventProvider[] BuiltInProviders =
        [
            new LocalEntityEventProvider(),
            new HttpEntityEventProvider(),
            new GrpcEntityEventProvider()
        ];

        /// <summary>
        /// Пустой provider сервисов для сценариев без настроенного DI.
        /// </summary>
        internal static IServiceProvider EmptyServices { get; } = new EmptyServiceProvider();

        /// <summary>
        /// Возвращает provider, который должен обработать события указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>Provider событийного слоя.</returns>
        internal static BaseEntityEventProvider Resolve(BaseEntityManager manager, IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(services);

            foreach (var provider in GetProviders(services))
            {
                if (provider.CanDispatch(manager))
                {
                    return provider;
                }
            }

            ThrowUnsupportedListener(manager);
            throw new InvalidOperationException("Entity event provider was not resolved.");
        }

        /// <summary>
        /// Возвращает provider-ы из DI и встроенные provider-ы по умолчанию.
        /// </summary>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>Список provider-ов событийного слоя.</returns>
        private static IEnumerable<BaseEntityEventProvider> GetProviders(IServiceProvider services)
        {
            return services
                .GetServices<BaseEntityEventProvider>()
                .Concat(BuiltInProviders);
        }

        /// <summary>
        /// Выбрасывает диагностическую ошибку для неподдержанного listener-а.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        private static void ThrowUnsupportedListener(BaseEntityManager manager)
        {
            if (manager.HasLocalEventListener)
            {
                throw new InvalidOperationException("Local entity event provider is not available.");
            }

            var listener = manager.EventListener
                ?? throw new InvalidOperationException("Event listener location is not configured.");
            if (!Uri.TryCreate(listener, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException(
                    $"Event listener '{listener}' is not a valid absolute URI.");
            }

            throw new InvalidOperationException(
                $"Unsupported event listener scheme '{uri.Scheme}'. Use http, https, grpc or grpcs.");
        }

        /// <summary>
        /// Пустой IServiceProvider, который не возвращает сервисы.
        /// </summary>
        private sealed class EmptyServiceProvider : IServiceProvider
        {
            /// <inheritdoc />
            public object? GetService(Type serviceType)
            {
                return null;
            }
        }

        #endregion Members
    }
}
