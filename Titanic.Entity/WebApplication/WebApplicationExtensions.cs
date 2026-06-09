using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Entity.Exceptions;
using Titanic.Entity.Events;
using Titanic.Entity.Events.Grpc;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Orm;
using Titanic.Entity.Strurture;
using Titanic.Entity.WebApplication.Api;
using Titanic.Entity.WebApplication.Configuration;
using Orm = Titanic.Entity.Orm;

namespace Titanic.Entity.WebApplication
{
    /// <summary>
    /// Методы расширения для настройки Entity ORM и Entity API в web-приложениях.
    /// </summary>
    public static class WebApplicationExtensions
    {
        #region Builder Extensions

        /// <summary>
        /// Инициализирует новый экземпляр AddTitanicEntity.
        /// </summary>
        public static WebApplicationBuilder AddTitanicEntity(
            this WebApplicationBuilder builder,
            string configSectionName = "TitanicEntity")
        {
            ArgumentNullException.ThrowIfNull(builder);

            var section = builder.Configuration.GetSection(configSectionName);
            var config = section.Get<EntityManagerConfig>() ?? new EntityManagerConfig();

            builder.Services.Configure<EntityManagerConfig>(section);
            EntityManager.Initialize(config);
            RegisterEntityApiServices(builder.Services);
            RegisterManagersInServices(builder.Services);

            return builder;
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddTitanicEntity.
        /// </summary>
        public static WebApplicationBuilder AddTitanicEntity(
            this WebApplicationBuilder builder,
            Action<EntityManagerConfig> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            var config = new EntityManagerConfig();
            configure(config);

            builder.Services.Configure<EntityManagerConfig>(options =>
            {
                options.Managers = config.Managers;
            });

            EntityManager.Initialize(config);
            RegisterEntityApiServices(builder.Services);
            RegisterManagersInServices(builder.Services);

            return builder;
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddTitanicEntityApi.
        /// </summary>
        public static WebApplicationBuilder AddTitanicEntityApi(
            this WebApplicationBuilder builder,
            string configSectionName = "TitanicEntity")
        {
            return builder.AddTitanicEntity(configSectionName);
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddTitanicEntityApi.
        /// </summary>
        public static WebApplicationBuilder AddTitanicEntityApi(
            this WebApplicationBuilder builder,
            Action<EntityManagerConfig> configure)
        {
            return builder.AddTitanicEntity(configure);
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddTitanicEntityEventListenerApi.
        /// </summary>
        public static WebApplicationBuilder AddTitanicEntityEventListenerApi(
            this WebApplicationBuilder builder,
            string configSectionName = "TitanicEntity")
        {
            builder.AddTitanicEntity(configSectionName);
            builder.Services.AddGrpc();
            return builder;
        }

        /// <summary>
        /// Инициализирует новый экземпляр AddTitanicEntityEventListenerApi.
        /// </summary>
        public static WebApplicationBuilder AddTitanicEntityEventListenerApi(
            this WebApplicationBuilder builder,
            Action<EntityManagerConfig> configure)
        {
            builder.AddTitanicEntity(configure);
            builder.Services.AddGrpc();
            return builder;
        }

        /// <summary>
        /// Инициализирует менеджер Entity ORM из конфигурации.
        /// </summary>
        public static WebApplicationBuilder InitEntityManager<TManager>(this WebApplicationBuilder builder, string managerKey)
            where TManager : BaseEntityManager, new()
        {
            ArgumentNullException.ThrowIfNull(builder);

            var settings = builder.Configuration
                ?.GetSection("Titanic")
                ?.GetSection("EntityManagers")
                ?.GetSection(managerKey)
                ?.Get<EntityManagerSettings>();

            if (settings is null)
            {
                throw new InvalidOperationException($"EntityManager '{managerKey}' not found in configuration.");
            }

            var dbProviderName = string.IsNullOrWhiteSpace(settings.DbProviderName)
                ? managerKey
                : settings.DbProviderName;
            var managerName = string.IsNullOrWhiteSpace(settings.Name)
                ? managerKey
                : settings.Name;
            var manager = BaseEntityManager.BuildManager<TManager>(managerName, DbManager.GetProvider(dbProviderName), settings);

            EntityManager.RegisterManager(manager);
            RegisterEntityApiServices(builder.Services);
            builder.Services.AddSingleton(manager);
            builder.Services.AddSingleton<TManager>(manager);

            return builder;
        }

        #endregion Builder Extensions

        #region App Extensions

        /// <summary>
        /// Инициализирует новый экземпляр MapTitanicEntityApi.
        /// </summary>
        public static Microsoft.AspNetCore.Builder.WebApplication MapTitanicEntityApi(this Microsoft.AspNetCore.Builder.WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            EntityManager.ConfigureServices(app.Services);

            foreach (var manager in EntityManager.GetManagers().Where(x => x.Api.AutoRegisterEndpoint))
            {
                MapManagerEndpoints(app, manager);
            }

            return app;
        }

        /// <summary>
        /// Инициализирует новый экземпляр MapTitanicEntityEventListenerApi.
        /// </summary>
        public static Microsoft.AspNetCore.Builder.WebApplication MapTitanicEntityEventListenerApi(
            this Microsoft.AspNetCore.Builder.WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);
            EntityManager.ConfigureServices(app.Services);

            foreach (var manager in EntityManager.GetManagers().Where(x => x.EventListenerApi.Mode == EntityEventListenerApiMode.Http))
            {
                MapEventListenerEndpoints(app, manager);
            }

            if (EntityManager.GetManagers().Any(x => x.EventListenerApi.Mode == EntityEventListenerApiMode.Grpc))
            {
                app.MapGrpcService<EntityEventListenerGrpcService>();
            }

            return app;
        }

        #endregion App Extensions

        #region Registration Helpers

        /// <summary>
        /// Инициализирует новый экземпляр RegisterEntityApiServices.
        /// </summary>
        private static void RegisterEntityApiServices(IServiceCollection services)
        {
            services.AddSingleton<HeaderEntityApiAuthorizationProvider>();
            services.AddSingleton<IEntityEventHttpClientFactory, DefaultEntityEventHttpClientFactory>();
            services.AddSingleton<IEntityEventGrpcClientFactory, DefaultEntityEventGrpcClientFactory>();
        }

        /// <summary>
        /// Инициализирует новый экземпляр RegisterManagersInServices.
        /// </summary>
        private static void RegisterManagersInServices(IServiceCollection services)
        {
            foreach (var manager in EntityManager.GetManagers())
            {
                services.AddSingleton(manager.GetType(), manager);
                services.AddSingleton(typeof(BaseEntityManager), manager);
            }
        }

        #endregion Registration Helpers

        #region Endpoint Mapping

        /// <summary>
        /// Регистрирует HTTP endpoint-ы событийного listener API для менеджера.
        /// </summary>
        /// <param name="app">Web-приложение.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        private static void MapEventListenerEndpoints(
            Microsoft.AspNetCore.Builder.WebApplication app,
            BaseEntityManager manager)
        {
            var basePath = NormalizeApiPath(manager.EventListenerApi.Path);

            app.MapPost($"{basePath}/{EntityEventListenerApiDefaults.HttpCreateActionPath}", (EntityEventDispatchRequest request) =>
            {
                request.ManagerName = manager.Name;
                return ToEventListenerResult(EntityEventListenerRequestExecutor.Create(manager, request));
            });

            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Saving);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Saved);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Inserting);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Inserted);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Updating);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Updated);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Deleting);
            MapEventListenerStageEndpoint(app, basePath, manager, EntityEventStage.Deleted);

            app.MapPost($"{basePath}/{EntityEventListenerApiDefaults.HttpDeleteActionPath}", (EntityEventDispatchRequest request) =>
            {
                request.ManagerName = manager.Name;
                return ToEventListenerResult(EntityEventListenerRequestExecutor.Delete(manager, request));
            });

            app.MapPost(basePath, (EntityEventDispatchRequest request) =>
            {
                request.ManagerName = manager.Name;
                return ToEventListenerResult(EntityEventListenerRequestExecutor.Execute(manager, request));
            });
        }

        /// <summary>
        /// Регистрирует HTTP endpoint конкретной стадии событийного pipeline.
        /// </summary>
        /// <param name="app">Web-приложение.</param>
        /// <param name="basePath">Базовый путь listener API.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        private static void MapEventListenerStageEndpoint(
            Microsoft.AspNetCore.Builder.WebApplication app,
            string basePath,
            BaseEntityManager manager,
            EntityEventStage stage)
        {
            var actionPath = EntityEventListenerApiDefaults.GetHttpActionPath(stage);
            app.MapPost($"{basePath}/{actionPath}", (EntityEventDispatchRequest request) =>
            {
                request.ManagerName = manager.Name;
                return ToEventListenerResult(EntityEventListenerRequestExecutor.ExecuteStage(manager, request, stage));
            });
        }

        /// <summary>
        /// Преобразует transport-ответ listener API в HTTP-результат.
        /// </summary>
        /// <param name="result">Transport-ответ listener API.</param>
        /// <returns>HTTP-результат listener API.</returns>
        private static IResult ToEventListenerResult(EntityEventDispatchResponse result)
        {
            if (result.Success)
            {
                return Results.Ok(result);
            }

            if (result.Canceled)
            {
                return Results.Json(result, statusCode: StatusCodes.Status409Conflict);
            }

            return Results.Json(result, statusCode: StatusCodes.Status500InternalServerError);
        }

        /// <summary>
        /// Инициализирует новый экземпляр MapManagerEndpoints.
        /// </summary>
        private static void MapManagerEndpoints(Microsoft.AspNetCore.Builder.WebApplication app, BaseEntityManager manager)
        {
            var basePath = NormalizeApiPath(manager.Api.Path);

            app.MapGet($"{basePath}/structure", async Task<IResult> (HttpContext context) =>
            {
                var authorization = await AuthorizeAsync(context, manager);
                if (!authorization.IsAuthorized || authorization.UserConnection == null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return Results.Ok(BuildStructureResponse(manager));
            });

            app.MapPost(basePath, async Task<IResult> (HttpContext context, EntityApiRequest request) =>
            {
                var authorization = await AuthorizeAsync(context, manager);
                if (!authorization.IsAuthorized || authorization.UserConnection == null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return ToHttpResult(ExecuteRequest(manager, authorization.UserConnection, request));
            });

            app.MapPost($"{basePath}/batch", async Task<IResult> (HttpContext context, EntityApiBatchRequest request) =>
            {
                var authorization = await AuthorizeAsync(context, manager);
                if (!authorization.IsAuthorized || authorization.UserConnection == null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                EnsureBatchRequestNames(request.Requests);

                var executionMode = request.ExecutionMode ?? manager.Api.DefaultBatchExecutionMode;
                var results = executionMode == EntityApiBatchExecutionMode.Parallel
                    ? await ExecuteBatchParallel(manager, authorization.UserConnection, request.Requests)
                    : ExecuteBatchSequential(manager, authorization.UserConnection, request.Requests);

                return Results.Ok(new EntityApiBatchResponse
                {
                    ExecutionMode = executionMode,
                    Results = results
                });
            });
        }

        #endregion Endpoint Mapping

        #region Operation Execution

        /// <summary>
        /// Инициализирует новый экземпляр ExecuteRequest.
        /// </summary>
        private static EntityApiOperationResult ExecuteRequest(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                return request.Operation switch
                {
                    EntityApiOperationType.Select => ExecuteSelect(manager, userConnection, request),
                    EntityApiOperationType.Save => ExecuteSave(manager, userConnection, request),
                    EntityApiOperationType.Delete => ExecuteDelete(manager, userConnection, request),
                    _ => EntityApiOperationResult.Fail(
                        request.Operation,
                        StatusCodes.Status400BadRequest,
                        $"Entity API operation '{request.Operation}' is not supported.",
                        request.Name)
                };
            }
            catch (InvalidOperationException ex)
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    ex.Message,
                    request.Name);
            }
            catch (ArgumentException ex)
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    ex.Message,
                    request.Name);
            }
            catch (NotExistColumnException ex)
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    ex.Message,
                    request.Name);
            }
            catch (NotExistTableException ex)
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    ex.Message,
                    request.Name);
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр ExecuteBatchSequential.
        /// </summary>
        private static List<EntityApiOperationResult> ExecuteBatchSequential(
            BaseEntityManager manager,
            UserConnection userConnection,
            IReadOnlyCollection<EntityApiRequest> requests)
        {
            return requests
                .Select(request => ExecuteRequest(manager, userConnection, request))
                .ToList();
        }

        /// <summary>
        /// Инициализирует новый экземпляр ExecuteBatchParallel.
        /// </summary>
        private static async Task<List<EntityApiOperationResult>> ExecuteBatchParallel(
            BaseEntityManager manager,
            UserConnection userConnection,
            IReadOnlyCollection<EntityApiRequest> requests)
        {
            var tasks = requests
                .Select(request => Task.Run(() => ExecuteRequest(manager, userConnection, request)))
                .ToArray();

            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureBatchRequestNames.
        /// </summary>
        private static void EnsureBatchRequestNames(IEnumerable<EntityApiRequest> requests)
        {
            foreach (var request in requests)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    request.Name = Guid.NewGuid().ToString("N");
                }
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр ExecuteSelect.
        /// </summary>
        private static EntityApiOperationResult ExecuteSelect(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            if (request.Query == null)
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    "Select operation requires Query.",
                    request.Name);
            }

            var esq = request.Query.ToESQ(manager, userConnection);
            esq.MaxReadRowCount = manager.Options.MaxReadRowCount;

            var rows = esq
                .GetEntityCollection()
                .Select(ToResponse)
                .ToList();

            return EntityApiOperationResult.Ok(request.Operation, rows, request.Name);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ExecuteSave.
        /// </summary>
        private static EntityApiOperationResult ExecuteSave(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            var structure = ResolveEntityStructure(manager, request);
            var values = NormalizeValues(request.Values);
            var primaryColumn = structure.GetPrimaryColumnStructure();
            var hasPrimaryKey = HasPrimaryKey(values, primaryColumn, out var primaryValue);

            if (hasPrimaryKey && IsEmptyPrimaryKeyValue(primaryValue))
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    $"Save update operation requires non-empty primary key filter '{primaryColumn.PropertyName}'. " +
                    "Omit primary key to create a new record.",
                    request.Name);
            }

            var entity = EntityManager.Create(
                structure,
                manager.Provider,
                userConnection,
                isNew: !hasPrimaryKey,
                manager: manager);
            entity.SetValues(values);
            entity.Save();

            return EntityApiOperationResult.Ok(request.Operation, ToResponse(entity), request.Name);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ExecuteDelete.
        /// </summary>
        private static EntityApiOperationResult ExecuteDelete(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            var structure = ResolveEntityStructure(manager, request);
            var deleteQuery = BuildDeleteFilterQuery(manager, userConnection, request, structure);
            if (!HasActiveFilters(deleteQuery.Filters))
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    "Delete operation requires at least one non-empty filter.",
                    request.Name);
            }

            var entities = deleteQuery.GetEntityCollection();
            var affected = 0;

            Parallel.ForEach(entities, entity =>
            {
                if (entity.Delete())
                {
                    Interlocked.Increment(ref affected);
                }
            });

            return EntityApiOperationResult.Ok(request.Operation, new { deleted = affected > 0, affected }, request.Name);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ToHttpResult.
        /// </summary>
        private static IResult ToHttpResult(EntityApiOperationResult result)
        {
            return result.Success
                ? Results.Ok(result.Result)
                : Results.Json(
                    new
                    {
                        error = result.ErrorMessage,
                        operation = result.Operation
                    },
                    statusCode: result.StatusCode);
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildStructureResponse.
        /// </summary>
        private static EntityApiManagerStructureResponse BuildStructureResponse(BaseEntityManager manager)
        {
            return new EntityApiManagerStructureResponse
            {
                Entities = manager.StructureScope.EntitiesStructure
                    .Select(entity => new EntityApiStructureEntityResponse
                    {
                        TableName = entity.TableName,
                        EntityTypeName = entity.EntityType.FullName ?? entity.EntityType.Name,
                        Columns = entity.ColumnsStructure
                            .Select(column => new EntityApiStructureColumnResponse
                            {
                                PropertyName = column.PropertyName,
                                ColumnName = column.ColumnName,
                                DataValueType = column.DataValueType,
                                IsNullable = column.IsNullable,
                                IsPrimary = column.IsPrimary,
                                IsDisplay = column.IsDisplay,
                                IsReference = column.IsReference,
                                ReferenceTableName = column.ReferenceTableName
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        #endregion Operation Execution

        #region Authorization

        /// <summary>
        /// Инициализирует новый экземпляр AuthorizeAsync.
        /// </summary>
        private static ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager)
        {
            var provider = CreateAuthorizationProvider(context.RequestServices, manager);
            return provider.AuthorizeAsync(context, manager);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateAuthorizationProvider.
        /// </summary>
        private static IEntityApiAuthorizationProvider CreateAuthorizationProvider(
            IServiceProvider services,
            BaseEntityManager manager)
        {
            if (string.IsNullOrWhiteSpace(manager.Api.AuthorizationProviderType))
            {
                return services.GetRequiredService<HeaderEntityApiAuthorizationProvider>();
            }

            var providerType = Type.GetType(manager.Api.AuthorizationProviderType)
                ?? throw new InvalidOperationException(
                    $"Entity API authorization provider '{manager.Api.AuthorizationProviderType}' not found.");
            if (!typeof(IEntityApiAuthorizationProvider).IsAssignableFrom(providerType))
            {
                throw new InvalidOperationException(
                    $"Entity API authorization provider '{manager.Api.AuthorizationProviderType}' must implement IEntityApiAuthorizationProvider.");
            }

            return (IEntityApiAuthorizationProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }

        #endregion Authorization

        #region Request Handling

        /// <summary>
        /// Инициализирует новый экземпляр CreateRequestEntity.
        /// </summary>
        private static Orm.Entity CreateRequestEntity(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiSaveRequest request)
        {
            return manager.Create(ResolveTableName(manager, request.TableName, request.EntityTypeName), userConnection);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateRequestEntity.
        /// </summary>
        private static Orm.Entity CreateRequestEntity(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiDeleteRequest request)
        {
            return manager.Create(ResolveTableName(manager, request.TableName, request.EntityTypeName), userConnection);
        }

        /// <summary>
        /// Инициализирует новый экземпляр CreateRequestEntity.
        /// </summary>
        private static Orm.Entity CreateRequestEntity(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            return manager.Create(ResolveTableName(manager, request.TableName, request.EntityTypeName), userConnection);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResolveTableName.
        /// </summary>
        private static string ResolveTableName(
            BaseEntityManager manager,
            string? tableName,
            string? entityTypeName)
        {
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }

            if (!string.IsNullOrWhiteSpace(entityTypeName))
            {
                return manager.StructureScope.GetEntityStructureByTypeName(entityTypeName).TableName;
            }

            throw new InvalidOperationException("Entity API request must contain TableName or EntityTypeName.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResolveEntityStructure.
        /// </summary>
        private static EntityStructure ResolveEntityStructure(BaseEntityManager manager, EntityApiRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.TableName))
            {
                return manager.StructureScope.GetEntityStructure(request.TableName);
            }

            if (!string.IsNullOrWhiteSpace(request.EntityTypeName))
            {
                return manager.StructureScope.GetEntityStructureByTypeName(request.EntityTypeName);
            }

            if (request.Query != null)
            {
                if (!string.IsNullOrWhiteSpace(request.Query.TableName))
                {
                    return manager.StructureScope.GetEntityStructure(request.Query.TableName);
                }

                if (!string.IsNullOrWhiteSpace(request.Query.EntityTypeName))
                {
                    return manager.StructureScope.GetEntityStructureByTypeName(request.Query.EntityTypeName);
                }
            }

            throw new InvalidOperationException("Entity API request must contain TableName or EntityTypeName.");
        }

        /// <summary>
        /// Инициализирует новый экземпляр BuildDeleteFilterQuery.
        /// </summary>
        private static EntitySchemaQuery BuildDeleteFilterQuery(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request,
            EntityStructure structure)
        {
            var query = new EntitySchemaQuery(manager.Provider, structure, manager.StructureScope, userConnection);
            query.AddPrimaryColumn();

            if (request.Query?.Filters != null)
            {
                CopyFilterNodes(request.Query.Filters.ToEntityFilterCollection(), query.Filters);
            }

            foreach (var value in NormalizeValues(request.Values).Where(x => !IsEmptyFilterValue(x.Value)))
            {
                query.AddFilter(EntityComparisonType.Equal, value.Key, value.Value);
            }

            return query;
        }

        /// <summary>
        /// Инициализирует новый экземпляр CopyFilterNodes.
        /// </summary>
        private static void CopyFilterNodes(EntityQueryFilterCollection source, EntityQueryFilterCollection target)
        {
            target.IsEnabled = source.IsEnabled;
            target.LogicalOperation = source.LogicalOperation;

            foreach (var node in source.Nodes)
            {
                switch (node)
                {
                    case EntityQueryFilter filter:
                        target.Add(filter);
                        break;
                    case EntityQueryFilterCollection group:
                        target.Add(group);
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр HasActiveFilters.
        /// </summary>
        private static bool HasActiveFilters(EntityQueryFilterCollection collection)
        {
            if (!collection.IsEnabled)
            {
                return false;
            }

            foreach (var node in collection.Nodes.Where(x => x.IsEnabled))
            {
                switch (node)
                {
                    case EntityQueryFilter:
                        return true;
                    case EntityQueryFilterCollection group when HasActiveFilters(group):
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Инициализирует новый экземпляр HasPrimaryKey.
        /// </summary>
        private static bool HasPrimaryKey(
            IReadOnlyDictionary<string, object?> values,
            ColumnStructure primaryColumn,
            out object? value)
        {
            return values.TryGetValue(primaryColumn.PropertyName, out value)
                || values.TryGetValue(primaryColumn.ColumnName, out value);
        }

        /// <summary>
        /// Инициализирует новый экземпляр IsEmptyPrimaryKeyValue.
        /// </summary>
        private static bool IsEmptyPrimaryKeyValue(object? value)
            => IsEmptyFilterValue(value);

        /// <summary>
        /// Инициализирует новый экземпляр IsEmptyFilterValue.
        /// </summary>
        private static bool IsEmptyFilterValue(object? value)
        {
            if (value == null || value is DBNull)
            {
                return true;
            }

            return value switch
            {
                int intValue => intValue == default,
                long longValue => longValue == default,
                Guid guidValue => guidValue == default,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                _ => false
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр ToResponse.
        /// </summary>
        private static Dictionary<string, EntityApiColumnValueResponse> ToResponse(Orm.Entity entity)
        {
            return entity.Values.ToDictionary(
                x => x.Key,
                x => new EntityApiColumnValueResponse
                {
                    Value = x.Value.Value,
                    DisplayValue = x.Value.DisplayValue
                },
                StringComparer.OrdinalIgnoreCase);
        }

        #endregion Request Handling

        #region Value Normalization

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeValues.
        /// </summary>
        private static Dictionary<string, object?> NormalizeValues(IReadOnlyDictionary<string, object?> values)
        {
            return values.ToDictionary(
                x => x.Key,
                x => NormalizeJsonValue(x.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeJsonValue.
        /// </summary>
        private static object? NormalizeJsonValue(object? value)
        {
            return value is System.Text.Json.JsonElement element
                ? NormalizeJsonElement(element)
                : value;
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeJsonElement.
        /// </summary>
        private static object? NormalizeJsonElement(System.Text.Json.JsonElement element)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Null => null,
                System.Text.Json.JsonValueKind.Undefined => null,
                System.Text.Json.JsonValueKind.String => element.GetString(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
                System.Text.Json.JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                System.Text.Json.JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
                System.Text.Json.JsonValueKind.Number => element.GetDouble(),
                _ => throw new NotSupportedException($"JSON value kind '{element.ValueKind}' is not supported in Entity API.")
            };
        }

        #endregion Value Normalization

        #region Path Helpers

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeApiPath.
        /// </summary>
        private static string NormalizeApiPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/entity";
            }

            var normalized = path.Trim();
            normalized = normalized.StartsWith('/') ? normalized : $"/{normalized}";
            return normalized.EndsWith('/') ? normalized.TrimEnd('/') : normalized;
        }

        #endregion Path Helpers
    }
}
