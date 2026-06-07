# Titanic.Entity

## Role

`Titanic.Entity` is the ORM layer built on top of `Titanic.Db`. It combines entity metadata, ORM paths, localization support, runtime managers, and HTTP API publishing.

Layer order:

```text
Titanic.Common -> Titanic.Db -> Titanic.Entity
```

If you only need the SQL builder and do not need metadata or HTTP API, use [Titanic.Db](../Titanic.Db/README.md).

## When to use it

Use `Titanic.Entity` when you need to:

- describe tables with entity models and attributes;
- build queries through ORM paths instead of manual SQL;
- work with `DisplayValue` and localized columns;
- save and delete entities through one ORM layer;
- expose an HTTP API for frontend or external clients.

## Main parts

### Managers

- `EntityManager` is the main registration and creation entry point.
- `BaseEntityManager` wraps `BaseDbProvider` and stores provider, API settings, and runtime options.
- `EntityDbManager` is the default manager implementation.

### ORM queries

- `EntitySchemaQuery` is the main entity read model.
- `EntitySchemaQuery<TEntity>` is the generic wrapper.
- `ESQ` and `ESQ<TEntity>` are compatibility aliases.
- `EntitySelectBuilder` builds ORM SELECT queries from column paths.
- `EntityQueryColumnCollection` and `EntityQueryColumn` describe selected columns.
- `EntityQueryFilterCollection` and `EntityQueryFilter` describe filters.
- `EntityWhereItem` is the fluent condition API.

### Entities and metadata

- `Entity` represents a database row.
- `Structure` scans assemblies and stores entity metadata.
- `EntityStructure` describes a table.
- `ColumnStructure` describes a column.
- `EntitySchemaValidator` validates the database schema at manager initialization time.

### HTTP API

- `ServiceCollectionExtensions` registers Entity ORM services in DI.
- `WebApplicationExtensions` publishes HTTP endpoints.
- `EntityManagerConfig`, `EntityManagerSettings`, `EntityManagerApiSettings`, `EntityManagerOptions` configure managers and API.
- `EntityApiRequest` and `EntityApiBatchRequest` are HTTP request models.
- `EntityApiOperationType` defines `Select`, `Save`, and `Delete`.
- `EntityApiBatchExecutionMode` defines `Sequential` and `Parallel`.

## Example entity model

```csharp
using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

[Entity("departments")]
public sealed class DepartmentEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name", isLocalized: true)]
    public string Name { get; set; } = string.Empty;

    [StringColumn("description", isLocalized: true)]
    public string? Description { get; set; }
}
```

## Example query

```csharp
var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("Id")
    .AddColumn("Name")
    .AddColumn("DepartmentId.Name", "DepartmentName")
    .AddFilter(EntityComparisonType.Contains, "Email", "@company.com")
    .OrderBy("Name")
    .GetEntityCollection();
```

## Example API registration

```csharp
using Titanic.Db.WebApplication;
using Titanic.Entity.WebApplication;

var builder = WebApplication.CreateBuilder(args);

builder.AddTitanicDb("TitanicDb");
builder.AddTitanicEntityApi("TitanicEntity");

var app = builder.Build();
app.MapTitanicEntityApi();
app.Run();
```

## Example configuration

```json
{
  "TitanicEntity": {
    "Managers": [
      {
        "Name": "posgreTest",
        "DbProviderName": "posgreTest",
        "ManagerType": "MyApp.EntityManagers.PostgresEntityManager, MyApp",
        "EntityModelNamespaces": [
          "MyApp.EntityModels.*"
        ],
        "Api": {
          "AutoRegisterEndpoint": true,
          "Path": "/entity/posgreTest",
          "AuthorizationHeaderName": "X-Entity-Key",
          "AuthorizationProviderType": "MyApp.Security.EntityApiUserConnectionProvider, MyApp",
          "DefaultBatchExecutionMode": "Sequential"
        },
        "ValidateDatabaseSchemaOnCompile": true,
        "Options": {
          "MaxReadRowCount": 20000
        }
      }
    ]
  }
}
```

Key settings:

- `DbProviderName`: provider name registered in `Titanic.Db`.
- `ManagerType`: user manager type built on top of `BaseEntityManager`.
- `EntityModelNamespaces`: namespace patterns used to build the manager-specific entity structure.
- `Api.Path`: base route for the manager HTTP API.
- `Api.AuthorizationProviderType`: user-defined provider type from `Titanic.Common` that resolves `UserConnection` by token.
- `Options.MaxReadRowCount`: maximum row count allowed for one read request.

## Entity API authorization

Authorization flow:

```text
Client
  -> X-Entity-Key
  -> User application
  -> IUserConnectionTokenProvider (Titanic.Common contract)
  -> UserConnection
  -> Titanic.Entity
  -> Titanic.Db
```

Responsibility split:

- `Titanic.Common` defines `IUserConnectionTokenProvider` and the base `UserConnection` contract.
- `Titanic.Entity` reads the token from `Api.AuthorizationHeaderName`, calls the provider, and works only with the returned `UserConnection`.
- the user application implements the provider, decides where tokens are stored, how a user is resolved, and how the user context is populated.
- if the application needs extra user state, it inherits from `UserConnection` and returns its own type from the provider.
- if the provider does not return a user, Entity API responds with `403 Forbidden`.
- the manager structure endpoint requires the `Admin` role in `UserConnection.Roles`.

## Important behavior

- table names in `[Entity("...")]` are defined without schema;
- localized columns are read through `sys_[table_name]_lcz`;
- each manager builds its own entity structure through `EntityModelNamespaces`;
- `UserConnection` is required for read, save, delete, and localization;
- HTTP API is a thin shell over Entity ORM, not a separate business layer.

## Where to go next

- Architecture overview: [../ARCHITECTURE.md](../ARCHITECTURE.md)
- JSON API contract: [ENTITY_API.md](ENTITY_API.md)
- SQL builder: [../Titanic.Db/README.md](../Titanic.Db/README.md)

## Related documents

- [../ARCHITECTURE.md](../ARCHITECTURE.md)
- [../Titanic.Common/README.md](../Titanic.Common/README.md)
- [../Titanic.Db/README.md](../Titanic.Db/README.md)
- [ENTITY_API.md](ENTITY_API.md)
