using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Authorization.Interfaces;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Entity.Exceptions;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Orm;
using Titanic.Entity.Strurture;
using Titanic.Entity.WebApplication.Api;
using Titanic.Entity.WebApplication.Configuration;
using Orm = Titanic.Entity.Orm;

namespace Titanic.Entity.WebApplication
{
    /// <summary>
    /// Расширения для инициализации Entity ORM и Entity API в web-приложении.
    /// </summary>
    public static class WebApplicationExtensions
    {
        #region Builder Extensions

        /// <summary>
        /// Инициализировать Entity ORM менеджеры из конфигурации приложения.
        /// </summary>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="configSectionName"> Имя секции конфигурации. По умолчанию TitanicEntity. </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
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
        /// Инициализировать Entity ORM менеджеры через делегат конфигурации.
        /// </summary>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="configure"> Делегат настройки конфигурации. </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
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
        /// Инициализировать Entity ORM и подготовить автоматические Entity API endpoint-ы.
        /// </summary>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="configSectionName"> Имя секции конфигурации. По умолчанию TitanicEntity. </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
        public static WebApplicationBuilder AddTitanicEntityApi(
            this WebApplicationBuilder builder,
            string configSectionName = "TitanicEntity")
        {
            return builder.AddTitanicEntity(configSectionName);
        }

        /// <summary>
        /// Инициализировать Entity ORM и подготовить автоматические Entity API endpoint-ы через делегат настройки.
        /// </summary>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="configure"> Делегат настройки конфигурации. </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
        public static WebApplicationBuilder AddTitanicEntityApi(
            this WebApplicationBuilder builder,
            Action<EntityManagerConfig> configure)
        {
            return builder.AddTitanicEntity(configure);
        }

        /// <summary>
        /// Инициализировать один Entity ORM менеджер по ключу в секции Titanic:EntityManagers.
        /// </summary>
        /// <typeparam name="TManager"> Тип менеджера. </typeparam>
        /// <param name="builder"> WebApplicationBuilder. </param>
        /// <param name="managerKey"> Ключ настроек менеджера. </param>
        /// <returns> WebApplicationBuilder для цепочки вызовов. </returns>
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
        /// Поднять Entity API endpoint-ы для менеджеров с включенным Api.AutoRegisterEndpoint.
        /// </summary>
        /// <param name="app"> WebApplication. </param>
        /// <returns> WebApplication для цепочки вызовов. </returns>
        public static Microsoft.AspNetCore.Builder.WebApplication MapTitanicEntityApi(this Microsoft.AspNetCore.Builder.WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            foreach (var manager in EntityManager.GetManagers().Where(x => x.Api.AutoRegisterEndpoint))
            {
                MapManagerEndpoints(app, manager);
            }

            return app;
        }

        #endregion App Extensions

        #region Registration Helpers

        /// <summary>
        /// Зарегистрировать базовые сервисы Entity API.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        private static void RegisterEntityApiServices(IServiceCollection services)
        {
            services.AddSingleton<EntityApiAuthorizationProviderFactory>();
        }

        /// <summary>
        /// Зарегистрировать все текущие менеджеры EntityManager в DI контейнере.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
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
        /// Зарегистрировать endpoint-ы конкретного Entity ORM менеджера.
        /// </summary>
        /// <param name="app"> WebApplication. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        private static void MapManagerEndpoints(Microsoft.AspNetCore.Builder.WebApplication app, BaseEntityManager manager)
        {
            var basePath = NormalizeApiPath(manager.Api.Path);

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

            app.MapGet($"{basePath}/structure", async Task<IResult> (HttpContext context) =>
            {
                var authorization = await AuthorizeStructureAsync(context, manager);
                if (!authorization.IsAuthorized || authorization.UserConnection == null)
                {
                    return Results.Json(
                        new EntityApiErrorResponse
                        {
                            Error = authorization.ErrorMessage ?? "Forbidden.",
                            StatusCode = StatusCodes.Status403Forbidden
                        },
                        statusCode: StatusCodes.Status403Forbidden);
                }

                return Results.Ok(ToStructureResponse(manager));
            });
        }

