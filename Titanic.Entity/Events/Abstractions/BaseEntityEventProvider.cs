using System.Net;
using System.Text.Json;
using Titanic.Common.Session;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Базовый провайдер вызова событийного слоя Entity ORM.
    /// </summary>
    public abstract class BaseEntityEventProvider
    {
        #region Members

        /// <summary>
        /// Передаёт событие подходящему провайдеру событийного слоя.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        internal static void DispatchEntityEvent(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            var services = EntityEventProviderResolver.GetServices();
            var provider = EntityEventProviderResolver.Resolve(manager, services);
            provider.Dispatch(entity, manager, stage, services);
        }

        /// <summary>
        /// Проверяет, может ли провайдер обработать события указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <returns><see langword="true" />, если провайдер поддерживает менеджер.</returns>
        public abstract bool CanDispatch(BaseEntityManager manager);

        /// <summary>
        /// Выполняет dispatch события Entity ORM.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="services">Провайдер сервисов приложения.</param>
        public abstract void Dispatch(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage,
            IServiceProvider services);

        /// <summary>
        /// Проверяет, является ли стадия началом обработки одной сущности.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns><see langword="true" />, если перед стадией нужно создать remote listener.</returns>
        protected internal static bool IsInitialStage(EntityEventStage stage)
        {
            return stage is EntityEventStage.Saving or EntityEventStage.Deleting;
        }

        /// <summary>
        /// Проверяет, является ли стадия завершением обработки одной сущности.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns><see langword="true" />, если после стадии нужно удалить remote listener.</returns>
        protected internal static bool IsFinalStage(EntityEventStage stage)
        {
            return stage is EntityEventStage.Saved or EntityEventStage.Deleted;
        }

        /// <summary>
        /// Вызывает локальные listener-ы, зарегистрированные для таблицы текущей сущности.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        protected internal static void DispatchLocalListeners(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            DispatchListeners(entity, manager, stage, EntityEventListenerRegistry.GetListeners(entity.TableName));
        }

        /// <summary>
        /// Вызывает переданные listener-ы для указанной стадии событийного pipeline.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="listeners">Экземпляры listener-ов событийного слоя.</param>
        protected internal static void DispatchListeners(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage,
            IReadOnlyCollection<BaseEntityEventListener> listeners)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(listeners);

            if (listeners.Count == 0)
            {
                return;
            }

            var args = new EntityEventArgs(manager, stage);
            foreach (var listener in listeners)
            {
                InvokeListener(listener, entity, args);

                if (args.IsCanceled)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(args.CancelReason)
                            ? $"Entity event pipeline was canceled during '{stage}'."
                            : args.CancelReason);
                }
            }
        }

        /// <summary>
        /// Создаёт транспортный dispatch-запрос из текущей ORM-сущности.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Dispatch-запрос событийного слоя.</returns>
        protected static EntityEventDispatchRequest CreateRequest(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            return new EntityEventDispatchRequest
            {
                ManagerName = manager.Name,
                TableName = entity.TableName,
                DispatchId = entity.EventDispatchId,
                Stage = stage.ToString(),
                IsNew = entity.IsNew,
                UserConnection = CloneUserConnection(entity.UserConnection),
                Values = CopyValues(entity.ToDictionary()),
                OldValues = CopyValues(entity.OldValues)
            };
        }

        /// <summary>
        /// Пытается получить URI внешнего listener-а из настроек менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="uri">URI внешнего listener-а.</param>
        /// <returns><see langword="true" />, если listener задан корректным абсолютным URI.</returns>
        protected static bool TryGetListenerUri(BaseEntityManager manager, out Uri? uri)
        {
            ArgumentNullException.ThrowIfNull(manager);

            uri = null;
            return !string.IsNullOrWhiteSpace(manager.EventListener)
                && Uri.TryCreate(manager.EventListener, UriKind.Absolute, out uri);
        }

        /// <summary>
        /// Возвращает URI внешнего listener-а или выбрасывает ошибку конфигурации.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <returns>URI внешнего listener-а.</returns>
        protected static Uri GetListenerUri(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            var listener = manager.EventListener
                ?? throw new InvalidOperationException("Event listener location is not configured.");
            if (!Uri.TryCreate(listener, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException(
                    $"Event listener '{listener}' is not a valid absolute URI.");
            }

            return uri;
        }

        /// <summary>
        /// Применяет значения, возвращённые внешним listener-ом, к ORM-сущности.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="response">Ответ событийного listener-а.</param>
        protected static void ApplyResponseValues(
            global::Titanic.Entity.Orm.Entity entity,
            EntityEventDispatchResponse response)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(response);

            if (!response.Success || response.Values.Count == 0)
            {
                return;
            }

            entity.SetValues(NormalizeValues(response.Values));
        }

        /// <summary>
        /// Проверяет успешность ответа событийного listener-а.
        /// </summary>
        /// <param name="response">Ответ событийного listener-а.</param>
        /// <param name="statusCode">HTTP-статус transport-вызова.</param>
        protected static void EnsureResponseSuccess(EntityEventDispatchResponse response, HttpStatusCode statusCode)
        {
            ArgumentNullException.ThrowIfNull(response);

            if (response.Success)
            {
                return;
            }

            if (response.Canceled || statusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(response.CancelReason)
                        ? "Entity event pipeline was canceled by external listener."
                        : response.CancelReason);
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "External entity event listener failed."
                    : response.ErrorMessage);
        }

        /// <summary>
        /// Нормализует значения, полученные из JSON transport-а.
        /// </summary>
        /// <param name="values">Значения из ответа listener-а.</param>
        /// <returns>Нормализованный словарь значений.</returns>
        protected internal static Dictionary<string, object?> NormalizeValues(IReadOnlyDictionary<string, object?> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return values.ToDictionary(
                x => x.Key,
                x => NormalizeJsonValue(x.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Вызывает метод listener-а, соответствующий стадии события.
        /// </summary>
        /// <param name="listener">Listener событийного слоя.</param>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="args">Аргументы события.</param>
        private static void InvokeListener(
            BaseEntityEventListener listener,
            global::Titanic.Entity.Orm.Entity entity,
            EntityEventArgs args)
        {
            switch (args.Stage)
            {
                case EntityEventStage.Saving:
                    listener.OnSaving(entity, args);
                    break;
                case EntityEventStage.Saved:
                    listener.OnSaved(entity, args);
                    break;
                case EntityEventStage.Inserting:
                    listener.OnInserting(entity, args);
                    break;
                case EntityEventStage.Inserted:
                    listener.OnInserted(entity, args);
                    break;
                case EntityEventStage.Updating:
                    listener.OnUpdating(entity, args);
                    break;
                case EntityEventStage.Updated:
                    listener.OnUpdated(entity, args);
                    break;
                case EntityEventStage.Deleting:
                    listener.OnDeleting(entity, args);
                    break;
                case EntityEventStage.Deleted:
                    listener.OnDeleted(entity, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(args),
                        args.Stage,
                        "Unsupported entity event stage.");
            }
        }

        /// <summary>
        /// Создаёт копию словаря значений сущности для транспортного запроса.
        /// </summary>
        /// <param name="values">Исходные значения сущности.</param>
        /// <returns>Копия словаря значений.</returns>
        private static Dictionary<string, object?> CopyValues(IReadOnlyDictionary<string, object?> values)
        {
            return values.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Клонирует пользовательский контекст для transport-запроса.
        /// </summary>
        /// <param name="userConnection">Исходный пользовательский контекст.</param>
        /// <returns>Копия пользовательского контекста.</returns>
        private static UserConnection CloneUserConnection(UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(userConnection);

            return new UserConnection
            {
                UserId = userConnection.UserId,
                Culture = new UserCulture
                {
                    Id = userConnection.Culture.Id,
                    Name = userConnection.Culture.Name
                }
            };
        }

        /// <summary>
        /// Нормализует одно JSON-значение в CLR-значение.
        /// </summary>
        /// <param name="value">Значение из JSON.</param>
        /// <returns>CLR-значение.</returns>
        private static object? NormalizeJsonValue(object? value)
        {
            return value is JsonElement element
                ? NormalizeJsonElement(element)
                : value;
        }

        /// <summary>
        /// Нормализует <see cref="JsonElement" /> в CLR-значение.
        /// </summary>
        /// <param name="element">JSON-элемент.</param>
        /// <returns>CLR-значение.</returns>
        private static object? NormalizeJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
                JsonValueKind.Number => element.GetDouble(),
                _ => throw new NotSupportedException(
                    $"JSON value kind '{element.ValueKind}' is not supported in entity event response.")
            };
        }

        #endregion Members
    }
}
