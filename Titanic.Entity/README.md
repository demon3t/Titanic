# Titanic.Entity

## Роль

`Titanic.Entity` — ORM-слой поверх `Titanic.Db`. Он связывает Entity-модели, metadata, ORM-пути, локализацию, runtime-менеджеры и HTTP API.

Порядок слоёв:

```text
Titanic.Common -> Titanic.Db -> Titanic.Entity
```

Если нужен только SQL builder без metadata и HTTP API, используйте [Titanic.Db](../Titanic.Db/README.md).

## Когда использовать

Используйте `Titanic.Entity`, если нужно:

- описывать таблицы через Entity-модели и атрибуты;
- строить запросы по ORM-путям вместо ручного SQL;
- работать с `DisplayValue` и локализуемыми колонками;
- сохранять и удалять сущности через единый ORM-слой;
- публиковать HTTP API для frontend или внешних клиентов.

## Основные части

### Менеджеры

- `EntityManager` — основная точка регистрации и получения менеджеров.
- `BaseEntityManager` — обёртка над `BaseDbProvider`, содержащая провайдер, настройки API и runtime-опции.
- `EntityDbManager` — стандартная реализация менеджера.

### ORM-запросы

- `EntitySchemaQuery` — основная модель чтения сущностей.
- `EntitySchemaQuery<TEntity>` — generic-обёртка.
- `ESQ` и `ESQ<TEntity>` — alias-типы совместимости.
- `EntitySelectBuilder` — построитель ORM SELECT по путям колонок.
- `EntityQueryColumnCollection` и `EntityQueryColumn` — описание выбираемых колонок.
- `EntityQueryFilterCollection` и `EntityQueryFilter` — описание фильтров.
- `EntityWhereItem` — fluent API условий.

### Сущности и metadata

- `Entity` — ORM-сущность, представляющая строку БД.
- `Structure` — сканирует сборки и хранит metadata Entity-моделей.
- `EntityStructure` — описание таблицы.
- `ColumnStructure` — описание колонки.
- `EntitySchemaValidator` — проверка схемы БД при инициализации менеджера.

### HTTP API

- `ServiceCollectionExtensions` — регистрация сервисов Entity ORM в DI.
- `WebApplicationExtensions` — публикация HTTP endpoint-ов.
- `EntityManagerConfig`, `EntityManagerSettings`, `EntityManagerApiSettings`, `EntityManagerOptions` — конфигурация менеджеров и API.
- `EntityApiRequest` и `EntityApiBatchRequest` — модели HTTP-запросов.
- `EntityApiOperationType` — операции `Select`, `Save`, `Delete`.
- `EntityApiBatchExecutionMode` — режимы `Sequential` и `Parallel`.

## Пример Entity-модели

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

## Пример запроса

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

## Пример регистрации API

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

## Пример конфигурации

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
          "StructureAuthorizationProviderType": "MyApp.Security.EntityStructureUserConnectionProvider, MyApp",
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

Ключевые настройки:

- `DbProviderName` — имя провайдера, зарегистрированного в `Titanic.Db`.
- `ManagerType` — тип пользовательского менеджера поверх `BaseEntityManager`.
- `EntityModelNamespaces` — namespace-patterns для manager-specific структуры.
- `Api.Path` — базовый route менеджерского HTTP API.
- `Api.AuthorizationProviderType` — тип пользовательского провайдера, который ищет `UserConnection` по токену для обычных endpoint-ов.
- `Api.StructureAuthorizationProviderType` — тип отдельного пользовательского провайдера для endpoint-а структуры.
- `Options.MaxReadRowCount` — максимальное количество строк в одном read-запросе.

## Авторизация Entity API

Поток запроса:

```text
Клиент
  -> X-Entity-Key
  -> Пользовательское backend-приложение
  -> IUserConnectionTokenProvider
  -> UserConnection
  -> Titanic.Entity
  -> Titanic.Db
```

Разделение ответственности:

- `Titanic.Common` задаёт только базовый контракт `UserConnection` и интерфейс `IUserConnectionTokenProvider`.
- `Titanic.Entity` читает токен из `Api.AuthorizationHeaderName`, вызывает пользовательский provider и работает только с возвращённым `UserConnection`.
- пользовательское приложение само решает, где искать токен, как валидировать пользователя и как заполнять расширенный пользовательский контекст;
- если приложению нужны дополнительные поля, оно наследует свой тип от `UserConnection`;
- если provider не вернул пользователя, Entity API отвечает `403 Forbidden`;
- endpoint структуры использует отдельный provider `Api.StructureAuthorizationProviderType`, поэтому решение о доступе к структуре полностью остаётся на стороне пользовательского приложения.

## Важные особенности

- имя таблицы в `[Entity("...")]` задаётся без схемы;
- локализуемые колонки читаются через `sys_[table_name]_lcz`;
- каждый менеджер строит свою собственную структуру через `EntityModelNamespaces`;
- `UserConnection` обязателен для чтения, сохранения, удаления и локализации;
- HTTP API — это thin layer над Entity ORM, а не отдельная бизнес-логика.

## Куда смотреть дальше

- обзор архитектуры: [../ARCHITECTURE.md](../ARCHITECTURE.md)
- JSON-контракт API: [ENTITY_API.md](ENTITY_API.md)
- SQL builder: [../Titanic.Db/README.md](../Titanic.Db/README.md)

## Связанные документы

- [../ARCHITECTURE.md](../ARCHITECTURE.md)
- [../Titanic.Common/README.md](../Titanic.Common/README.md)
- [../Titanic.Db/README.md](../Titanic.Db/README.md)
- [ENTITY_API.md](ENTITY_API.md)