        #endregion Endpoint Mapping

        #region Operation Execution

        /// <summary>
        /// Выполнить одну операцию Entity API.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <returns> Результат операции. </returns>
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
        /// Выполнить batch-запрос последовательно.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="requests"> Список операций. </param>
        /// <returns> Результаты операций. </returns>
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
        /// Выполнить batch-запрос параллельно.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="requests"> Список операций. </param>
        /// <returns> Результаты операций. </returns>
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
        /// Заполнить отсутствующие имена batch-операций стабильными для текущего запроса Guid.
        /// </summary>
        /// <param name="requests"> Операции batch-запроса. </param>
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
        /// Выполнить Select операцию.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <returns> Результат операции. </returns>
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
        /// Выполнить Save операцию.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <returns> Результат операции. </returns>
        private static EntityApiOperationResult ExecuteSave(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            var structure = ResolveEntityStructure(manager, request);
            var values = NormalizeValues(request.Values);
            var primaryColumn = structure.GetPrimaryColumnStructure();

            if (HasPrimaryKey(values, primaryColumn, out var primaryValue)
                && IsEmptyPrimaryKeyValue(primaryValue))
            {
                return EntityApiOperationResult.Fail(
                    request.Operation,
                    StatusCodes.Status400BadRequest,
                    $"Save update operation requires non-empty primary key filter '{primaryColumn.PropertyName}'. " +
                    "Omit primary key to create a new record.",
                    request.Name);
            }

            var entity = manager.Create(structure.TableName, userConnection);
            entity.SetValues(values);
            entity.Save();

            return EntityApiOperationResult.Ok(request.Operation, ToResponse(entity), request.Name);
        }

        /// <summary>
        /// Выполнить Delete операцию.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <returns> Результат операции. </returns>
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
        /// Преобразовать результат операции в HTTP-ответ.
        /// </summary>
        /// <param name="result"> Результат операции. </param>
        /// <returns> HTTP-ответ. </returns>
        private static IResult ToHttpResult(EntityApiOperationResult result)
        {
            return result.Success
                ? Results.Ok(result.Result)
                : Results.Json(result, statusCode: result.StatusCode);
        }

        #endregion Operation Execution

        #region Authorization

        /// <summary>
        /// Выполнить авторизацию Entity API.
        /// </summary>
        /// <param name="context"> HTTP-контекст. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <returns> Результат авторизации. </returns>
        private static ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager)
        {
            if (!context.Request.Headers.TryGetValue(manager.Api.AuthorizationHeaderName, out var token)
                || string.IsNullOrWhiteSpace(token.ToString()))
            {
                return ValueTask.FromResult(EntityApiAuthorizationResult.Fail(
                    $"Header '{manager.Api.AuthorizationHeaderName}' is required."));
            }

            var factory = context.RequestServices.GetRequiredService<EntityApiAuthorizationProviderFactory>();
            var provider = factory.CreateProvider(context.RequestServices, manager, EntityApiAuthorizationProviderKind.Default);
            return ResolveAuthorizationAsync(provider, token.ToString(), context);
        }

        /// <summary>
        /// Выполнить авторизацию endpoint-а структуры менеджера.
        /// </summary>
        /// <param name="context"> HTTP-контекст. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <returns> Результат авторизации. </returns>
        private static ValueTask<EntityApiAuthorizationResult> AuthorizeStructureAsync(HttpContext context, BaseEntityManager manager)
        {
            if (!context.Request.Headers.TryGetValue(manager.Api.AuthorizationHeaderName, out var token)
                || string.IsNullOrWhiteSpace(token.ToString()))
            {
                return ValueTask.FromResult(EntityApiAuthorizationResult.Fail(
                    $"Header '{manager.Api.AuthorizationHeaderName}' is required."));
            }

            var factory = context.RequestServices.GetRequiredService<EntityApiAuthorizationProviderFactory>();
            var provider = factory.CreateProvider(context.RequestServices, manager, EntityApiAuthorizationProviderKind.Structure);
            return ResolveStructureAuthorizationAsync(provider, token.ToString(), context);
        }

