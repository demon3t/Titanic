using System.Net.Http.Json;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Configuration;
using Titanic.Db.PosgreSql;
using Titanic.Db.WebApplication;
using Titanic.Entity.Attributes;
using Titanic.Entity.Events;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тесты времени жизни экземпляра remote listener-а.
    /// </summary>
    public sealed class EntityEventRemoteListenerLifetimeTests : IDisposable
    {
        #region Members

        private const string ManagerName = "RemoteLifetimeManager";
        private const string ListenerPath = "/entity-event-listener/lifetime";

        /// <summary>
        /// Освобождает статическое состояние тестовых listener-ов.
        /// </summary>
        public void Dispose()
        {
            ResetRuntimeState(disableListener: true);
        }

        /// <summary>
        /// Проверяет, что remote listener переиспользуется до финальной стадии и удаляется после неё.
        /// </summary>
        [Fact]
        public async Task RemoteListener_ShouldReuseInstanceUntilFinalStageAndReleaseAfterSaved()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateListenerAppAsync();
            using var client = listenerApp.CreateHttpClient();
            var dispatchId = Guid.NewGuid().ToString("N");

            await CreateAsync(client, dispatchId);
            await DispatchAsync(client, EntityEventStage.Saving, dispatchId);
            await DispatchAsync(client, EntityEventStage.Inserting, dispatchId);
            await DispatchAsync(client, EntityEventStage.Saved, dispatchId);
            await DeleteAsync(client, dispatchId);
            await CreateAsync(client, dispatchId);
            await DispatchAsync(client, EntityEventStage.Saving, dispatchId);

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(4, instanceIds.Count);
            Assert.Equal(instanceIds[0], instanceIds[1]);
            Assert.Equal(instanceIds[0], instanceIds[2]);
            Assert.NotEqual(instanceIds[0], instanceIds[3]);
        }

        /// <summary>
        /// Проверяет, что remote listener удаляется после истечения idle timeout.
        /// </summary>
        [Fact]
        public async Task RemoteListener_ShouldExpireInstance_WhenIdleTimeoutPassed()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateListenerAppAsync(TimeSpan.FromMilliseconds(50));
            using var client = listenerApp.CreateHttpClient();
            var dispatchId = Guid.NewGuid().ToString("N");

            await CreateAsync(client, dispatchId);
            await DispatchAsync(client, EntityEventStage.Saving, dispatchId);
            await Task.Delay(150);
            await DispatchAsync(client, EntityEventStage.Inserting, dispatchId);

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(2, instanceIds.Count);
            Assert.NotEqual(instanceIds[0], instanceIds[1]);
        }

        /// <summary>
        /// Проверяет, что client-side remote dispatch передаёт один DispatchId на весь Save pipeline.
        /// </summary>
        [Fact]
        public async Task EntitySave_WithRemoteHttpListener_ShouldReuseOneRemoteListenerInstance()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateListenerAppAsync();

            var manager = CreateRemoteManager(listenerApp.GetHttpListenerUri(ListenerPath));
            var entity = manager.Create<OrmDepartmentEntity>(CreateUserConnection())
                .Set(nameof(OrmDepartmentEntity.Name), $"lifetime-{Guid.NewGuid():N}");

            entity.Save();

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(4, instanceIds.Count);
            Assert.Single(instanceIds.Distinct());
        }

        /// <summary>
        /// Выполняет HTTP-запрос создания remote listener-а.
        /// </summary>
        /// <param name="client">HTTP-клиент тестового приложения.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        private static async Task CreateAsync(HttpClient client, string dispatchId)
        {
            var response = await client.PostAsJsonAsync(
                BuildActionPath(HttpEntityEventProvider.CreateActionPath),
                CreateDispatchRequest(EntityEventStage.Saving, dispatchId));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Выполняет HTTP-запрос конкретной стадии к listener API.
        /// </summary>
        /// <param name="client">HTTP-клиент тестового приложения.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        private static async Task DispatchAsync(HttpClient client, EntityEventStage stage, string dispatchId)
        {
            var response = await client.PostAsJsonAsync(
                BuildStagePath(stage),
                CreateDispatchRequest(stage, dispatchId));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Выполняет HTTP-запрос удаления remote listener-а.
        /// </summary>
        /// <param name="client">HTTP-клиент тестового приложения.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        private static async Task DeleteAsync(HttpClient client, string dispatchId)
        {
            var response = await client.PostAsJsonAsync(
                BuildActionPath(HttpEntityEventProvider.DeleteActionPath),
                CreateDispatchRequest(EntityEventStage.Saved, dispatchId));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Создаёт тестовое приложение listener API.
        /// </summary>
        /// <param name="listenerIdleTimeout">Idle timeout экземпляра remote listener-а.</param>
        /// <returns>Запущенное тестовое приложение.</returns>
        private static Task<EntityEventListenerTestApplication> CreateListenerAppAsync(TimeSpan? listenerIdleTimeout = null)
        {
            ResetRuntimeState();
            return EntityEventListenerTestApplication.StartAsync(builder =>
            {
                builder.AddTitanicDb(config =>
                {
                    config.DefaultProviderName = "RemoteLifetimeInMemory";
                    config.Providers =
                    [
                        new DbProviderConfig
                        {
                            Name = "RemoteLifetimeInMemory",
                            ConnectionString = "in-memory",
                            Types = new ProviderTypeConfig
                            {
                                ProviderType = typeof(EntityEventInMemoryDbProvider).AssemblyQualifiedName!,
                                EngineType = typeof(PostgresEngine).AssemblyQualifiedName!
                            }
                        }
                    ];
                });

                builder.AddTitanicEntityEventListenerApi(config =>
                {
                    config.Managers =
                    [
                        new EntityManagerSettings
                        {
                            Name = ManagerName,
                            DbProviderName = "RemoteLifetimeInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.Http,
                                Path = ListenerPath,
                                ListenerInstanceIdleTimeout = listenerIdleTimeout ?? TimeSpan.FromMinutes(5)
                            }
                        }
                    ];
                });
            });
        }

        /// <summary>
        /// Создаёт менеджер, который вызывает внешний HTTP listener.
        /// </summary>
        /// <returns>Менеджер Entity ORM.</returns>
        private static BaseEntityManager CreateRemoteManager(string listenerUri)
        {
            var manager = new EntityDbManager();
            manager.Initialize(
                ManagerName,
                new EntityEventInMemoryDbProvider("in-memory", new PostgresEngine()),
                new EntityManagerSettings
                {
                    EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                    EventListener = listenerUri
                });

            return manager;
        }

        /// <summary>
        /// Создаёт dispatch-запрос с указанной стадией и идентификатором обработки.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        /// <returns>Dispatch-запрос события.</returns>
        private static EntityEventDispatchRequest CreateDispatchRequest(EntityEventStage stage, string dispatchId)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = ManagerName,
                TableName = "departments",
                DispatchId = dispatchId,
                Stage = stage.ToString(),
                IsNew = true,
                UserConnection = CreateUserConnection(),
                Values = new Dictionary<string, object?>
                {
                    [nameof(OrmDepartmentEntity.Name)] = $"manual-{Guid.NewGuid():N}"
                }
            };
        }

        /// <summary>
        /// Создаёт путь HTTP-действия listener API.
        /// </summary>
        /// <param name="actionPath">Относительный путь действия.</param>
        /// <returns>Полный путь HTTP-действия.</returns>
        private static string BuildActionPath(string actionPath)
        {
            return $"{ListenerPath.TrimEnd('/')}/{actionPath}";
        }

        /// <summary>
        /// Создаёт путь HTTP endpoint-а конкретной стадии событийного pipeline.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Полный путь HTTP endpoint-а стадии.</returns>
        private static string BuildStagePath(EntityEventStage stage)
        {
            return BuildActionPath(HttpEntityEventProvider.GetActionPath(stage));
        }

        /// <summary>
        /// Создаёт тестовый пользовательский контекст.
        /// </summary>
        /// <returns>Пользовательский контекст.</returns>
        private static UserConnection CreateUserConnection()
        {
            return new UserConnection
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Culture = new UserCulture
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Test"
                }
            };
        }

        /// <summary>
        /// Сбрасывает статическое состояние тестов.
        /// </summary>
        /// <param name="disableListener">Отключить тестовый listener.</param>
        private static void ResetRuntimeState(bool disableListener = false)
        {
            LifetimeEventSink.Reset();
            LifetimeEventListener.IsEnabled = !disableListener;
            global::Titanic.Entity.EntityManager.Reset();
            global::Titanic.Entity.EntityManager.ResetServices();
            DbManager.Reset();
        }

        /// <summary>
        /// Тестовый listener, позволяющий отслеживать переиспользование экземпляра.
        /// </summary>
        [EntityEventListener("departments")]
        private sealed class LifetimeEventListener : BaseEntityEventListener
        {
            private readonly int _instanceId = LifetimeEventSink.NextInstanceId();
            private readonly string _entityName;

            /// <summary>
            /// Создаёт listener для указанной Entity-таблицы.
            /// </summary>
            /// <param name="entityName">Имя Entity-таблицы.</param>
            public LifetimeEventListener(string entityName)
            {
                _entityName = entityName;
            }

            /// <summary>
            /// Признак включения listener-а.
            /// </summary>
            public static bool IsEnabled { get; set; }

            /// <inheritdoc />
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                Add("saving");
            }

            /// <inheritdoc />
            public override void OnInserting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                Add("inserting");
            }

            /// <inheritdoc />
            public override void OnInserted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                Add("inserted");
            }

            /// <inheritdoc />
            public override void OnSaved(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                Add("saved");
            }

            /// <summary>
            /// Фиксирует вызов текущего экземпляра listener-а.
            /// </summary>
            /// <param name="stage">Стадия события.</param>
            private void Add(string stage)
            {
                if (IsEnabled)
                {
                    LifetimeEventSink.Add(_instanceId, $"{stage}:{_entityName}");
                }
            }
        }

        /// <summary>
        /// Накопитель вызовов тестового listener-а.
        /// </summary>
        private static class LifetimeEventSink
        {
            private static readonly object SyncRoot = new();
            private static readonly List<int> InternalInstanceIds = [];
            private static readonly List<string> InternalEvents = [];
            private static int _nextInstanceId;

            /// <summary>
            /// Идентификаторы экземпляров listener-а по порядку вызовов.
            /// </summary>
            public static IReadOnlyList<int> InstanceIds
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return InternalInstanceIds.ToArray();
                    }
                }
            }

            /// <summary>
            /// Возвращает следующий идентификатор экземпляра listener-а.
            /// </summary>
            /// <returns>Идентификатор экземпляра.</returns>
            public static int NextInstanceId()
            {
                lock (SyncRoot)
                {
                    return ++_nextInstanceId;
                }
            }

            /// <summary>
            /// Добавляет запись о вызове listener-а.
            /// </summary>
            /// <param name="instanceId">Идентификатор экземпляра listener-а.</param>
            /// <param name="value">Описание события.</param>
            public static void Add(int instanceId, string value)
            {
                lock (SyncRoot)
                {
                    InternalInstanceIds.Add(instanceId);
                    InternalEvents.Add(value);
                }
            }

            /// <summary>
            /// Сбрасывает накопленные вызовы.
            /// </summary>
            public static void Reset()
            {
                lock (SyncRoot)
                {
                    InternalInstanceIds.Clear();
                    InternalEvents.Clear();
                    _nextInstanceId = 0;
                }
            }
        }

        #endregion Members
    }
}
