using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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

        private const string HttpManagerName = "RemoteLifetimeManager";
        private const string WebSocketManagerName = "RemoteLifetimeManagerWebSocket";
        private const string HttpListenerPath = "/entity-event-listener/lifetime";
        private const string WebSocketListenerPath = "/entity-event-listener/lifetime-ws";
        private static readonly JsonSerializerOptions WebSocketJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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

            await CreateAsync(client, HttpListenerPath, dispatchId, HttpManagerName);
            await DispatchAsync(client, HttpListenerPath, EntityEventStage.Saving, dispatchId, HttpManagerName);
            await DispatchAsync(client, HttpListenerPath, EntityEventStage.Inserting, dispatchId, HttpManagerName);
            await DispatchAsync(client, HttpListenerPath, EntityEventStage.Saved, dispatchId, HttpManagerName);
            await DeleteAsync(client, HttpListenerPath, dispatchId, HttpManagerName);
            await CreateAsync(client, HttpListenerPath, dispatchId, HttpManagerName);
            await DispatchAsync(client, HttpListenerPath, EntityEventStage.Saving, dispatchId, HttpManagerName);

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

            await CreateAsync(client, HttpListenerPath, dispatchId, HttpManagerName);
            await DispatchAsync(client, HttpListenerPath, EntityEventStage.Saving, dispatchId, HttpManagerName);
            await Task.Delay(150);
            await DispatchAsync(client, HttpListenerPath, EntityEventStage.Inserting, dispatchId, HttpManagerName);

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

            var manager = CreateRemoteManager(HttpManagerName, listenerApp.GetHttpListenerUri(HttpListenerPath));
            var entity = manager.Create<OrmDepartmentEntity>(CreateUserConnection())
                .Set(nameof(OrmDepartmentEntity.Name), $"lifetime-{Guid.NewGuid():N}");

            entity.Save();

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(4, instanceIds.Count);
            Assert.Single(instanceIds.Distinct());
        }

        /// <summary>
        /// Проверяет, что remote WebSocket listener переиспользуется до финальной стадии и удаляется после неё.
        /// </summary>
        [Fact]
        public async Task RemoteWebSocketListener_ShouldReuseInstanceUntilFinalStageAndReleaseAfterSaved()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateListenerAppAsync();
            using var socket = await ConnectWebSocketAsync(listenerApp, WebSocketListenerPath);
            var dispatchId = Guid.NewGuid().ToString("N");

            await SendWebSocketAsync(socket, EntityEventWebSocketAction.Create, EntityEventStage.Saving, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.ExecuteStage, EntityEventStage.Saving, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.ExecuteStage, EntityEventStage.Inserting, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.ExecuteStage, EntityEventStage.Saved, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.Delete, EntityEventStage.Saved, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.Create, EntityEventStage.Saving, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.ExecuteStage, EntityEventStage.Saving, dispatchId, WebSocketManagerName);

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(4, instanceIds.Count);
            Assert.Equal(instanceIds[0], instanceIds[1]);
            Assert.Equal(instanceIds[0], instanceIds[2]);
            Assert.NotEqual(instanceIds[0], instanceIds[3]);
        }

        /// <summary>
        /// Проверяет, что remote WebSocket listener удаляется после истечения idle timeout.
        /// </summary>
        [Fact]
        public async Task RemoteWebSocketListener_ShouldExpireInstance_WhenIdleTimeoutPassed()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateListenerAppAsync(TimeSpan.FromMilliseconds(50));
            using var socket = await ConnectWebSocketAsync(listenerApp, WebSocketListenerPath);
            var dispatchId = Guid.NewGuid().ToString("N");

            await SendWebSocketAsync(socket, EntityEventWebSocketAction.Create, EntityEventStage.Saving, dispatchId, WebSocketManagerName);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.ExecuteStage, EntityEventStage.Saving, dispatchId, WebSocketManagerName);
            await Task.Delay(150);
            await SendWebSocketAsync(socket, EntityEventWebSocketAction.ExecuteStage, EntityEventStage.Inserting, dispatchId, WebSocketManagerName);

            var instanceIds = LifetimeEventSink.InstanceIds;
            Assert.Equal(2, instanceIds.Count);
            Assert.NotEqual(instanceIds[0], instanceIds[1]);
        }

        /// <summary>
        /// Проверяет, что client-side WebSocket dispatch передаёт один DispatchId на весь Save pipeline.
        /// </summary>
        [Fact]
        public async Task EntitySave_WithRemoteWebSocketListener_ShouldReuseOneRemoteListenerInstance()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateListenerAppAsync();

            var manager = CreateRemoteManager(WebSocketManagerName, listenerApp.GetWebSocketListenerUri(WebSocketListenerPath));
            var entity = manager.Create<OrmDepartmentEntity>(CreateUserConnection())
                .Set(nameof(OrmDepartmentEntity.Name), $"lifetime-ws-{Guid.NewGuid():N}");

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
        private static async Task CreateAsync(HttpClient client, string listenerPath, string dispatchId, string managerName)
        {
            var response = await client.PostAsJsonAsync(
                BuildActionPath(listenerPath, HttpEntityEventProvider.CreateActionPath),
                CreateDispatchRequest(EntityEventStage.Saving, dispatchId, managerName));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Выполняет HTTP-запрос конкретной стадии к listener API.
        /// </summary>
        /// <param name="client">HTTP-клиент тестового приложения.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        private static async Task DispatchAsync(
            HttpClient client,
            string listenerPath,
            EntityEventStage stage,
            string dispatchId,
            string managerName)
        {
            var response = await client.PostAsJsonAsync(
                BuildStagePath(listenerPath, stage),
                CreateDispatchRequest(stage, dispatchId, managerName));
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Выполняет HTTP-запрос удаления remote listener-а.
        /// </summary>
        /// <param name="client">HTTP-клиент тестового приложения.</param>
        /// <param name="dispatchId">Идентификатор обработки одной сущности.</param>
        private static async Task DeleteAsync(HttpClient client, string listenerPath, string dispatchId, string managerName)
        {
            var response = await client.PostAsJsonAsync(
                BuildActionPath(listenerPath, HttpEntityEventProvider.DeleteActionPath),
                CreateDispatchRequest(EntityEventStage.Saved, dispatchId, managerName));
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
                            Name = HttpManagerName,
                            DbProviderName = "RemoteLifetimeInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.Http,
                                Path = HttpListenerPath,
                                ListenerInstanceIdleTimeout = listenerIdleTimeout ?? TimeSpan.FromMinutes(5)
                            }
                        },
                        new EntityManagerSettings
                        {
                            Name = WebSocketManagerName,
                            DbProviderName = "RemoteLifetimeInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.WebSocket,
                                Path = WebSocketListenerPath,
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
        private static BaseEntityManager CreateRemoteManager(string managerName, string listenerUri)
        {
            var manager = new EntityDbManager();
            manager.Initialize(
                managerName,
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
        private static EntityEventDispatchRequest CreateDispatchRequest(EntityEventStage stage, string dispatchId, string managerName)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = managerName,
                TableName = "departments",
                DispatchId = dispatchId,
                Stage = stage,
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
        private static string BuildActionPath(string listenerPath, string actionPath)
        {
            return $"{listenerPath.TrimEnd('/')}/{actionPath}";
        }

        /// <summary>
        /// Создаёт путь HTTP endpoint-а конкретной стадии событийного pipeline.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Полный путь HTTP endpoint-а стадии.</returns>
        private static string BuildStagePath(string listenerPath, EntityEventStage stage)
        {
            return BuildActionPath(listenerPath, HttpEntityEventProvider.GetActionPath(stage));
        }

        /// <summary>
        /// Открывает WebSocket-соединение с тестовым listener API.
        /// </summary>
        /// <param name="listenerApp">Тестовое приложение listener API.</param>
        /// <param name="path">Путь WebSocket endpoint-а.</param>
        /// <returns>Подключённый WebSocket-клиент.</returns>
        private static async Task<ClientWebSocket> ConnectWebSocketAsync(EntityEventListenerTestApplication listenerApp, string path)
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(listenerApp.GetWebSocketListenerUri(path)), CancellationToken.None);
            return socket;
        }

        /// <summary>
        /// Отправляет transport-запрос по WebSocket и проверяет успешность transport-ответа.
        /// </summary>
        /// <param name="socket">Активный WebSocket-клиент.</param>
        /// <param name="action">Команда listener API.</param>
        /// <param name="stage">Стадия pipeline.</param>
        /// <param name="dispatchId">Идентификатор обработки.</param>
        /// <param name="managerName">Имя целевого менеджера.</param>
        private static async Task SendWebSocketAsync(
            ClientWebSocket socket,
            EntityEventWebSocketAction action,
            EntityEventStage stage,
            string dispatchId,
            string managerName)
        {
            var payload = JsonSerializer.Serialize(
                new EntityEventWebSocketRequest
                {
                    Action = action,
                    Request = CreateDispatchRequest(stage, dispatchId, managerName)
                },
                WebSocketJsonOptions);

            await SendWebSocketTextAsync(socket, payload);
            var responsePayload = await ReceiveWebSocketTextAsync(socket);
            var response = JsonSerializer.Deserialize<EntityEventWebSocketResponse>(
                responsePayload,
                WebSocketJsonOptions)
                ?? throw new InvalidOperationException("WebSocket listener вернул пустой transport-ответ.");

            Assert.True(response.Response.Success, response.Response.ErrorMessage ?? "WebSocket listener вернул ошибку.");
        }

        /// <summary>
        /// Отправляет одно текстовое сообщение по WebSocket.
        /// </summary>
        /// <param name="socket">Активный WebSocket-клиент.</param>
        /// <param name="payload">Текст transport-сообщения.</param>
        private static Task SendWebSocketTextAsync(ClientWebSocket socket, string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            return socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
        }

        /// <summary>
        /// Читает одно текстовое сообщение из WebSocket.
        /// </summary>
        /// <param name="socket">Активный WebSocket-клиент.</param>
        /// <returns>Текст transport-сообщения.</returns>
        private static async Task<string> ReceiveWebSocketTextAsync(ClientWebSocket socket)
        {
            var buffer = new byte[4096];
            using var stream = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new WebSocketException("Listener закрыл WebSocket до чтения transport-ответа.");
                }

                if (result.Count > 0)
                {
                    stream.Write(buffer, 0, result.Count);
                }

                if (result.EndOfMessage)
                {
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
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