        /// <summary>
        /// Получить контекст пользователя через общий provider-контракт.
        /// </summary>
        private static async ValueTask<EntityApiAuthorizationResult> ResolveAuthorizationAsync(
            IUserConnectionTokenProvider provider,
            string token,
            HttpContext context)
        {
            var userConnection = await provider.FindByTokenAsync(token, context);
            return userConnection == null
                ? EntityApiAuthorizationResult.Fail("Forbidden.")
                : EntityApiAuthorizationResult.Success(userConnection);
        }

        /// <summary>
        /// Получить контекст пользователя для endpoint-а структуры и проверить admin-доступ.
        /// </summary>
        private static async ValueTask<EntityApiAuthorizationResult> ResolveStructureAuthorizationAsync(
            IUserConnectionTokenProvider provider,
            string token,
            HttpContext context)
        {
            var userConnection = await provider.FindByTokenAsync(token, context);
            if (userConnection == null)
            {
                return EntityApiAuthorizationResult.Fail("Forbidden.");
            }

            return userConnection.Roles.Contains("Admin")
                ? EntityApiAuthorizationResult.Success(userConnection)
                : EntityApiAuthorizationResult.Fail("Administrator permissions are required.");
        }

        #endregion Authorization

        #region Request Handling

        /// <summary>
        /// Создать Entity из HTTP-модели Save.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель. </param>
        /// <returns> ORM-сущность. </returns>
        private static Orm.Entity CreateRequestEntity(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiSaveRequest request)
        {
            return manager.Create(ResolveTableName(manager, request.TableName, request.EntityTypeName), userConnection);
        }

        /// <summary>
        /// Создать Entity из HTTP-модели Delete.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель. </param>
        /// <returns> ORM-сущность. </returns>
        private static Orm.Entity CreateRequestEntity(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiDeleteRequest request)
        {
            return manager.Create(ResolveTableName(manager, request.TableName, request.EntityTypeName), userConnection);
        }

        /// <summary>
        /// Создать Entity из единой HTTP-модели операции.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <returns> ORM-сущность. </returns>
        private static Orm.Entity CreateRequestEntity(
            BaseEntityManager manager,
            UserConnection userConnection,
            EntityApiRequest request)
        {
            return manager.Create(ResolveTableName(manager, request.TableName, request.EntityTypeName), userConnection);
        }

        /// <summary>
        /// Получить имя таблицы по HTTP-модели.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <param name="entityTypeName"> Имя CLR-типа сущности. </param>
        /// <returns> Имя таблицы. </returns>
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
        /// Получить структуру сущности по HTTP-модели операции.
        /// </summary>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <returns> Структура сущности. </returns>
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
        /// Построить ESQ с фильтрами для безопасного удаления.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <param name="request"> HTTP-модель операции. </param>
        /// <param name="structure"> Структура удаляемой сущности. </param>
        /// <returns> ESQ, выбирающий primary key удаляемых строк. </returns>
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
        /// Скопировать дерево фильтров между коллекциями ESQ.
        /// </summary>
        /// <param name="source"> Источник фильтров. </param>
        /// <param name="target"> Целевая коллекция фильтров. </param>
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
        /// Проверить, содержит ли коллекция хотя бы один активный leaf-фильтр.
        /// </summary>
        /// <param name="collection"> Коллекция фильтров. </param>
        /// <returns> <c>true</c>, если есть активный фильтр. </returns>
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
        /// Проверить, передан ли primary key как write-фильтр.
        /// </summary>
        /// <param name="values"> Значения операции. </param>
        /// <param name="primaryColumn"> Первичная колонка сущности. </param>
        /// <param name="value"> Значение первичного ключа. </param>
        /// <returns> <c>true</c>, если значение первичного ключа передано. </returns>
        private static bool HasPrimaryKey(
            IReadOnlyDictionary<string, object?> values,
            ColumnStructure primaryColumn,
            out object? value)
        {
            return values.TryGetValue(primaryColumn.PropertyName, out value)
                || values.TryGetValue(primaryColumn.ColumnName, out value);
        }

