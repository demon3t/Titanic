using Microsoft.Extensions.DependencyInjection;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Выбирает провайдер вызова событийного слоя для конкретного менеджера.
    /// </summary>
    internal static class EntityEventProviderResolver
    {
        #region Members

        private static readonly BaseEntityEventProvider[] BuiltInProviders =
        [
            new LocalEntityEventProvider(),
            new HttpEntityEventProvider(),
            new GrpcEntityEventProvider(),
            new WebSocketEntityEventProvider()
        ];

        private static IServiceProvider? _services;

        /// <summary>
        /// Привязывает провайдер сервисов приложения к событийному слою.
        /// </summary>
        /// <param name="services">Провайдер сервисов приложения.</param>
        internal static void ConfigureServices(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);

            _services = services;
        }

        /// <summary>
        /// Сбрасывает провайдер сервисов приложения.
        /// </summary>
        internal static void ResetServices()
        {
            _services = null;
        }

        /// <summary>
        /// Возвращает провайдер сервисов приложения или пустую реализацию.
        /// </summary>
        /// <returns>Провайдер сервисов для событийного слоя.</returns>
        internal static IServiceProvider GetServices()
        {
            return _services ?? EmptyServiceProvider.Instance;
        }

        /// <summary>
        /// Возвращает провайдер, который должен обработать события указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>Провайдер событийного слоя.</returns>
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
        /// Возвращает провайдеры из DI и встроенные провайдеры по умолчанию.
        /// </summary>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>Список провайдеров событийного слоя.</returns>
        private static IEnumerable<BaseEntityEventProvider> GetProviders(IServiceProvider services)
        {
            return services
                .GetServices<BaseEntityEventProvider>()
                .Concat(BuiltInProviders)
                .DistinctBy(provider => provider.GetType());
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
                $"Unsupported event listener scheme '{uri.Scheme}'. Use http, https, grpc, grpcs, ws or wss.");
        }

        /// <summary>
        /// Пустой провайдер сервисов, который не возвращает зависимости.
        /// </summary>
        private sealed class EmptyServiceProvider : IServiceProvider
        {
            /// <summary>
            /// Единственный экземпляр пустого провайдера сервисов.
            /// </summary>
            internal static readonly EmptyServiceProvider Instance = new();

            private EmptyServiceProvider()
            {
            }

            /// <inheritdoc />
            public object? GetService(Type serviceType)
            {
                return null;
            }
        }

        #endregion Members
    }
}
