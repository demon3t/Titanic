using System.Data.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Configuration;
using Titanic.Db.Interfaces;
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
        private const string ListenerUri = "http://listener.test/entity-event-listener/lifetime";

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
            var client = listenerApp.GetTestClient();
            var dispatchId = Guid.NewGuid().ToString("N");

            await DispatchAsync(client, EntityEventStage.Saving, dispatchId);
            await DispatchAsync(client, EntityEventStage.Inserting, dispatchId);
            await DispatchAsync(client, EntityEventStage.Saved, dispatchId);
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
            var client = listenerApp.GetTestClient();
            var dispatchId = Guid.NewGuid().ToString("N");

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
            ConfigureEntityServices(services =>
            {
                services.AddSingleton<IEntityEventHttpClientFactory>(new LifetimeHttpClientFactory(listenerApp));
            });

            var manager = CreateRemoteManager();
            var entity = manager.Create<OrmDepartmentEntity>(CreateUserConnection())
                .Set(nameof(OrmDepartmentEntity.Name), $"lifetime-{Guid.NewGuid():N}");

            entity.Save();

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(4, instanceIds.Count);
            Assert.Single(instanceIds.Distinct());
        }

        /// <summary>
        /// Выполняет HTTP dispatch-запрос к listener API.
        /// </summary>
        /// <param name="client">HTTP-клиент тестового приложения.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        private static async Task DispatchAsync(HttpClient client, EntityEventStage stage, string dispatchId)
        {
            var response = await client.PostAsJsonAsync(ListenerPath, CreateDispatchRequest(stage, dispatchId));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Создаёт тестовое приложение listener API.
        /// </summary>
        /// <param name="listenerIdleTimeout">Idle timeout экземпляра remote listener-а.</param>
        /// <returns>Запущенное тестовое приложение.</returns>
        private static async Task<WebApplication> CreateListenerAppAsync(TimeSpan? listenerIdleTimeout = null)
        {
            ResetRuntimeState();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseTestServer();

            builder.AddTitanicDb(config =>
            {
                config.DefaultProviderName = "RemoteLifetimeMock";
                config.Providers =
                [
                    new DbProviderConfig
                    {
                        Name = "RemoteLifetimeMock",
                        ConnectionString = "mock",
                        Types = new ProviderTypeConfig
                        {
                            ProviderType = typeof(LifetimeMockDbProvider).AssemblyQualifiedName!,
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
                        DbProviderName = "RemoteLifetimeMock",
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

            var app = builder.Build();
            app.MapTitanicEntityEventListenerApi();
            await app.StartAsync();
            return app;
        }

        /// <summary>
        /// Создаёт менеджер, который вызывает внешний HTTP listener.
        /// </summary>
        /// <returns>Менеджер Entity ORM.</returns>
        private static BaseEntityManager CreateRemoteManager()
        {
            var manager = new EntityDbManager();
            manager.Initialize(
                ManagerName,
                new LifetimeMockDbProvider("mock", new PostgresEngine()),
                new EntityManagerSettings
                {
                    EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                    EventListener = ListenerUri
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
        /// Настраивает сервисы Entity ORM для client-side remote dispatch.
        /// </summary>
        /// <param name="configure">Делегат настройки DI.</param>
        private static void ConfigureEntityServices(Action<IServiceCollection> configure)
        {
            global::Titanic.Entity.EntityManager.ResetServices();
            var services = new ServiceCollection();
            configure(services);
            global::Titanic.Entity.EntityManager.ConfigureServices(services.BuildServiceProvider());
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

        /// <summary>
        /// HTTP factory, который направляет remote dispatch в TestServer.
        /// </summary>
        private sealed class LifetimeHttpClientFactory : IEntityEventHttpClientFactory
        {
            private readonly WebApplication _app;

            /// <summary>
            /// Создаёт factory для указанного тестового приложения.
            /// </summary>
            /// <param name="app">Тестовое приложение listener API.</param>
            public LifetimeHttpClientFactory(WebApplication app)
            {
                _app = app;
            }

            /// <inheritdoc />
            public HttpClient CreateClient(Uri listenerUri)
            {
                var client = _app.GetTestClient();
                client.BaseAddress = new Uri(listenerUri.GetLeftPart(UriPartial.Authority));
                return client;
            }
        }

        /// <summary>
        /// Mock DB provider для тестов remote listener lifetime без реальной БД.
        /// </summary>
        private sealed class LifetimeMockDbProvider : BaseDbProvider
        {
            /// <summary>
            /// Создаёт mock provider.
            /// </summary>
            /// <param name="connectionString">Строка подключения.</param>
            /// <param name="engine">SQL-движок.</param>
            public LifetimeMockDbProvider(string connectionString, BaseDbEngine engine)
                : base(connectionString, engine)
            {
            }

            /// <inheritdoc />
            public override int Execute(IQuery query)
            {
                return 1;
            }

            /// <inheritdoc />
            public override T ExecuteScalar<T>(IQuery query)
            {
                object result = typeof(T) switch
                {
                    var type when type == typeof(Guid) => Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    var type when type == typeof(int) => 1,
                    var type when type == typeof(long) => 1L,
                    var type when type == typeof(object) => 1,
                    _ => Activator.CreateInstance<T>()!
                };

                return (T)result;
            }

            /// <inheritdoc />
            public override List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
            {
                return [];
            }

            /// <inheritdoc />
            protected override DbConnection CreateConnection()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            protected override DbParameter CreateParameter(QueryParameter parameter)
            {
                throw new NotSupportedException();
            }
        }

        #endregion Members
    }
}
