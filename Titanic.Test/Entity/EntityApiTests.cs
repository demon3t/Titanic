using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Configuration;
using Titanic.Db.Interfaces;
using Titanic.Db.PosgreSql;
using Titanic.Db.WebApplication;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Orm;
using Titanic.Entity.WebApplication;
using Titanic.Entity.WebApplication.Api;
using Titanic.Entity.WebApplication.Configuration;
using EntityManager = Titanic.Entity.EntityManager;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тесты автоматического HTTP API для Entity ORM.
    /// </summary>
    public sealed class EntityApiTests
    {
        #region Members
        private const string ApiPath = "/entity-api/test";
        private const string StructurePath = $"{ApiPath}/structure";
        private const string AuthHeader = "X-Test-Entity-Auth";

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_AllConfiguredEndpoints_ShouldBeMappedAutomatically.
        /// </summary>
        [Fact]
        public async Task EntityApi_AllConfiguredEndpoints_ShouldBeMappedAutomatically()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = app.GetTestClient();

            var operationResponse = await client.PostAsJsonAsync(ApiPath, CreateSelectOperationRequest());
            var batchResponse = await client.PostAsJsonAsync($"{ApiPath}/batch", CreateBatchRequest());
            var structureResponse = await client.GetAsync(StructurePath);

            Assert.Equal(HttpStatusCode.Forbidden, operationResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, batchResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, structureResponse.StatusCode);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_LegacyActionEndpoints_ShouldNotBeMappedAutomatically.
        /// </summary>
        [Fact]
        public async Task EntityApi_LegacyActionEndpoints_ShouldNotBeMappedAutomatically()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);
            var legacyEndpoints = new Dictionary<string, object>
            {
                ["select"] = CreateSelectRequest(),
                ["save"] = CreateSaveRequest(),
                ["update"] = CreateUpdateRequest(),
                ["delete"] = CreateDeleteRequest()
            };

            foreach (var endpoint in legacyEndpoints)
            {
                var response = await client.PostAsJsonAsync($"{ApiPath}/{endpoint.Key}", endpoint.Value);

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Select_ShouldReturnForbiddenWithoutMockAuthorization.
        /// </summary>
        [Fact]
        public async Task EntityApi_Select_ShouldReturnForbiddenWithoutMockAuthorization()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = app.GetTestClient();

            var response = await client.PostAsJsonAsync(ApiPath, CreateSelectOperationRequest());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Select_ShouldReturnRowsWithMockAuthorization.
        /// </summary>
        [Fact]
        public async Task EntityApi_Select_ShouldReturnRowsWithMockAuthorization()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, CreateSelectOperationRequest());

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.Single(root.EnumerateArray());
            Assert.Equal("Api Employee", root[0].GetProperty("Name").GetProperty("value").GetString());
            Assert.Contains("FROM", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("LIMIT", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Select_ShouldReturnBadRequestForUnknownColumn.
        /// </summary>
        [Fact]
        public async Task EntityApi_Select_ShouldReturnBadRequestForUnknownColumn()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, new EntityApiRequest
            {
                Operation = EntityApiOperationType.Select,
                Query = new ESQJsonModel
                {
                    TableName = "departments",
                    RowCount = 10,
                    Columns =
                    [
                        new ESQColumnJsonModel
                        {
                            Path = "Id"
                        },
                        new ESQColumnJsonModel
                        {
                            Path = "Email"
                        }
                    ]
                }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("Column", json);
            Assert.Contains("Email", json);
            Assert.Contains("departments", json);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_UpdateOperation_ShouldReturnBadRequestInsteadOfJsonBindingError.
        /// </summary>
        [Fact]
        public async Task EntityApi_UpdateOperation_ShouldReturnBadRequestInsteadOfJsonBindingError()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);
            using var content = new StringContent(
                """
                {
                  "operation": "Update",
                  "tableName": "departments",
                  "values": {
                    "Id": 1,
                    "Name": "Updated API Department",
                    "Description": "Updated from API test"
                  }
                }
                """,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(ApiPath, content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("not supported", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BadHttpRequestException", json, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Select_ShouldReturnNotFoundWhenAutoRegistrationDisabled.
        /// </summary>
        [Fact]
        public async Task EntityApi_Select_ShouldReturnNotFoundWhenAutoRegistrationDisabled()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: false);
            var client = CreateAuthorizedClient(app);

            var operationResponse = await client.PostAsJsonAsync(ApiPath, CreateSelectOperationRequest());
            var batchResponse = await client.PostAsJsonAsync($"{ApiPath}/batch", CreateBatchRequest());
            var structureResponse = await client.GetAsync(StructurePath);

            Assert.Equal(HttpStatusCode.NotFound, operationResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, batchResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, structureResponse.StatusCode);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Structure_ShouldReturnForbiddenWithoutMockAuthorization.
        /// </summary>
        [Fact]
        public async Task EntityApi_Structure_ShouldReturnForbiddenWithoutMockAuthorization()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = app.GetTestClient();

            var response = await client.GetAsync(StructurePath);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Structure_ShouldReturnManagerMetadataWithMockAuthorization.
        /// </summary>
        [Fact]
        public async Task EntityApi_Structure_ShouldReturnManagerMetadataWithMockAuthorization()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.GetAsync(StructurePath);

            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;

            var entities = root.GetProperty("entities").EnumerateArray().ToList();
            Assert.Contains(entities, x => x.GetProperty("tableName").GetString() == "employees");
            Assert.Contains(entities, x => x.GetProperty("tableName").GetString() == "departments");
            Assert.Contains(entities, x => x.GetProperty("tableName").GetString() == "addresses");

            var employee = entities.Single(x =>
                x.GetProperty("entityTypeName").GetString() == typeof(OrmEmployeeEntity).FullName);
            Assert.Equal("employees", employee.GetProperty("tableName").GetString());

            var columns = employee.GetProperty("columns").EnumerateArray().ToList();
            Assert.Contains(columns, x => x.GetProperty("propertyName").GetString() == "Name");
            Assert.Contains(columns, x => x.GetProperty("propertyName").GetString() == "Email");

            var departmentId = columns.Single(x => x.GetProperty("propertyName").GetString() == "DepartmentId");
            Assert.True(departmentId.GetProperty("isReference").GetBoolean());
            Assert.Equal("departments", departmentId.GetProperty("referenceTableName").GetString());
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Structure_ShouldRespectManagerScope.
        /// </summary>
        [Fact]
        public async Task EntityApi_Structure_ShouldRespectManagerScope()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.GetAsync(StructurePath);

            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var entities = json.RootElement.GetProperty("entities").EnumerateArray().ToList();

            Assert.DoesNotContain(entities, x => x.GetProperty("tableName").GetString() == "hidden_scoped_entities");
            Assert.DoesNotContain(entities, x =>
                x.GetProperty("entityTypeName").GetString() == typeof(Hidden.OrmHiddenScopedEntity).FullName);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Save_ShouldExecuteOrmSaveWithMockAuthorization.
        /// </summary>
        [Fact]
        public async Task EntityApi_Save_ShouldExecuteOrmSaveWithMockAuthorization()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, CreateSaveRequest());

            response.EnsureSuccessStatusCode();
            Assert.Contains("INSERT", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("departments", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Save_ShouldExecuteOrmUpdateWhenPrimaryKeyProvided.
        /// </summary>
        [Fact]
        public async Task EntityApi_Save_ShouldExecuteOrmUpdateWhenPrimaryKeyProvided()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, CreateUpdateRequest());

            response.EnsureSuccessStatusCode();
            Assert.Contains("UPDATE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("departments", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Проверяет, что строковый GUID из JSON-запроса сохраняется как типизированный параметр обновления.
        /// </summary>
        [Fact]
        public async Task EntityApi_SaveWithStringGuidPrimaryKey_ShouldUseGuidParameter()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);
            var recordId = Guid.Parse("7e8d71b9-2591-d1b9-c51d-aa205eca08ad");

            var response = await client.PostAsJsonAsync(ApiPath, new EntityApiRequest
            {
                Operation = EntityApiOperationType.Save,
                TableName = "departments",
                Values = new Dictionary<string, object?>
                {
                    ["Id"] = recordId.ToString(),
                    ["Name"] = "Updated from API"
                }
            });

            response.EnsureSuccessStatusCode();

            Assert.Contains("UPDATE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(
                EntityApiMockDbProvider.LastParameters,
                parameter => parameter.Value is Guid guidValue && guidValue == recordId);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Save_ShouldExecuteOrmInsertWhenPrimaryKeyMissing.
        /// </summary>
        [Fact]
        public async Task EntityApi_Save_ShouldExecuteOrmInsertWhenPrimaryKeyMissing()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, CreateSaveRequest());

            response.EnsureSuccessStatusCode();
            Assert.Contains("INSERT", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("departments", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Delete_ShouldExecuteOrmDeleteWithMockAuthorization.
        /// </summary>
        [Fact]
        public async Task EntityApi_Delete_ShouldExecuteOrmDeleteWithMockAuthorization()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, CreateDeleteRequest());

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.GetProperty("deleted").GetBoolean());
            Assert.Contains("DELETE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("departments", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_DeleteWithNonPrimaryFilter_ShouldExecuteOrmDelete.
        /// </summary>
        [Fact]
        public async Task EntityApi_DeleteWithNonPrimaryFilter_ShouldExecuteOrmDelete()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, new EntityApiRequest
            {
                Operation = EntityApiOperationType.Delete,
                TableName = "departments",
                Values = new Dictionary<string, object?>
                {
                    ["Name"] = "Unsafe delete"
                }
            });

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            Assert.True(document.RootElement.GetProperty("deleted").GetBoolean());
            Assert.Contains("DELETE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("Name", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_DeleteWithoutFilter_ShouldReturnBadRequest.
        /// </summary>
        [Fact]
        public async Task EntityApi_DeleteWithoutFilter_ShouldReturnBadRequest()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, new EntityApiRequest
            {
                Operation = EntityApiOperationType.Delete,
                TableName = "departments"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("at least one", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_DeleteWithQueryFilter_ShouldExecuteOrmDelete.
        /// </summary>
        [Fact]
        public async Task EntityApi_DeleteWithQueryFilter_ShouldExecuteOrmDelete()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, new EntityApiRequest
            {
                Operation = EntityApiOperationType.Delete,
                Query = new ESQJsonModel
                {
                    TableName = "employees",
                    Filters = new ESQFilterCollectionJsonModel
                    {
                        Items =
                        [
                            new ESQFilterJsonModel
                            {
                                Path = "DepartmentId.Name",
                                ComparisonType = EntityComparisonType.Equal,
                                Value = "Old Department"
                            }
                        ]
                    }
                }
            });

            response.EnsureSuccessStatusCode();

            Assert.Contains("DELETE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("LEFT JOIN", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("departments", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_SaveWithEmptyPrimaryKey_ShouldReturnBadRequest.
        /// </summary>
        [Fact]
        public async Task EntityApi_SaveWithEmptyPrimaryKey_ShouldReturnBadRequest()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(ApiPath, new EntityApiRequest
            {
                Operation = EntityApiOperationType.Save,
                TableName = "departments",
                Values = new Dictionary<string, object?>
                {
                    ["Id"] = 0,
                    ["Name"] = "Unsafe update"
                }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("primary key filter", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE", EntityApiMockDbProvider.LastSql, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Batch_ShouldUseConfiguredDefaultExecutionMode.
        /// </summary>
        [Fact]
        public async Task EntityApi_Batch_ShouldUseConfiguredDefaultExecutionMode()
        {
            await using var app = await CreateAppAsync(
                autoRegisterApiEndpoint: true,
                defaultBatchExecutionMode: EntityApiBatchExecutionMode.Sequential);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync($"{ApiPath}/batch", CreateBatchRequest());

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EntityApiBatchResponse>();

            Assert.NotNull(result);
            Assert.Equal(EntityApiBatchExecutionMode.Sequential, result.ExecutionMode);
            Assert.Equal(3, result.Results.Count);
            Assert.All(result.Results, item => Assert.True(item.Success));
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("INSERT", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(EntityApiMockDbProvider.SqlHistory, sql => sql.Contains("DELETE", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Batch_ShouldReturnRequestNames.
        /// </summary>
        [Fact]
        public async Task EntityApi_Batch_ShouldReturnRequestNames()
        {
            await using var app = await CreateAppAsync(autoRegisterApiEndpoint: true);
            var client = CreateAuthorizedClient(app);
            var request = CreateBatchRequest();
            request.Requests[0].Name = "loadEmployees";
            request.Requests[1].Name = null;

            var response = await client.PostAsJsonAsync($"{ApiPath}/batch", request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EntityApiBatchResponse>();

            Assert.NotNull(result);
            Assert.Equal("loadEmployees", result.Results[0].Name);
            Assert.False(string.IsNullOrWhiteSpace(result.Results[1].Name));
            Assert.NotEqual("loadEmployees", result.Results[1].Name);
        }

        /// <summary>
        /// Инициализирует новый экземпляр EntityApi_Batch_ShouldUseRequestExecutionModeOverride.
        /// </summary>
        [Fact]
        public async Task EntityApi_Batch_ShouldUseRequestExecutionModeOverride()
        {
            await using var app = await CreateAppAsync(
                autoRegisterApiEndpoint: true,
                defaultBatchExecutionMode: EntityApiBatchExecutionMode.Sequential);
            var client = CreateAuthorizedClient(app);

            var response = await client.PostAsJsonAsync(
                $"{ApiPath}/batch",
                CreateBatchRequest(EntityApiBatchExecutionMode.Parallel));

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EntityApiBatchResponse>();

            Assert.NotNull(result);
            Assert.Equal(EntityApiBatchExecutionMode.Parallel, result.ExecutionMode);
            Assert.Equal(3, result.Results.Count);
            Assert.All(result.Results, item => Assert.True(item.Success));
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateSelectRequest.
        /// </summary>
        private static ESQJsonModel CreateSelectRequest()
        {
            return new ESQJsonModel
            {
                TableName = "employees",
                RowCount = 100,
                Columns =
                [
                    new ESQColumnJsonModel
                    {
                        Path = "Name"
                    }
                ]
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateSelectOperationRequest.
        /// </summary>
        private static EntityApiRequest CreateSelectOperationRequest()
        {
            return new EntityApiRequest
            {
                Operation = EntityApiOperationType.Select,
                Query = CreateSelectRequest()
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateBatchRequest.
        /// </summary>
        private static EntityApiBatchRequest CreateBatchRequest(EntityApiBatchExecutionMode? executionMode = null)
        {
            return new EntityApiBatchRequest
            {
                ExecutionMode = executionMode,
                Requests =
                [
                    CreateSelectOperationRequest(),
                    CreateSaveRequest(),
                    CreateDeleteRequest()
                ]
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateSaveRequest.
        /// </summary>
        private static EntityApiRequest CreateSaveRequest()
        {
            return new EntityApiRequest
            {
                Operation = EntityApiOperationType.Save,
                TableName = "departments",
                Values = new Dictionary<string, object?>
                {
                    ["Name"] = "Api Department",
                    ["Description"] = "Created from API test"
                }
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateUpdateRequest.
        /// </summary>
        private static EntityApiRequest CreateUpdateRequest()
        {
            return new EntityApiRequest
            {
                Operation = EntityApiOperationType.Save,
                TableName = "departments",
                Values = new Dictionary<string, object?>
                {
                    ["Id"] = Guid.NewGuid(),
                    ["Name"] = "Updated API Department",
                    ["Description"] = "Updated from API test"
                }
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateDeleteRequest.
        /// </summary>
        private static EntityApiRequest CreateDeleteRequest()
        {
            return new EntityApiRequest
            {
                Operation = EntityApiOperationType.Delete,
                TableName = "departments",
                Values = new Dictionary<string, object?>
                {
                    ["Id"] = Guid.NewGuid()
                }
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateAuthorizedClient.
        /// </summary>
        private static HttpClient CreateAuthorizedClient(WebApplication app)
        {
            var client = app.GetTestClient();
            client.DefaultRequestHeaders.Add(AuthHeader, "allow");
            return client;
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateAppAsync.
        /// </summary>
        private static async Task<WebApplication> CreateAppAsync(
            bool autoRegisterApiEndpoint,
            EntityApiBatchExecutionMode defaultBatchExecutionMode = EntityApiBatchExecutionMode.Sequential)
        {
            EntityApiMockDbProvider.ResetState();
            DbManager.Reset();
            EntityManager.Reset();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.UseTestServer();

            builder.AddTitanicDb(config =>
            {
                config.DefaultProviderName = "EntityApiMock";
                config.Providers =
                [
                    new DbProviderConfig
                    {
                        Name = "EntityApiMock",
                        ConnectionString = "mock",
                        Types = new ProviderTypeConfig
                        {
                            ProviderType = typeof(EntityApiMockDbProvider).AssemblyQualifiedName!,
                            EngineType = typeof(PostgresEngine).AssemblyQualifiedName!
                        }
                    }
                ];
            });

            builder.AddTitanicEntityApi(config =>
            {
                config.Managers =
                [
                    new EntityManagerSettings
                    {
                        Name = "EntityApiMock",
                        DbProviderName = "EntityApiMock",
                        EntityModelNamespaces = ["Titanic.Test.Entity"],
                        Api = new EntityManagerApiSettings
                        {
                            AutoRegisterEndpoint = autoRegisterApiEndpoint,
                            Path = ApiPath,
                            AuthorizationHeaderName = AuthHeader,
                            AuthorizationProviderType = typeof(MockEntityApiAuthorizationProvider).AssemblyQualifiedName!,
                            DefaultBatchExecutionMode = defaultBatchExecutionMode
                        },
                        Options = new EntityManagerOptions
                        {
                            MaxReadRowCount = 3
                        }
                    }
                ];
            });

            var app = builder.Build();
            app.MapTitanicEntityApi();
            await app.StartAsync();
            return app;
        }

        #endregion Members
    }

    /// <summary>
    /// Mock-провайдер авторизации Entity API для HTTP-тестов.
    /// </summary>
    public sealed class MockEntityApiAuthorizationProvider : IEntityApiAuthorizationProvider
    {
        /// <inheritdoc />
        public ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager)
        {
            if (!context.Request.Headers.TryGetValue(manager.Api.AuthorizationHeaderName, out var value)
                || value.ToString() != "allow")
            {
                return ValueTask.FromResult(EntityApiAuthorizationResult.Fail("Mock authorization rejected request."));
            }

            return ValueTask.FromResult(EntityApiAuthorizationResult.Success(new UserConnection
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Culture = new UserCulture
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Test"
                }
            }));
        }
    }

    /// <summary>
    /// Mock DB provider для проверки Entity API без реального PostgreSQL.
    /// </summary>
    public sealed class EntityApiMockDbProvider : BaseDbProvider
    {
        /// <summary>
        /// Последний SQL, построенный ORM API.
        /// </summary>
        public static string LastSql { get; private set; } = string.Empty;

        /// <summary>
        /// Последние параметры, построенные ORM API.
        /// </summary>
        public static IReadOnlyList<QueryParameter> LastParameters { get; private set; } = [];

        /// <summary>
        /// История SQL-запросов, построенных ORM API.
        /// </summary>
        public static IReadOnlyList<string> SqlHistory
        {
            get
            {
                lock (_syncRoot)
                {
                    return _sqlHistory.ToArray();
                }
            }
        }

        private static readonly object _syncRoot = new();

        private static readonly List<string> _sqlHistory = [];

        /// <summary>
        /// Создать mock provider через reflection-фабрику DbManager.
        /// </summary>
        /// <param name="connectionString"> Строка подключения. </param>
            /// <param name="engine"> SQL-движок. </param>
        public EntityApiMockDbProvider(string connectionString, BaseDbEngine engine)
            : base(connectionString, engine)
        {
        }

        /// <summary>
        /// Сбросить состояние mock provider перед тестом.
        /// </summary>
        public static void ResetState()
        {
            lock (_syncRoot)
            {
                LastSql = string.Empty;
                LastParameters = [];
                _sqlHistory.Clear();
            }
        }

        /// <inheritdoc />
        public override int Execute(IQuery query)
        {
            Capture(query);
            return 1;
        }

        /// <inheritdoc />
        public override T ExecuteScalar<T>(IQuery query)
        {
            var build = Capture(query);
            object value = typeof(T) == typeof(int)
                           && build.Sql.Contains("COUNT", StringComparison.OrdinalIgnoreCase)
                ? 1
                : Guid.Parse("33333333-3333-3333-3333-333333333333");
            return (T)value;
        }

        /// <inheritdoc />
        public override List<T> ExecuteReader<T>(IQuery query, Func<DbDataReader, T> mapRow)
        {
            ArgumentNullException.ThrowIfNull(mapRow);
            var build = Capture(query);

            using var reader = CreateReader(build.Sql);
            var rows = new List<T>();
            while (reader.Read())
            {
                rows.Add(mapRow(reader));
            }

            return rows;
        }

        /// <inheritdoc />
        protected override DbConnection CreateConnection()
        {
            throw new NotSupportedException("EntityApiMockDbProvider does not create database connections.");
        }

        /// <inheritdoc />
        protected override DbParameter CreateParameter(QueryParameter parameter)
        {
            throw new NotSupportedException("EntityApiMockDbProvider does not create database parameters.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр Capture.
        /// </summary>
        private QueryBuildResult Capture(IQuery query)
        {
            var build = Build(query);
            lock (_syncRoot)
            {
                LastSql = build.Sql;
                LastParameters = build.Parameters.ToArray();
                _sqlHistory.Add(build.Sql);
            }

            return build;
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateReader.
        /// </summary>
        private static DbDataReader CreateReader(string sql)
        {
            var table = new DataTable();
            var values = new List<object>();

            if (sql.Contains(" AS \"Id\"", StringComparison.OrdinalIgnoreCase))
            {
                table.Columns.Add("Id", typeof(Guid));
                values.Add(Guid.Parse("44444444-4444-4444-4444-444444444444"));
            }

            if (sql.Contains(" AS \"Name\"", StringComparison.OrdinalIgnoreCase))
            {
                table.Columns.Add("Name", typeof(string));
                values.Add("Api Employee");
            }

            table.Rows.Add(values.ToArray());
            return table.CreateDataReader();
        }
    }
}