        /// <summary>
        /// Проверить, является ли значение primary key пустым.
        /// </summary>
        /// <param name="value"> Значение primary key. </param>
        /// <returns> <c>true</c>, если значение не может использоваться как write-фильтр. </returns>
        private static bool IsEmptyPrimaryKeyValue(object? value)
            => IsEmptyFilterValue(value);

        /// <summary>
        /// Проверить, является ли значение фильтра пустым.
        /// </summary>
        /// <param name="value"> Значение фильтра. </param>
        /// <returns> <c>true</c>, если значение не может использоваться как write-фильтр. </returns>
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
        /// Преобразовать Entity в HTTP-модель.
        /// </summary>
        /// <param name="entity"> ORM-сущность. </param>
        /// <returns> HTTP-модель значений колонок. </returns>
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
        /// Нормализовать значения, прочитанные System.Text.Json как object.
        /// </summary>
        /// <param name="values"> Значения HTTP-модели. </param>
        /// <returns> Нормализованные значения. </returns>
        private static Dictionary<string, object?> NormalizeValues(IReadOnlyDictionary<string, object?> values)
        {
            return values.ToDictionary(
                x => x.Key,
                x => NormalizeJsonValue(x.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Нормализовать JSON-значение.
        /// </summary>
        /// <param name="value"> Исходное значение. </param>
        /// <returns> Нормализованное CLR-значение. </returns>
        private static object? NormalizeJsonValue(object? value)
        {
            return value is System.Text.Json.JsonElement element
                ? NormalizeJsonElement(element)
                : value;
        }

        /// <summary>
        /// Нормализовать JsonElement.
        /// </summary>
        /// <param name="element"> JSON-элемент. </param>
        /// <returns> Нормализованное CLR-значение. </returns>
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
        /// Нормализовать базовый путь API.
        /// </summary>
        /// <param name="path"> Путь из конфигурации. </param>
        /// <returns> Нормализованный путь. </returns>
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

        /// <summary>
        /// Преобразовать manager-specific scope в HTTP-модель структуры.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <returns> Структура менеджера для HTTP API. </returns>
        private static EntityManagerStructureResponse ToStructureResponse(BaseEntityManager manager)
        {
            return new EntityManagerStructureResponse
            {
                ManagerName = manager.Name,
                NamespacePatterns = manager.StructureScope.NamespacePatterns.ToList(),
                Entities = manager.StructureScope.EntitiesStructure
                    .OrderBy(entity => entity.TableName, StringComparer.OrdinalIgnoreCase)
                    .Select(entity => new EntityStructureResponse
                    {
                        EntityTypeName = entity.EntityType.FullName ?? entity.EntityType.Name,
                        EntityTypeShortName = entity.EntityType.Name,
                        TableName = entity.TableName,
                        IsView = entity.IsView,
                        IsLocalizationDisabled = entity.IsLocalizationDisabled,
                        Columns = entity.ColumnsStructure
                            .OrderBy(column => column.PropertyName, StringComparer.OrdinalIgnoreCase)
                            .Select(column => new EntityColumnStructureResponse
                            {
                                PropertyName = column.PropertyName,
                                ColumnName = column.ColumnName,
                                DataValueType = column.DataValueType,
                                IsNullable = column.IsNullable,
                                IsPrimary = column.IsPrimary,
                                IsDisplay = column.IsDisplay,
                                IsLocalized = column.IsLocalized,
                                IsReference = column.IsReference,
                                ReferenceTableName = column.ReferenceTableName
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        #endregion Path Helpers
    }
}





