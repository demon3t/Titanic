using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Grpc.Net.Client;
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
using Titanic.Entity.Events.Grpc;
using Titanic.Entity.Interfaces;
using Titanic.Entity.WebApplication;
using Titanic.Entity.WebApplication.Configuration;
using Titanic.Test.Db.Integration;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тесты локального и удалённого транспорта обработчиков событий Entity ORM.
    /// </summary>
    public sealed class EntityEventTransportTests : IClassFixture<IntegrationTestFixture>
    {
        #region Members

        private const string LocalManagerName = "TransportManagerLocal";
        private const string HttpManagerName = "TransportManagerHttp";
        private const string GrpcManagerName = "TransportManagerGrpc";
        private const string HttpListenerPath = "/entity-event-listener/transport-http";
        private const string HttpListenerUri = "http://listener.test/entity-event-listener/transport-http";
        private const string GrpcListenerUri = "grpc://listener.test";
        private const string FilledDescription = "filled-by-event-listener";

        private readonly IntegrationTestFixture _fixture;

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventTransportTests.
        /// </summary>
        public EntityEventTransportTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_WithLocalEventListener_ShouldFillFieldAndPersistIt.
        /// </summary>
        [SkippableFact]
        public void Entity_Save_WithLocalEventListener_ShouldFillFieldAndPersistIt()
        {
            EnsureIntegrationDatabase();
            ResetRuntimeState();
            ConfigureEntityServices();
            var manager = CreateDbBackedManager(LocalManagerName, null);
            var entity = CreateDepartmentEntity(manager, "transport-local");

            entity.Save();

            Assert.Equal(FilledDescription, entity.Get<string>(nameof(OrmDepartmentEntity.Description)));
            Assert.Equal(
                new[] { "saving:departments", "inserting:departments", "inserted:departments", "saved:departments" },
                TransportEventSink.Events);
            AssertPersistedDescription(manager, entity.Get<string>(nameof(OrmDepartmentEntity.Name))!, FilledDescription);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_WithRemoteHttpEventListener_ShouldFillFieldAndPersistIt.
        /// </summary>
        [SkippableFact]
        public async Task Entity_Save_WithRemoteHttpEventListener_ShouldFillFieldAndPersistIt()
        {
            EnsureIntegrationDatabase();
            ResetRuntimeState();
            await using var listenerApp = await CreateDbBackedListenerAppAsync();
            ConfigureEntityServices(services =>
            {
                services.AddSingleton<IEntityEventHttpClientFactory>(new TestEntityEventHttpClientFactory(listenerApp));
            });

            var manager = CreateDbBackedManager(HttpManagerName, HttpListenerUri);
            var entity = CreateDepartmentEntity(manager, "transport-http");

            entity.Save();

            Assert.Equal(FilledDescription, entity.Get<string>(nameof(OrmDepartmentEntity.Description)));
            Assert.Equal(
                new[] { "saving:departments", "inserting:departments", "inserted:departments", "saved:departments" },
                TransportEventSink.Events);
            AssertPersistedDescription(manager, entity.Get<string>(nameof(OrmDepartmentEntity.Name))!, FilledDescription);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_WithRemoteGrpcEventListener_ShouldFillFieldAndPersistIt.
        /// </summary>
        [SkippableFact]
        public async Task Entity_Save_WithRemoteGrpcEventListener_ShouldFillFieldAndPersistIt()
        {
            EnsureIntegrationDatabase();
            ResetRuntimeState();
            await using var listenerApp = await CreateDbBackedListenerAppAsync();
            ConfigureEntityServices(services =>
            {
                services.AddSingleton<IEntityEventGrpcClientFactory>(new TestEntityEventGrpcClientFactory(listenerApp));
            });

            var manager = CreateDbBackedManager(GrpcManagerName, GrpcListenerUri);
            var entity = CreateDepartmentEntity(manager, "transport-grpc");

            entity.Save();

            Assert.Equal(FilledDescription, entity.Get<string>(nameof(OrmDepartmentEntity.Description)));
            Assert.Equal(
                new[] { "saving:departments", "inserting:departments", "inserted:departments", "saved:departments" },
                TransportEventSink.Events);
            AssertPersistedDescription(manager, entity.Get<string>(nameof(OrmDepartmentEntity.Name))!, FilledDescription);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_WithRemoteHttpEventListener_ShouldPassUserConnectionToListener.
        /// </summary>
        [Fact]
        public async Task Entity_Save_WithRemoteHttpEventListener_ShouldPassUserConnectionToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateMockListenerAppAsync();
            ConfigureEntityServices(services =>
            {
                services.AddSingleton<IEntityEventHttpClientFactory>(new TestEntityEventHttpClientFactory(listenerApp));
            });

            var manager = CreateMockManager(HttpManagerName, HttpListenerUri);
            var entity = CreateDepartmentEntity(manager, "transport-http-user");

            entity.Save();

            AssertCapturedUserConnection();
        }

        /// <summary>
        /// Инициализирует новый экземпляр Entity_Save_WithRemoteGrpcEventListener_ShouldPassUserConnectionToListener.
        /// </summary>
        [Fact]
        public async Task Entity_Save_WithRemoteGrpcEventListener_ShouldPassUserConnectionToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateMockListenerAppAsync();
            ConfigureEntityServices(services =>
            {
                services.AddSingleton<IEntityEventGrpcClientFactory>(new TestEntityEventGrpcClientFactory(listenerApp));
            });

            var manager = CreateMockManager(GrpcManagerName, GrpcListenerUri);
            var entity = CreateDepartmentEntity(manager, "transport-grpc-user");

            entity.Save();

            AssertCapturedUserConnection();
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventListenerApi_HttpEndpoint_ShouldReturnSuccessAndMutatedValues.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_HttpEndpoint_ShouldReturnSuccessAndMutatedValues()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateMockListenerAppAsync();
            var client = listenerApp.GetTestClient();

            var response = await client.PostAsJsonAsync(
                HttpListenerPath,
                CreateDispatchRequest(EntityEventStage.Saving));

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<EntityEventDispatchResponse>();

            Assert.NotNull(body);
            Assert.True(body.Success);
            Assert.Equal(new[] { "saving:departments" }, TransportEventSink.Events);
            Assert.Equal(FilledDescription, GetStringValue(body.Values, nameof(OrmDepartmentEntity.Description)));
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventListenerApi_ShouldMapMultipleHttpEndpointsFromManagerConfiguration.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_ShouldMapMultipleHttpEndpointsFromManagerConfiguration()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateMultiManagerListenerAppAsync();
            var client = listenerApp.GetTestClient();

            var firstResponse = await client.PostAsJsonAsync(
                "/entity-event-listener/transport-a",
                CreateDispatchRequest(EntityEventStage.Saving, "TransportManagerA"));
            var secondResponse = await client.PostAsJsonAsync(
                "/entity-event-listener/transport-b",
                CreateDispatchRequest(EntityEventStage.Saving, "TransportManagerB"));

            firstResponse.EnsureSuccessStatusCode();
            secondResponse.EnsureSuccessStatusCode();
            Assert.Equal(new[] { "saving:departments", "saving:departments" }, TransportEventSink.Events);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateDbBackedListenerAppAsync.
        /// </summary>
        private static async Task<WebApplication> CreateDbBackedListenerAppAsync()
        {
            ResetAllState();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseTestServer();

            var dbConfig = TestConfigurationLoader.LoadDbConfig();
            builder.AddTitanicDb(config =>
            {
                config.DefaultProviderName = dbConfig.DefaultProviderName;
                config.Providers = dbConfig.Providers;
            });

            builder.AddTitanicEntityEventListenerApi(config =>
            {
                config.Managers =
                [
                    new EntityManagerSettings
                    {
                        Name = HttpManagerName,
                        DbProviderName = IntegrationTestFixture.ProviderName,
                        EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                        EventListenerApi = new EntityManagerEventListenerApiSettings
                        {
                            Mode = EntityEventListenerApiMode.Http,
                            Path = HttpListenerPath
                        }
                    },
                    new EntityManagerSettings
                    {
                        Name = GrpcManagerName,
                        DbProviderName = IntegrationTestFixture.ProviderName,
                        EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                        EventListenerApi = new EntityManagerEventListenerApiSettings
                        {
                            Mode = EntityEventListenerApiMode.Grpc
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
        /// Инициализирует новый экземпляр CreateMockListenerAppAsync.
        /// </summary>
        private static async Task<WebApplication> CreateMockListenerAppAsync()
        {
            ResetAllState();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseTestServer();

            builder.AddTitanicDb(config =>
            {
                config.DefaultProviderName = "EventListenerMock";
                config.Providers =
                [
                    new DbProviderConfig
                    {
                        Name = "EventListenerMock",
                        ConnectionString = "mock",
                        Types = new ProviderTypeConfig
                        {
                            ProviderType = typeof(EventTransportMockDbProvider).AssemblyQualifiedName!,
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
                        Name = HttpManagerName,
                        DbProviderName = "EventListenerMock",
                        EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                        EventListenerApi = new EntityManagerEventListenerApiSettings
                        {
                            Mode = EntityEventListenerApiMode.Http,
                            Path = HttpListenerPath
                        }
                    },
                    new EntityManagerSettings
                    {
                        Name = GrpcManagerName,
                        DbProviderName = "EventListenerMock",
                        EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                        EventListenerApi = new EntityManagerEventListenerApiSettings
                        {
                            Mode = EntityEventListenerApiMode.Grpc
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
        /// Инициализирует новый экземпляр CreateMultiManagerListenerAppAsync.
        /// </summary>
        private static async Task<WebApplication> CreateMultiManagerListenerAppAsync()
        {
            ResetAllState();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseTestServer();

            builder.AddTitanicDb(config =>
            {
                config.DefaultProviderName = "EventListenerMock";
                config.Providers =
                [
                    new DbProviderConfig
                    {
                        Name = "EventListenerMock",
                        ConnectionString = "mock",
                        Types = new ProviderTypeConfig
                        {
                            ProviderType = typeof(EventTransportMockDbProvider).AssemblyQualifiedName!,
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
                        Name = "TransportManagerA",
                        DbProviderName = "EventListenerMock",
                        EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                        EventListenerApi = new EntityManagerEventListenerApiSettings
                        {
                            Mode = EntityEventListenerApiMode.Http,
                            Path = "/entity-event-listener/transport-a"
                        }
                    },
                    new EntityManagerSettings
                    {
                        Name = "TransportManagerB",
                        DbProviderName = "EventListenerMock",
                        EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                        EventListenerApi = new EntityManagerEventListenerApiSettings
                        {
                            Mode = EntityEventListenerApiMode.Http,
                            Path = "/entity-event-listener/transport-b"
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
        /// Инициализирует новый экземпляр CreateDbBackedManager.
        /// </summary>
        private static BaseEntityManager CreateDbBackedManager(string managerName, string? eventListener)
        {
            var manager = new EntityDbManager();
            manager.Initialize(
                managerName,
                DbManager.GetProvider(IntegrationTestFixture.ProviderName),
                new EntityManagerSettings
                {
                    EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                    EventListener = eventListener
                });

            return manager;
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateMockManager.
        /// </summary>
        private static BaseEntityManager CreateMockManager(string managerName, string? eventListener)
        {
            var provider = new EventTransportMockDbProvider("mock", new PostgresEngine());
            var manager = new EntityDbManager();
            manager.Initialize(
                managerName,
                provider,
                new EntityManagerSettings
                {
                    EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                    EventListener = eventListener
                });

            return manager;
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateDepartmentEntity.
        /// </summary>
        private static global::Titanic.Entity.Orm.Entity CreateDepartmentEntity(BaseEntityManager manager, string namePrefix)
        {
            return manager.Create<OrmDepartmentEntity>(CreateUserConnection())
                .Set(nameof(OrmDepartmentEntity.Name), $"{namePrefix}-{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateDispatchRequest.
        /// </summary>
        private static EntityEventDispatchRequest CreateDispatchRequest(EntityEventStage stage, string managerName = HttpManagerName)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = managerName,
                TableName = "departments",
                Stage = stage.ToString(),
                IsNew = true,
                UserConnection = CreateUserConnection(),
                Values = new Dictionary<string, object?>
                {
                    [nameof(OrmDepartmentEntity.Name)] = "manual-http-dispatch"
                }
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateUserConnection.
        /// </summary>
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
        /// Инициализирует новый экземпляр ConfigureEntityServices.
        /// </summary>
        private static void ConfigureEntityServices(Action<IServiceCollection>? configure = null)
        {
            global::Titanic.Entity.EntityManager.ResetServices();
            var services = new ServiceCollection();
            configure?.Invoke(services);
            global::Titanic.Entity.EntityManager.ConfigureServices(services.BuildServiceProvider());
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResetRuntimeState.
        /// </summary>
        private static void ResetRuntimeState()
        {
            TransportEventSink.Reset();
            TransportEventListener.IsEnabled = true;
            global::Titanic.Entity.EntityManager.Reset();
            global::Titanic.Entity.EntityManager.ResetServices();
        }

        /// <summary>
        /// Инициализирует новый экземпляр AssertCapturedUserConnection.
        /// </summary>
        private static void AssertCapturedUserConnection()
        {
            Assert.NotNull(TransportEventSink.LastUserConnection);
            Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), TransportEventSink.LastUserConnection!.UserId);
            Assert.NotNull(TransportEventSink.LastUserConnection.Culture);
            Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), TransportEventSink.LastUserConnection.Culture.Id);
            Assert.Equal("Test", TransportEventSink.LastUserConnection.Culture.Name);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResetAllState.
        /// </summary>
        private static void ResetAllState()
        {
            ResetRuntimeState();
            DbManager.Reset();
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureIntegrationDatabase.
        /// </summary>
        private void EnsureIntegrationDatabase()
        {
            DbManager.Initialize(TestConfigurationLoader.LoadDbConfig());
            DbManager.RegisterDatabase<TestDatabase>(IntegrationTestFixture.ProviderName);

            Exception exception = null!;
            if (!DbManager.Get<TestDatabase>().CheckConnection(ref exception))
            {
                Skip.IfNot(false,
                    $"PostgreSQL is not available, skipping integration test. Reason: {exception?.Message}");
            }

            _fixture.TruncateAll();
        }

        /// <summary>
        /// Инициализирует новый экземпляр AssertPersistedDescription.
        /// </summary>
        private static void AssertPersistedDescription(BaseEntityManager manager, string name, string expectedDescription)
        {
            EnsureTestDatabaseRegistered();

            var esq = manager.Query<OrmDepartmentEntity>(CreateUserConnection());
            esq.AddPrimaryColumn();
            esq.AddDisplayColumn();
            esq.AddColumn(nameof(OrmDepartmentEntity.Description));
            esq.AddFilter(Titanic.Entity.Orm.EntityComparisonType.Equal, nameof(OrmDepartmentEntity.Name), name);

            var row = esq.GetEntityCollection().Single();
            Assert.Equal(expectedDescription, row.Get<string>(nameof(OrmDepartmentEntity.Description)));

            var persistedDescription = DbManager.Get<TestDatabase>()
                .Select()
                .Column(Column.Name("d", "description"))
                .From("departments").As("d")
                .Where("d", "name").IsEqual(Column.Parameter(name))
                .ExecuteScalar<string>();

            Assert.Equal(expectedDescription, persistedDescription);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureTestDatabaseRegistered.
        /// </summary>
        private static void EnsureTestDatabaseRegistered()
        {
            try
            {
                _ = DbManager.Get<TestDatabase>();
            }
            catch (KeyNotFoundException)
            {
                DbManager.RegisterDatabase<TestDatabase>(IntegrationTestFixture.ProviderName);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр GetStringValue.
        /// </summary>
        private static string? GetStringValue(IReadOnlyDictionary<string, object?> values, string key)
        {
            if (!values.TryGetValue(key, out var value))
            {
                return null;
            }

            return value switch
            {
                null => null,
                string stringValue => stringValue,
                System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } element => element.GetString(),
                _ => value.ToString()
            };
        }

        [EntityEventListener("departments")]
        private sealed class TransportEventListener : BaseEntityEventListener
        {
            public static bool IsEnabled { get; set; }

            private readonly string _entityName;

            /// <summary>
            /// Инициализирует новый экземпляр TransportEventListener.
            /// </summary>
            public TransportEventListener(string entityName)
            {
                _entityName = entityName;
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnSaving.
            /// </summary>
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    entity.Set(nameof(OrmDepartmentEntity.Description), FilledDescription);
                    TransportEventSink.SetUserConnection(entity.UserConnection);
                    TransportEventSink.Add($"saving:{_entityName}");
                }
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnInserting.
            /// </summary>
            public override void OnInserting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    TransportEventSink.Add($"inserting:{_entityName}");
                }
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnInserted.
            /// </summary>
            public override void OnInserted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    TransportEventSink.Add($"inserted:{_entityName}");
                }
            }

            /// <summary>
            /// Инициализирует новый экземпляр OnSaved.
            /// </summary>
            public override void OnSaved(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    TransportEventSink.Add($"saved:{_entityName}");
                }
            }
        }

        private sealed class TestEntityEventHttpClientFactory : IEntityEventHttpClientFactory
        {
            private readonly WebApplication _app;

            /// <summary>
            /// Инициализирует новый экземпляр TestEntityEventHttpClientFactory.
            /// </summary>
            public TestEntityEventHttpClientFactory(WebApplication app)
            {
                _app = app;
            }

            /// <summary>
            /// Инициализирует новый экземпляр CreateClient.
            /// </summary>
            public HttpClient CreateClient(Uri listenerUri)
            {
                var client = _app.GetTestClient();
                client.BaseAddress = new Uri(listenerUri.GetLeftPart(UriPartial.Authority));
                return client;
            }
        }

        private sealed class TestEntityEventGrpcClientFactory : IEntityEventGrpcClientFactory
        {
            private readonly WebApplication _app;

            /// <summary>
            /// Инициализирует новый экземпляр TestEntityEventGrpcClientFactory.
            /// </summary>
            public TestEntityEventGrpcClientFactory(WebApplication app)
            {
                _app = app;
            }

            /// <summary>
            /// Инициализирует новый экземпляр CreateClient.
            /// </summary>
            public EntityEventListenerGrpc.EntityEventListenerGrpcClient CreateClient(Uri listenerUri)
            {
                var client = _app.GetTestClient();
                client.BaseAddress = new Uri("http://localhost");
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                var channel = GrpcChannel.ForAddress(
                    "http://localhost",
                    new GrpcChannelOptions
                    {
                        HttpClient = client
                    });

                return new EntityEventListenerGrpc.EntityEventListenerGrpcClient(channel);
            }
        }

        private static class TransportEventSink
        {
            private static readonly object SyncRoot = new();
            private static readonly List<string> InternalEvents = [];
            private static UserConnection? _lastUserConnection;

            public static IReadOnlyList<string> Events
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return InternalEvents.ToArray();
                    }
                }
            }

            public static UserConnection? LastUserConnection
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _lastUserConnection;
                    }
                }
            }

            /// <summary>
            /// Инициализирует новый экземпляр Add.
            /// </summary>
            public static void Add(string value)
            {
                lock (SyncRoot)
                {
                    InternalEvents.Add(value);
                }
            }

            /// <summary>
            /// Инициализирует новый экземпляр SetUserConnection.
            /// </summary>
            public static void SetUserConnection(UserConnection userConnection)
            {
                lock (SyncRoot)
                {
                    _lastUserConnection = new UserConnection
                    {
                        UserId = userConnection.UserId,
                        Culture = new UserCulture
                        {
                            Id = userConnection.Culture.Id,
                            Name = userConnection.Culture.Name
                        }
                    };
                }
            }

            /// <summary>
            /// Инициализирует новый экземпляр Reset.
            /// </summary>
            public static void Reset()
            {
                lock (SyncRoot)
                {
                    InternalEvents.Clear();
                    _lastUserConnection = null;
                }
            }
        }

        private sealed class EventTransportMockDbProvider : BaseDbProvider
        {
            /// <summary>
            /// Инициализирует новый экземпляр EventTransportMockDbProvider.
            /// </summary>
            public EventTransportMockDbProvider(string connectionString, BaseDbEngine engine)
                : base(connectionString, engine)
            {
            }

            /// <summary>
            /// Инициализирует новый экземпляр Execute.
            /// </summary>
            public override int Execute(IQuery query)
            {
                return 1;
            }

            /// <summary>
            /// Выполняет scalar-запрос.
            /// </summary>
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

            /// <summary>
            /// Выполняет reader-запрос.
            /// </summary>
            public override List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
            {
                return [];
            }

            /// <summary>
            /// Инициализирует новый экземпляр CreateConnection.
            /// </summary>
            protected override DbConnection CreateConnection()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Инициализирует новый экземпляр CreateParameter.
            /// </summary>
            protected override DbParameter CreateParameter(QueryParameter parameter)
            {
                throw new NotSupportedException();
            }
        }

        #endregion Members
    }
}


