using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Configuration;
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
    public sealed class EntityEventTransportTests
    {
        #region Members

        private const string LocalManagerName = "TransportManagerLocal";
        private const string HttpManagerName = "TransportManagerHttp";
        private const string GrpcManagerName = "TransportManagerGrpc";
        private const string WebSocketManagerName = "TransportManagerWebSocket";
        private const string HttpListenerPath = "/entity-event-listener/transport-http";
        private const string WebSocketListenerPath = "/entity-event-listener/transport-ws";
        private const string FilledDescription = "filled-by-event-listener";
        private const string OldDescription = "old-description";
        private const string EmployeeDepartmentAlias = "DepartmentLookup";
        private const string EmployeeDepartmentDisplayValue = "Finance";
        private static readonly JsonSerializerOptions WebSocketJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
        /// Инициализирует новый экземпляр Entity_Save_WithRemoteWebSocketEventListener_ShouldFillFieldAndPersistIt.
        /// </summary>
        [SkippableFact]
        public async Task Entity_Save_WithRemoteWebSocketEventListener_ShouldFillFieldAndPersistIt()
        {
            EnsureIntegrationDatabase();
            ResetRuntimeState();
            await using var listenerApp = await CreateDbBackedListenerAppAsync();

            var manager = CreateDbBackedManager(WebSocketManagerName, listenerApp.GetWebSocketListenerUri(WebSocketListenerPath));
            var entity = CreateDepartmentEntity(manager, "transport-websocket");

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
        /// Инициализирует новый экземпляр Entity_Save_WithRemoteWebSocketEventListener_ShouldPassUserConnectionToListener.
        /// </summary>
        [Fact]
        public async Task Entity_Save_WithRemoteWebSocketEventListener_ShouldPassUserConnectionToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();

            var manager = CreateInMemoryManager(WebSocketManagerName, listenerApp.GetWebSocketListenerUri(WebSocketListenerPath));
            var entity = CreateDepartmentEntity(manager, "transport-websocket-user");

            entity.Save();

            AssertCapturedUserConnection();
        }

        /// <summary>
        /// Проверяет, что remote gRPC provider передаёт старые значения существующей сущности.
        /// </summary>
        [Fact]
        public async Task Entity_Save_WithRemoteGrpcEventListener_ShouldPassOldValuesToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();

            var manager = CreateInMemoryManager(GrpcManagerName, listenerApp.GrpcListenerUri);
            var entity = CreateExistingDepartmentEntity(manager, "transport-grpc-old", OldDescription);
            entity.Set(nameof(OrmDepartmentEntity.Description), "updated-description");

            entity.Save();

            Assert.Equal(OldDescription, TransportEventSink.LastOldDescription);
        }

        /// <summary>
        /// Проверяет, что remote WebSocket provider передаёт старые значения существующей сущности.
        /// </summary>
        [Fact]
        public async Task Entity_Save_WithRemoteWebSocketEventListener_ShouldPassOldValuesToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();

            var manager = CreateInMemoryManager(WebSocketManagerName, listenerApp.GetWebSocketListenerUri(WebSocketListenerPath));
            var entity = CreateExistingDepartmentEntity(manager, "transport-websocket-old", OldDescription);
            entity.Set(nameof(OrmDepartmentEntity.Description), "updated-description");

            entity.Save();

            Assert.Equal(OldDescription, TransportEventSink.LastOldDescription);
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
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.CreateActionPath),
                request);
            var response = await client.PostAsJsonAsync(
                BuildStagePath(HttpListenerPath, EntityEventStage.Saving),
                request);
            var deleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.DeleteActionPath),
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
        /// Проверяет, что HTTP listener API передаёт старые значения в локальный listener.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_HttpEndpoint_ShouldPassOldValuesToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var client = listenerApp.CreateHttpClient();
            var request = CreateDispatchRequest(EntityEventStage.Updating);
            request.Values[nameof(OrmDepartmentEntity.Description)] = "updated-description";
            request.OldValues[nameof(OrmDepartmentEntity.Description)] = OldDescription;

            var createResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.CreateActionPath),
                request);
            var response = await client.PostAsJsonAsync(
                BuildStagePath(HttpListenerPath, EntityEventStage.Updating),
                request);
            var deleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.DeleteActionPath),
                request);

            createResponse.EnsureSuccessStatusCode();
            response.EnsureSuccessStatusCode();
            deleteResponse.EnsureSuccessStatusCode();
            Assert.Equal(OldDescription, TransportEventSink.LastOldDescription);
        }

        /// <summary>
        /// Проверяет, что gRPC listener API передаёт старые значения в локальный listener.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_GrpcEndpoint_ShouldPassOldValuesToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var channel = GrpcChannel.ForAddress(listenerApp.GrpcBaseAddress);
            var client = new EntityEventListenerGrpc.EntityEventListenerGrpcClient(channel);
            var request = CreateGrpcDispatchRequest(EntityEventStage.Updating);
            request.Values[nameof(OrmDepartmentEntity.Description)] = new EntityEventGrpcValue
            {
                StringValue = "updated-description"
            };
            request.OldValues[nameof(OrmDepartmentEntity.Description)] = new EntityEventGrpcValue
            {
                StringValue = OldDescription
            };

            client.Create(request);
            var response = client.OnUpdating(request);
            client.Delete(request);

            Assert.True(response.Success);
            Assert.Equal(OldDescription, TransportEventSink.LastOldDescription);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityEventListenerApi_WebSocketEndpoint_ShouldReturnSuccessAndMutatedValues.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_WebSocketEndpoint_ShouldReturnSuccessAndMutatedValues()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var socket = await ConnectWebSocketAsync(listenerApp, WebSocketListenerPath);
            var request = CreateDispatchRequest(EntityEventStage.Saving, WebSocketManagerName);

            var createResponse = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.Create, request);
            var response = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.ExecuteStage, request);
            var deleteResponse = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.Delete, request);

            Assert.True(createResponse.Response.Success);
            Assert.True(response.Response.Success);
            Assert.True(deleteResponse.Response.Success);
            Assert.Equal(new[] { "saving:departments" }, TransportEventSink.Events);
            Assert.Equal(FilledDescription, GetStringValue(response.Response.Values, nameof(OrmDepartmentEntity.Description)));
        }

        /// <summary>
        /// Проверяет, что WebSocket listener API передаёт старые значения в локальный listener.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_WebSocketEndpoint_ShouldPassOldValuesToListener()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var socket = await ConnectWebSocketAsync(listenerApp, WebSocketListenerPath);
            var request = CreateDispatchRequest(EntityEventStage.Updating, WebSocketManagerName);
            request.Values[nameof(OrmDepartmentEntity.Description)] = "updated-description";
            request.OldValues[nameof(OrmDepartmentEntity.Description)] = OldDescription;

            var createResponse = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.Create, request);
            var response = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.ExecuteStage, request);
            var deleteResponse = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.Delete, request);

            Assert.True(createResponse.Response.Success);
            Assert.True(response.Response.Success);
            Assert.True(deleteResponse.Response.Success);
            Assert.Equal(OldDescription, TransportEventSink.LastOldDescription);
        }

        /// <summary>
        /// Проверяет, что HTTP listener API передаёт полный снимок Entity с custom alias и display-значением ссылочной колонки.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_HttpEndpoint_ShouldPreserveEntitySnapshotWithCustomAlias()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var client = listenerApp.CreateHttpClient();
            var manager = CreateInMemoryManager(HttpManagerName, null);
            var entity = CreateExistingEmployeeEntityWithReferenceAlias(manager);
            var request = CreateEmployeeDispatchRequest(entity);

            var createResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.CreateActionPath),
                request);
            var response = await client.PostAsJsonAsync(
                BuildStagePath(HttpListenerPath, EntityEventStage.Saving),
                request);
            var deleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.DeleteActionPath),
                request);

            createResponse.EnsureSuccessStatusCode();
            response.EnsureSuccessStatusCode();
            deleteResponse.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<EntityEventDispatchResponse>();

            Assert.NotNull(body);
            Assert.True(body.Success);
            Assert.True(TransportEventSink.LastEmployeeDepartmentPathResolved);
            Assert.Equal(EmployeeDepartmentAlias, TransportEventSink.LastEmployeeDepartmentAlias);
            Assert.Equal(42, TransportEventSink.LastEmployeeDepartmentValue);
            Assert.Equal(EmployeeDepartmentDisplayValue, TransportEventSink.LastEmployeeDepartmentDisplayValue);
            Assert.NotNull(body.Entity);
            Assert.Equal(EmployeeDepartmentAlias, body.Entity!.Paths[nameof(OrmEmployeeEntity.DepartmentId)]);
        }

        /// <summary>
        /// Проверяет, что gRPC listener API передаёт полный снимок Entity с custom alias и display-значением ссылочной колонки.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_GrpcEndpoint_ShouldPreserveEntitySnapshotWithCustomAlias()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var channel = GrpcChannel.ForAddress(listenerApp.GrpcBaseAddress);
            var client = new EntityEventListenerGrpc.EntityEventListenerGrpcClient(channel);
            var manager = CreateInMemoryManager(GrpcManagerName, null);
            var entity = CreateExistingEmployeeEntityWithReferenceAlias(manager);
            var request = CreateGrpcDispatchRequest(CreateEmployeeDispatchRequest(entity, GrpcManagerName));

            client.Create(request);
            var response = client.OnSaving(request);
            client.Delete(request);

            Assert.True(response.Success);
            Assert.True(TransportEventSink.LastEmployeeDepartmentPathResolved);
            Assert.Equal(EmployeeDepartmentAlias, TransportEventSink.LastEmployeeDepartmentAlias);
            Assert.Equal(42, TransportEventSink.LastEmployeeDepartmentValue);
            Assert.Equal(EmployeeDepartmentDisplayValue, TransportEventSink.LastEmployeeDepartmentDisplayValue);
            Assert.False(string.IsNullOrWhiteSpace(response.Entity.TableName));
        }

        /// <summary>
        /// Проверяет, что WebSocket listener API передаёт полный снимок Entity с custom alias и display-значением ссылочной колонки.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_WebSocketEndpoint_ShouldPreserveEntitySnapshotWithCustomAlias()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var socket = await ConnectWebSocketAsync(listenerApp, WebSocketListenerPath);
            var manager = CreateInMemoryManager(WebSocketManagerName, null);
            var entity = CreateExistingEmployeeEntityWithReferenceAlias(manager);
            var request = CreateEmployeeDispatchRequest(entity, WebSocketManagerName);

            var createResponse = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.Create, request);
            var response = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.ExecuteStage, request);
            var deleteResponse = await SendWebSocketRequestAsync(socket, EntityEventWebSocketAction.Delete, request);

            Assert.True(createResponse.Response.Success);
            Assert.True(response.Response.Success);
            Assert.True(deleteResponse.Response.Success);
            Assert.True(TransportEventSink.LastEmployeeDepartmentPathResolved);
            Assert.Equal(EmployeeDepartmentAlias, TransportEventSink.LastEmployeeDepartmentAlias);
            Assert.Equal(42, TransportEventSink.LastEmployeeDepartmentValue);
            Assert.Equal(EmployeeDepartmentDisplayValue, TransportEventSink.LastEmployeeDepartmentDisplayValue);
            Assert.NotNull(response.Response.Entity);
            Assert.Equal(EmployeeDepartmentAlias, response.Response.Entity!.Paths[nameof(OrmEmployeeEntity.DepartmentId)]);
        }

        /// <summary>
        /// Проверяет, что HTTP и gRPC возвращают эквивалентный snapshot одной и той же Entity.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_HttpGrpcAndWebSocketContracts_ShouldReturnEquivalentEntitySnapshots()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateInMemoryListenerAppAsync();
            using var httpClient = listenerApp.CreateHttpClient();
            using var channel = GrpcChannel.ForAddress(listenerApp.GrpcBaseAddress);
            var grpcClient = new EntityEventListenerGrpc.EntityEventListenerGrpcClient(channel);
            using var webSocket = await ConnectWebSocketAsync(listenerApp, WebSocketListenerPath);

            var sourceManager = CreateInMemoryManager(HttpManagerName, null);
            var sourceEntity = CreateExistingEmployeeEntityWithReferenceAlias(sourceManager);
            var httpRequest = CreateEmployeeDispatchRequest(sourceEntity, HttpManagerName);
            var grpcRequest = CreateGrpcDispatchRequest(CreateEmployeeDispatchRequest(sourceEntity, GrpcManagerName));
            var webSocketRequest = CreateEmployeeDispatchRequest(sourceEntity, WebSocketManagerName);

            await httpClient.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.CreateActionPath),
                httpRequest);
            var httpResponse = await httpClient.PostAsJsonAsync(
                BuildStagePath(HttpListenerPath, EntityEventStage.Saving),
                httpRequest);
            await httpClient.PostAsJsonAsync(
                BuildActionPath(HttpListenerPath, HttpEntityEventProvider.DeleteActionPath),
                httpRequest);

            grpcClient.Create(grpcRequest);
            var grpcResponse = grpcClient.OnSaving(grpcRequest);
            grpcClient.Delete(grpcRequest);

            var webSocketCreateResponse = await SendWebSocketRequestAsync(webSocket, EntityEventWebSocketAction.Create, webSocketRequest);
            var webSocketResponse = await SendWebSocketRequestAsync(webSocket, EntityEventWebSocketAction.ExecuteStage, webSocketRequest);
            var webSocketDeleteResponse = await SendWebSocketRequestAsync(webSocket, EntityEventWebSocketAction.Delete, webSocketRequest);

            httpResponse.EnsureSuccessStatusCode();
            var httpBody = await httpResponse.Content.ReadFromJsonAsync<EntityEventDispatchResponse>();

            Assert.NotNull(httpBody);
            Assert.NotNull(httpBody!.Entity);
            Assert.True(grpcResponse.Success);
            Assert.NotNull(grpcResponse.Entity);
            Assert.True(webSocketCreateResponse.Response.Success);
            Assert.True(webSocketResponse.Response.Success);
            Assert.True(webSocketDeleteResponse.Response.Success);
            Assert.NotNull(webSocketResponse.Response.Entity);

            AssertEquivalentSnapshots(httpBody.Entity!, grpcResponse.Entity);
            AssertEquivalentSnapshots(httpBody.Entity!, ToGrpcSnapshot(webSocketResponse.Response.Entity!));
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
                BuildActionPath(firstPath, HttpEntityEventProvider.CreateActionPath),
                firstRequest);
            var firstResponse = await client.PostAsJsonAsync(
                BuildStagePath(firstPath, EntityEventStage.Saving),
                firstRequest);
            var firstDeleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(firstPath, HttpEntityEventProvider.DeleteActionPath),
                firstRequest);
            var secondCreateResponse = await client.PostAsJsonAsync(
                BuildActionPath(secondPath, HttpEntityEventProvider.CreateActionPath),
                secondRequest);
            var secondResponse = await client.PostAsJsonAsync(
                BuildStagePath(secondPath, EntityEventStage.Saving),
                secondRequest);
            var secondDeleteResponse = await client.PostAsJsonAsync(
                BuildActionPath(secondPath, HttpEntityEventProvider.DeleteActionPath),
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
        /// Проверяет, что listener API мапит несколько WebSocket endpoint-ов из конфигурации менеджеров.
        /// </summary>
        [Fact]
        public async Task EntityEventListenerApi_ShouldMapMultipleWebSocketEndpointsFromManagerConfiguration()
        {
            ResetRuntimeState();
            await using var listenerApp = await CreateMultiManagerListenerAppAsync();
            using var firstSocket = await ConnectWebSocketAsync(listenerApp, "/entity-event-listener/transport-ws-a");
            using var secondSocket = await ConnectWebSocketAsync(listenerApp, "/entity-event-listener/transport-ws-b");
            var firstRequest = CreateDispatchRequest(EntityEventStage.Saving, "TransportManagerWsA");
            var secondRequest = CreateDispatchRequest(EntityEventStage.Saving, "TransportManagerWsB");

            var firstCreateResponse = await SendWebSocketRequestAsync(firstSocket, EntityEventWebSocketAction.Create, firstRequest);
            var firstResponse = await SendWebSocketRequestAsync(firstSocket, EntityEventWebSocketAction.ExecuteStage, firstRequest);
            var firstDeleteResponse = await SendWebSocketRequestAsync(firstSocket, EntityEventWebSocketAction.Delete, firstRequest);
            var secondCreateResponse = await SendWebSocketRequestAsync(secondSocket, EntityEventWebSocketAction.Create, secondRequest);
            var secondResponse = await SendWebSocketRequestAsync(secondSocket, EntityEventWebSocketAction.ExecuteStage, secondRequest);
            var secondDeleteResponse = await SendWebSocketRequestAsync(secondSocket, EntityEventWebSocketAction.Delete, secondRequest);

            Assert.True(firstCreateResponse.Response.Success);
            Assert.True(firstResponse.Response.Success);
            Assert.True(firstDeleteResponse.Response.Success);
            Assert.True(secondCreateResponse.Response.Success);
            Assert.True(secondResponse.Response.Success);
            Assert.True(secondDeleteResponse.Response.Success);
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
                        },
                        new EntityManagerSettings
                        {
                            Name = WebSocketManagerName,
                            DbProviderName = IntegrationTestFixture.ProviderName,
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.WebSocket,
                                Path = WebSocketListenerPath
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
                        },
                        new EntityManagerSettings
                        {
                            Name = WebSocketManagerName,
                            DbProviderName = "EventListenerInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.WebSocket,
                                Path = WebSocketListenerPath
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
                        },
                        new EntityManagerSettings
                        {
                            Name = "TransportManagerWsA",
                            DbProviderName = "EventListenerInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.WebSocket,
                                Path = "/entity-event-listener/transport-ws-a"
                            }
                        },
                        new EntityManagerSettings
                        {
                            Name = "TransportManagerWsB",
                            DbProviderName = "EventListenerInMemory",
                            EntityModelNamespaces = [typeof(OrmDepartmentEntity).Namespace!],
                            EventListenerApi = new EntityManagerEventListenerApiSettings
                            {
                                Mode = EntityEventListenerApiMode.WebSocket,
                                Path = "/entity-event-listener/transport-ws-b"
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
        /// Создаёт существующую тестовую сущность со снимком старых значений.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="namePrefix">Префикс имени сущности.</param>
        /// <param name="description">Исходное описание сущности.</param>
        /// <returns>Существующая ORM-сущность.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateExistingDepartmentEntity(
            BaseEntityManager manager,
            string namePrefix,
            string description)
        {
            var builder = manager.Select<OrmDepartmentEntity>(CreateUserConnection());
            builder.AddColumn(nameof(OrmDepartmentEntity.Id));
            builder.AddColumn(nameof(OrmDepartmentEntity.Name));
            builder.AddColumn(nameof(OrmDepartmentEntity.Description));

            return builder.CreateRecord(new Dictionary<string, object?>
            {
                [nameof(OrmDepartmentEntity.Id)] = 1,
                [nameof(OrmDepartmentEntity.Name)] = $"{namePrefix}-{Guid.NewGuid():N}",
                [nameof(OrmDepartmentEntity.Description)] = description
            });
        }

        /// <summary>
        /// Создаёт существующую сущность сотрудника со ссылочной колонкой, выбранной через custom alias.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <returns>Существующая ORM-сущность сотрудника.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateExistingEmployeeEntityWithReferenceAlias(BaseEntityManager manager)
        {
            var builder = manager.Select<OrmEmployeeEntity>(CreateUserConnection());
            builder.AddColumn(nameof(OrmEmployeeEntity.Id));
            builder.AddColumn(nameof(OrmEmployeeEntity.Name));
            builder.AddColumn(nameof(OrmEmployeeEntity.DepartmentId), EmployeeDepartmentAlias);

            return builder.CreateRecord(new Dictionary<string, object?>
            {
                [nameof(OrmEmployeeEntity.Id)] = 1,
                [nameof(OrmEmployeeEntity.Name)] = $"employee-{Guid.NewGuid():N}",
                [EmployeeDepartmentAlias] = 42,
                [$"{EmployeeDepartmentAlias}_DisplayValue"] = EmployeeDepartmentDisplayValue
            });
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
                Stage = stage,
                IsNew = true,
                UserConnection = CreateUserConnection(),
                Values = new Dictionary<string, object?>
                {
                    [nameof(OrmDepartmentEntity.Name)] = "manual-http-dispatch"
                }
            };
        }

        /// <summary>
        /// Создаёт HTTP dispatch-запрос для сущности сотрудника с полным snapshot-снимком.
        /// </summary>
        /// <param name="entity">Исходная ORM-сущность.</param>
        /// <param name="managerName">Имя Entity manager-а.</param>
        /// <returns>Dispatch-запрос событийного listener-а.</returns>
        private static EntityEventDispatchRequest CreateEmployeeDispatchRequest(
            global::Titanic.Entity.Orm.Entity entity,
            string managerName = HttpManagerName)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = managerName,
                TableName = "employees",
                DispatchId = Guid.NewGuid().ToString("N"),
                Stage = EntityEventStage.Saving,
                IsNew = entity.IsNew,
                UserConnection = CreateUserConnection(),
                Values = entity.ToDictionary(),
                OldValues = entity.OldValues.ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase),
                Entity = CreateSnapshot(entity, "employees")
            };
        }

        /// <summary>
        /// Создаёт gRPC dispatch-запрос с указанной стадией.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>gRPC dispatch-запрос.</returns>
        private static EntityEventGrpcRequest CreateGrpcDispatchRequest(EntityEventStage stage)
        {
            return CreateGrpcDispatchRequest(CreateDispatchRequest(stage, GrpcManagerName));
        }

        /// <summary>
        /// Преобразует transport-запрос в gRPC dispatch-запрос.
        /// </summary>
        /// <param name="request">Transport-запрос.</param>
        /// <returns>gRPC dispatch-запрос.</returns>
        private static EntityEventGrpcRequest CreateGrpcDispatchRequest(EntityEventDispatchRequest request)
        {
            var grpcRequest = new EntityEventGrpcRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
                DispatchId = request.DispatchId,
                Stage = ToGrpcStage(request.Stage),
                IsNew = request.IsNew,
                Entity = request.Entity == null
                    ? new EntityEventGrpcEntitySnapshot()
                    : ToGrpcSnapshot(request.Entity),
                UserConnection = new EntityEventGrpcUserConnection
                {
                    UserId = request.UserConnection.UserId.ToString(),
                    Culture = new EntityEventGrpcUserCulture
                    {
                        Id = request.UserConnection.Culture.Id.ToString(),
                        Name = request.UserConnection.Culture.Name
                    }
                }
            };

            foreach (var value in request.Values)
            {
                grpcRequest.Values[value.Key] = ToGrpcValue(value.Value);
            }

            foreach (var value in request.OldValues)
            {
                grpcRequest.OldValues[value.Key] = ToGrpcValue(value.Value);
            }

            return grpcRequest;
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
            return BuildActionPath(basePath, HttpEntityEventProvider.GetActionPath(stage));
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
        /// Отправляет transport-запрос по WebSocket и читает transport-ответ.
        /// </summary>
        /// <param name="socket">Активный WebSocket-клиент.</param>
        /// <param name="action">Команда listener API.</param>
        /// <param name="request">Transport-запрос.</param>
        /// <returns>Transport-ответ listener API.</returns>
        private static async Task<EntityEventWebSocketResponse> SendWebSocketRequestAsync(
            ClientWebSocket socket,
            EntityEventWebSocketAction action,
            EntityEventDispatchRequest request)
        {
            var payload = JsonSerializer.Serialize(
                new EntityEventWebSocketRequest
                {
                    Action = action,
                    Request = request
                },
                WebSocketJsonOptions);

            await SendWebSocketTextAsync(socket, payload);
            var responsePayload = await ReceiveWebSocketTextAsync(socket);
            return JsonSerializer.Deserialize<EntityEventWebSocketResponse>(
                responsePayload,
                WebSocketJsonOptions)
                ?? throw new InvalidOperationException("WebSocket listener вернул пустой transport-ответ.");
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
            TransportEmployeeEventListener.IsEnabled = true;
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

        /// <summary>
        /// Создаёт transport-снимок сущности из публичного состояния ORM-сущности.
        /// </summary>
        /// <param name="entity">Исходная ORM-сущность.</param>
        /// <param name="tableName">Имя таблицы сущности.</param>
        /// <returns>Снимок сущности.</returns>
        private static EntityEventEntitySnapshot CreateSnapshot(global::Titanic.Entity.Orm.Entity entity, string tableName)
        {
            return new EntityEventEntitySnapshot
            {
                TableName = tableName,
                IsNew = entity.IsNew,
                Paths = entity.Paths.ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase),
                Columns = entity.Values.ToDictionary(
                    x => x.Key,
                    x => new EntityEventColumnSnapshot
                    {
                        Alias = x.Key,
                        DataValueType = (int)x.Value.DataValueType,
                        IsReference = x.Value is global::Titanic.Entity.Orm.ReferenceColumnValue,
                        Value = x.Value.Value,
                        DisplayValue = x.Value.DisplayValue
                    },
                    StringComparer.OrdinalIgnoreCase),
                OldValues = entity.OldValues.ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Преобразует CLR-значение в типизированное protobuf-значение для тестового gRPC dispatch-а.
        /// </summary>
        /// <param name="value">CLR-значение.</param>
        /// <returns>Типизированное protobuf-значение.</returns>
        private static EntityEventGrpcValue ToGrpcValue(object? value)
        {
            return value switch
            {
                null => new EntityEventGrpcValue { NullValue = true },
                bool boolValue => new EntityEventGrpcValue { BoolValue = boolValue },
                string stringValue => new EntityEventGrpcValue { StringValue = stringValue },
                char charValue => new EntityEventGrpcValue { StringValue = charValue.ToString() },
                byte byteValue => new EntityEventGrpcValue { Int32Value = byteValue },
                sbyte sbyteValue => new EntityEventGrpcValue { Int32Value = sbyteValue },
                short shortValue => new EntityEventGrpcValue { Int32Value = shortValue },
                ushort ushortValue => new EntityEventGrpcValue { Int32Value = ushortValue },
                int intValue => new EntityEventGrpcValue { Int32Value = intValue },
                uint uintValue when uintValue <= int.MaxValue => new EntityEventGrpcValue { Int32Value = (int)uintValue },
                uint uintValue => new EntityEventGrpcValue { Int64Value = uintValue },
                long longValue => new EntityEventGrpcValue { Int64Value = longValue },
                ulong ulongValue when ulongValue <= long.MaxValue => new EntityEventGrpcValue { Int64Value = (long)ulongValue },
                ulong ulongValue => new EntityEventGrpcValue { StringValue = ulongValue.ToString(CultureInfo.InvariantCulture) },
                float floatValue => new EntityEventGrpcValue { DoubleValue = floatValue },
                double doubleValue => new EntityEventGrpcValue { DoubleValue = doubleValue },
                decimal decimalValue => new EntityEventGrpcValue { DecimalValue = decimalValue.ToString(CultureInfo.InvariantCulture) },
                Guid guidValue => new EntityEventGrpcValue { GuidValue = guidValue.ToString() },
                DateTime dateTimeValue => new EntityEventGrpcValue { DateTimeValue = dateTimeValue.ToString("O", CultureInfo.InvariantCulture) },
                DateTimeOffset dateTimeOffsetValue => new EntityEventGrpcValue { DateTimeOffsetValue = dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture) },
                byte[] bytesValue => new EntityEventGrpcValue { BytesValue = ByteString.CopyFrom(bytesValue) },
                _ => new EntityEventGrpcValue { StringValue = value.ToString() ?? string.Empty }
            };
        }

        /// <summary>
        /// Преобразует стадию transport-контракта в gRPC enum для тестового запроса.
        /// </summary>
        /// <param name="stage">Стадия transport-контракта.</param>
        /// <returns>Стадия gRPC-контракта.</returns>
        private static EntityEventGrpcStage ToGrpcStage(EntityEventStage stage)
        {
            return stage switch
            {
                EntityEventStage.Saving => EntityEventGrpcStage.Saving,
                EntityEventStage.Saved => EntityEventGrpcStage.Saved,
                EntityEventStage.Inserting => EntityEventGrpcStage.Inserting,
                EntityEventStage.Inserted => EntityEventGrpcStage.Inserted,
                EntityEventStage.Updating => EntityEventGrpcStage.Updating,
                EntityEventStage.Updated => EntityEventGrpcStage.Updated,
                EntityEventStage.Deleting => EntityEventGrpcStage.Deleting,
                EntityEventStage.Deleted => EntityEventGrpcStage.Deleted,
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported entity event stage.")
            };
        }

        /// <summary>
        /// Преобразует snapshot Entity в gRPC-модель для тестового запроса.
        /// </summary>
        /// <param name="snapshot">Transport-snapshot Entity.</param>
        /// <returns>gRPC-snapshot Entity.</returns>
        private static EntityEventGrpcEntitySnapshot ToGrpcSnapshot(EntityEventEntitySnapshot snapshot)
        {
            var grpcSnapshot = new EntityEventGrpcEntitySnapshot
            {
                TableName = snapshot.TableName,
                IsNew = snapshot.IsNew
            };

            foreach (var path in snapshot.Paths)
            {
                grpcSnapshot.Paths[path.Key] = path.Value;
            }

            foreach (var column in snapshot.Columns)
            {
                grpcSnapshot.Columns[column.Key] = new EntityEventGrpcColumnSnapshot
                {
                    Alias = column.Value.Alias,
                    DataValueType = column.Value.DataValueType,
                    IsReference = column.Value.IsReference,
                    Value = ToGrpcValue(column.Value.Value),
                    DisplayValue = ToGrpcValue(column.Value.DisplayValue)
                };
            }

            foreach (var oldValue in snapshot.OldValues)
            {
                grpcSnapshot.OldValues[oldValue.Key] = ToGrpcValue(oldValue.Value);
            }

            return grpcSnapshot;
        }

        /// <summary>
        /// Сверяет эквивалентность HTTP и gRPC snapshot одной и той же Entity.
        /// </summary>
        /// <param name="httpSnapshot">Snapshot из HTTP-контракта.</param>
        /// <param name="grpcSnapshot">Snapshot из gRPC-контракта.</param>
        private static void AssertEquivalentSnapshots(
            EntityEventEntitySnapshot httpSnapshot,
            EntityEventGrpcEntitySnapshot grpcSnapshot)
        {
            Assert.Equal(httpSnapshot.TableName, grpcSnapshot.TableName);
            Assert.Equal(httpSnapshot.IsNew, grpcSnapshot.IsNew);
            Assert.Equal(httpSnapshot.Paths.Count, grpcSnapshot.Paths.Count);
            Assert.Equal(httpSnapshot.Columns.Count, grpcSnapshot.Columns.Count);
            Assert.Equal(httpSnapshot.OldValues.Count, grpcSnapshot.OldValues.Count);

            foreach (var path in httpSnapshot.Paths)
            {
                Assert.True(grpcSnapshot.Paths.ContainsKey(path.Key));
                Assert.Equal(path.Value, grpcSnapshot.Paths[path.Key]);
            }

            foreach (var column in httpSnapshot.Columns)
            {
                Assert.True(grpcSnapshot.Columns.ContainsKey(column.Key));
                var grpcColumn = grpcSnapshot.Columns[column.Key];

                Assert.Equal(column.Value.Alias, grpcColumn.Alias);
                Assert.Equal(column.Value.DataValueType, grpcColumn.DataValueType);
                Assert.Equal(column.Value.IsReference, grpcColumn.IsReference);
                AssertEquivalentGrpcValue(column.Value.Value, grpcColumn.Value);
                AssertEquivalentGrpcValue(column.Value.DisplayValue, grpcColumn.DisplayValue);
            }
        }

        /// <summary>
        /// Проверяет, что gRPC-значение эквивалентно CLR-значению из HTTP-контракта.
        /// </summary>
        /// <param name="expected">Ожидаемое CLR-значение.</param>
        /// <param name="actual">Фактическое gRPC-значение.</param>
        private static void AssertEquivalentGrpcValue(object? expected, EntityEventGrpcValue actual)
        {
            switch (expected)
            {
                case null:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.NullValue, actual.KindCase);
                    break;
                case bool boolValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.BoolValue, actual.KindCase);
                    Assert.Equal(boolValue, actual.BoolValue);
                    break;
                case string stringValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.StringValue, actual.KindCase);
                    Assert.Equal(stringValue, actual.StringValue);
                    break;
                case char charValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.StringValue, actual.KindCase);
                    Assert.Equal(charValue.ToString(), actual.StringValue);
                    break;
                case byte byteValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int32Value, actual.KindCase);
                    Assert.Equal(byteValue, actual.Int32Value);
                    break;
                case sbyte sbyteValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int32Value, actual.KindCase);
                    Assert.Equal(sbyteValue, actual.Int32Value);
                    break;
                case short shortValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int32Value, actual.KindCase);
                    Assert.Equal(shortValue, actual.Int32Value);
                    break;
                case ushort ushortValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int32Value, actual.KindCase);
                    Assert.Equal(ushortValue, actual.Int32Value);
                    break;
                case int intValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int32Value, actual.KindCase);
                    Assert.Equal(intValue, actual.Int32Value);
                    break;
                case uint uintValue when uintValue <= int.MaxValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int32Value, actual.KindCase);
                    Assert.Equal((int)uintValue, actual.Int32Value);
                    break;
                case uint uintValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int64Value, actual.KindCase);
                    Assert.Equal((long)uintValue, actual.Int64Value);
                    break;
                case long longValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int64Value, actual.KindCase);
                    Assert.Equal(longValue, actual.Int64Value);
                    break;
                case ulong ulongValue when ulongValue <= long.MaxValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.Int64Value, actual.KindCase);
                    Assert.Equal((long)ulongValue, actual.Int64Value);
                    break;
                case ulong ulongValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.StringValue, actual.KindCase);
                    Assert.Equal(ulongValue.ToString(CultureInfo.InvariantCulture), actual.StringValue);
                    break;
                case float floatValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.DoubleValue, actual.KindCase);
                    Assert.Equal(floatValue, actual.DoubleValue, 6);
                    break;
                case double doubleValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.DoubleValue, actual.KindCase);
                    Assert.Equal(doubleValue, actual.DoubleValue, 12);
                    break;
                case decimal decimalValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.DecimalValue, actual.KindCase);
                    Assert.Equal(decimalValue.ToString(CultureInfo.InvariantCulture), actual.DecimalValue);
                    break;
                case Guid guidValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.GuidValue, actual.KindCase);
                    Assert.Equal(guidValue.ToString(), actual.GuidValue);
                    break;
                case DateTime dateTimeValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.DateTimeValue, actual.KindCase);
                    Assert.Equal(dateTimeValue.ToString("O", CultureInfo.InvariantCulture), actual.DateTimeValue);
                    break;
                case DateTimeOffset dateTimeOffsetValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.DateTimeOffsetValue, actual.KindCase);
                    Assert.Equal(dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture), actual.DateTimeOffsetValue);
                    break;
                case byte[] bytesValue:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.BytesValue, actual.KindCase);
                    Assert.Equal(bytesValue, actual.BytesValue.ToByteArray());
                    break;
                default:
                    Assert.Equal(EntityEventGrpcValue.KindOneofCase.StringValue, actual.KindCase);
                    Assert.Equal(expected.ToString(), actual.StringValue);
                    break;
            }
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
            /// Фиксирует старые значения перед обновлением сущности.
            /// </summary>
            public override void OnUpdating(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (IsEnabled)
                {
                    TransportEventSink.SetOldDescription(GetOldDescription(entity));
                    TransportEventSink.Add($"updating:{_entityName}");
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

            /// <summary>
            /// Возвращает старое значение описания из снимка ORM-сущности.
            /// </summary>
            /// <param name="entity">Текущая ORM-сущность.</param>
            /// <returns>Старое описание сущности.</returns>
            private static string? GetOldDescription(global::Titanic.Entity.Orm.Entity entity)
            {
                return entity.OldValues.TryGetValue(nameof(OrmDepartmentEntity.Description), out var value)
                    ? value?.ToString()
                    : null;
            }
        }

        [EntityEventListener("employees")]
        private sealed class TransportEmployeeEventListener : BaseEntityEventListener
        {
            public static bool IsEnabled { get; set; }

            /// <summary>
            /// Инициализирует listener для сущности сотрудников.
            /// </summary>
            /// <param name="entityName">Имя Entity-таблицы.</param>
            public TransportEmployeeEventListener(string entityName)
            {
            }

            /// <summary>
            /// Проверяет, что удалённый listener получил ссылочную колонку по исходному ORM-пути, а не только по алиасу transport-а.
            /// </summary>
            public override void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
            {
                if (!IsEnabled)
                {
                    return;
                }

                var hasPath = entity.Paths.TryGetValue(nameof(OrmEmployeeEntity.DepartmentId), out var alias);
                TransportEventSink.SetEmployeeDepartmentSnapshot(
                    hasPath && entity.Contains(nameof(OrmEmployeeEntity.DepartmentId)),
                    alias,
                    entity.Get<int?>(nameof(OrmEmployeeEntity.DepartmentId)),
                    entity.GetDisplayValue<string>(nameof(OrmEmployeeEntity.DepartmentId)));
            }
        }

        private static class TransportEventSink
        {
            private static readonly object SyncRoot = new();
            private static readonly List<string> InternalEvents = [];
            private static UserConnection? _lastUserConnection;
            private static string? _lastOldDescription;
            private static bool _lastEmployeeDepartmentPathResolved;
            private static string? _lastEmployeeDepartmentAlias;
            private static int? _lastEmployeeDepartmentValue;
            private static string? _lastEmployeeDepartmentDisplayValue;

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

            public static string? LastOldDescription
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _lastOldDescription;
                    }
                }
            }

            public static bool LastEmployeeDepartmentPathResolved
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _lastEmployeeDepartmentPathResolved;
                    }
                }
            }

            public static string? LastEmployeeDepartmentAlias
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _lastEmployeeDepartmentAlias;
                    }
                }
            }

            public static int? LastEmployeeDepartmentValue
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _lastEmployeeDepartmentValue;
                    }
                }
            }

            public static string? LastEmployeeDepartmentDisplayValue
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return _lastEmployeeDepartmentDisplayValue;
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
            /// Сохраняет старое описание, полученное listener-ом.
            /// </summary>
            /// <param name="description">Старое описание сущности.</param>
            public static void SetOldDescription(string? description)
            {
                lock (SyncRoot)
                {
                    _lastOldDescription = description;
                }
            }

            /// <summary>
            /// Сохраняет данные ссылочной колонки сотрудника, прочитанные удалённым listener-ом.
            /// </summary>
            /// <param name="pathResolved">Удалось ли разрешить исходный ORM-путь.</param>
            /// <param name="alias">Алиас, восстановленный из снимка сущности.</param>
            /// <param name="value">Сырое значение ссылочной колонки.</param>
            /// <param name="displayValue">Display-значение ссылочной колонки.</param>
            public static void SetEmployeeDepartmentSnapshot(
                bool pathResolved,
                string? alias,
                int? value,
                string? displayValue)
            {
                lock (SyncRoot)
                {
                    _lastEmployeeDepartmentPathResolved = pathResolved;
                    _lastEmployeeDepartmentAlias = alias;
                    _lastEmployeeDepartmentValue = value;
                    _lastEmployeeDepartmentDisplayValue = displayValue;
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
                    _lastOldDescription = null;
                    _lastEmployeeDepartmentPathResolved = false;
                    _lastEmployeeDepartmentAlias = null;
                    _lastEmployeeDepartmentValue = null;
                    _lastEmployeeDepartmentDisplayValue = null;
                }
            }
        }

        #endregion Members
    }
}


