using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
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
using Titanic.Test.Db.Integration;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тесты локального и удалённого транспорта обработчиков событий Entity ORM.
    /// </summary>
    public sealed class EntityEventTransportTests
    {
        #region Members

        private const string LocalManagerName = "TransportManagerLocal";
        private const string HttpManagerName = "TransportManagerHttp";
        private const string GrpcManagerName = "TransportManagerGrpc";
        private const string HttpListenerPath = "/entity-event-listener/transport-http";
        private const string FilledDescription = "filled-by-event-listener";

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

            var manager = CreateDbBackedManager(HttpManagerName, listenerApp.GetHttpListenerUri(HttpListenerPath));
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

            var manager = CreateDbBackedManager(GrpcManagerName, listenerApp.GrpcListenerUri);
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
            await using var listenerApp = await CreateInMemoryListenerAppAsync();

            var manager = CreateInMemoryManager(HttpManagerName, listenerApp.GetHttpListenerUri(HttpListenerPath));
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
            await using var listenerApp = await CreateInMemoryListenerAppAsync();

            var manager = CreateInMemoryManager(GrpcManagerName, listenerApp.GrpcListenerUri);
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
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var client = listenerApp.CreateHttpClient();
            var request = CreateDispatchRequest(EntityEventStage.Saving);

            var createResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, EntityEventListenerApiDefaults.HttpCreateActionPath),
                request);
            var response = await client.PostAsJsonAsync(
                BuildStagePath(HttpListenerPath, EntityEventStage.Saving),
                request);
            var deleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, EntityEventListenerApiDefaults.HttpDeleteActionPath),
                request);

            createResponse.EnsureSuccessStatusCode();
            response.EnsureSuccessStatusCode();
            deleteResponse.EnsureSuccessStatusCode();
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
            using var client = listenerApp.CreateHttpClient();
            var firstPath = "/entity-event-listener/transport-a";
            var secondPath = "/entity-event-listener/transport-b";
            var firstRequest = CreateDispatchRequest(EntityEventStage.Saving, "TransportManagerA");
            var secondRequest = CreateDispatchRequest(EntityEventStage.Saving, "TransportManagerB");

            var firstCreateResponse = await client.PostAsJsonAsync(
                BuildActionPath(firstPath, EntityEventListenerApiDefaults.HttpCreateActionPath),
                firstRequest);
            var firstResponse = await client.PostAsJsonAsync(
                BuildStagePath(firstPath, EntityEventStage.Saving),
                firstRequest);
            var firstDeleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(firstPath, EntityEventListenerApiDefaults.HttpDeleteActionPath),
                firstRequest);
            var secondCreateResponse = await client.PostAsJsonAsync(
                BuildActionPath(secondPath, EntityEventListenerApiDefaults.HttpCreateActionPath),
                secondRequest);
            var secondResponse = await client.PostAsJsonAsync(
                BuildStagePath(secondPath, EntityEventStage.Saving),
                secondRequest);
            var secondDeleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(secondPath, EntityEventListenerApiDefaults.HttpDeleteActionPath),
                secondRequest);

            firstCreateResponse.EnsureSuccessStatusCode();
            firstResponse.EnsureSuccessStatusCode();
            firstDeleteResponse.EnsureSuccessStatusCode();
            secondCreateResponse.EnsureSuccessStatusCode();
            secondResponse.EnsureSuccessStatusCode();
            secondDeleteResponse.EnsureSuccessStatusCode();
            Assert.Equal(new[] { "saving:departments", "saving:departments" }, TransportEventSink.Events);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateDbBackedListenerAppAsync.
        /// </summary>
        private static Task<EntityEventListenerTestApplication> CreateDbBackedListenerAppAsync()
        {
            ResetAllState();
            return EntityEventListenerTestApplication.StartAsync(builder =>
            {
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
            });
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateInMemoryListenerAppAsync.
        /// </summary>
        private static Task<EntityEventListenerTestApplication> CreateInMemoryListenerAppAsync()
        {
            ResetAllState();
            return EntityEventListenerTestApplication.StartAsync(builder =>
            {
                builder.AddTitanicDb(config =>
                {
                    config.DefaultProviderName = "EventListenerInMemory";
                    config.Providers =
                    [
                        new DbProviderConfig
                        {
                            Name = "EventListenerInMemory",
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
                            Name = HttpManagerName,
                            DbProviderName = "EventListenerInMemory",
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
                            DbProviderName = "EventListenerInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.Grpc
                            }
                        }
                    ];
                });
            });
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateMultiManagerListenerAppAsync.
        /// </summary>
        private static Task<EntityEventListenerTestApplication> CreateMultiManagerListenerAppAsync()
        {
            ResetAllState();
            return EntityEventListenerTestApplication.StartAsync(builder =>
            {
                builder.AddTitanicDb(config =>
                {
                    config.DefaultProviderName = "EventListenerInMemory";
                    config.Providers =
                    [
                        new DbProviderConfig
                        {
                            Name = "EventListenerInMemory",
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
                            Name = "TransportManagerA",
                            DbProviderName = "EventListenerInMemory",
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
                            DbProviderName = "EventListenerInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.Http,
                                Path = "/entity-event-listener/transport-b"
                            }
                        }
                    ];
                });
            });
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
        /// Инициализирует новый экземпляр CreateInMemoryManager.
        /// </summary>
        private static BaseEntityManager CreateInMemoryManager(string managerName, string? eventListener)
        {
            var provider = new EntityEventInMemoryDbProvider("in-memory", new PostgresEngine());
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
                DispatchId = Guid.NewGuid().ToString("N"),
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
        /// Создаёт путь HTTP-действия listener API.
        /// </summary>
        /// <param name="basePath">Базовый путь listener API.</param>
        /// <param name="actionPath">Относительный путь действия.</param>
        /// <returns>Полный путь HTTP-действия.</returns>
        private static string BuildActionPath(string basePath, string actionPath)
        {
            return $"{basePath.TrimEnd('/')}/{actionPath}";
        }

        /// <summary>
        /// Создаёт путь HTTP endpoint-а конкретной стадии событийного pipeline.
        /// </summary>
        /// <param name="basePath">Базовый путь listener API.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Полный путь HTTP endpoint-а стадии.</returns>
        private static string BuildStagePath(string basePath, EntityEventStage stage)
        {
            return BuildActionPath(basePath, EntityEventListenerApiDefaults.GetHttpActionPath(stage));
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
        private static void EnsureIntegrationDatabase()
        {
            try
            {
                var fixture = new IntegrationTestFixture();
                fixture.TruncateAll();
            }
            catch (Exception ex)
            {
                Skip.If(true, $"PostgreSQL is not available, skipping integration test. Reason: {ex.Message}");
            }
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

        #endregion Members
    }
}


