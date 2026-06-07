# Titanic.Entity

## Роль в архитектуре

`Titanic.Entity` — ORM-слой поверх `Titanic.Db`. Он связывает SQL builder, metadata-модель сущностей и HTTP API для UI.

Слой занимает верхнюю позицию среди базовых пакетов решения:

```text
Titanic.Common -> Titanic.Db -> Titanic.Entity
```

Именно здесь находятся Entity-модели, структура колонок, ORM-пути, локализация, `EntityManager`, `EntitySchemaQuery` и автоматическая публикация HTTP endpoint-ов.

## Когда использовать

Используйте `Titanic.Entity`, если нужно:

- описывать таблицы через Entity-модели и атрибуты;
- строить запросы по ORM-путям вместо ручного SQL;
- работать с `DisplayValue` и локализуемыми колонками;
- сохранять и удалять сущности через единый ORM-слой;
- поднимать HTTP API для frontend или внешних клиентов.

Если нужен только SQL builder, без metadata и API, достаточно [Titanic.Db](../Titanic.Db/README.md).

## Основные части пакета

### Менеджеры

- `EntityManager` — статическая точка входа для регистрации менеджеров, получения менеджера по типу, создания `EntitySchemaQuery`, `EntitySelectBuilder` и `Entity`.
- `BaseEntityManager` — базовая обёртка над `BaseDbProvider`; хранит провайдер, настройки API, runtime-опции и проверку схемы БД.
- `EntityDbManager` — стандартная реализация `BaseEntityManager`.

### ORM-запросы

- `EntitySchemaQuery` — основная модель чтения сущностей.
- `EntitySchemaQuery<TEntity>` — generic-обёртка.
- `ESQ` и `ESQ<TEntity>` — alias-тип для совместимости.
- `EntitySelectBuilder` — ORM SELECT builder по путям колонок.
- `EntityQueryColumnCollection`, `EntityQueryColumn` — описание выбираемых колонок.
- `EntityQueryFilterCollection`, `EntityQueryFilter` — описание фильтров.
- `EntityWhereItem` — fluent-условия для `EntitySelectBuilder`.

### Сущности и значения колонок

- `Entity` — ORM-сущность, представляющая запись БД.
- `ColumnValue` — базовый класс значения колонки с `Value` и `DisplayValue`.
- `ScalarColumnValue` — значение обычной скалярной колонки.
- `StringColumnValue` — значение строковой колонки.
- `ReferenceColumnValue` — значение ссылочной колонки с `DisplayValue` связанной записи.

### Metadata и атрибуты

- `Structure` — сканирует сборки и хранит metadata Entity-моделей.
- `EntityStructure` — описание таблицы.
- `ColumnStructure` — описание колонки.
- `EntitySchemaValidator` — проверка схемы БД при инициализации менеджера.
- `EntityAttribute`, `PrimaryColumnAttribute`, `DisplayColumnAttribute`, `ColumnAttribute`, `StringColumnAttribute`, `ReferenceColumnAttribute`, `DisableLocalizationAttribute` — атрибуты описания сущностей.

### HTTP API

- `ServiceCollectionExtensions` — регистрация Entity ORM сервисов в DI.
- `WebApplicationExtensions` — регистрация и публикация endpoint-ов.
- `EntityManagerConfig`, `EntityManagerSettings`, `EntityManagerApiSettings`, `EntityManagerOptions` — конфигурация менеджеров и API.
- `EntityApiRequest`, `EntityApiBatchRequest` — модели HTTP-запросов.
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

## Пример чтения через EntitySchemaQuery

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
- `EntityModelNamespaces` — список namespace-patterns, по которым менеджер собирает свою структуру сущностей.
- `Api.Path` — базовый route для HTTP API конкретного менеджера.
- `Api.AuthorizationProviderType` — тип пользовательского провайдера из `Titanic.Common`, который по токену возвращает `UserConnection`.
- `Options.MaxReadRowCount` — максимальное количество строк для одного запроса чтения.

## Что важно знать про слой

- имя таблицы в `[Entity("...")]` задаётся без схемы;
- локализуемые колонки читаются через таблицу `sys_[table_name]_lcz`;
- менеджер получает свою структуру сущностей через `EntityModelNamespaces`;
- `UserConnection` обязателен для чтения, сохранения, удаления и построения локализации;
- HTTP API — это оболочка над Entity ORM, а не отдельная прикладная бизнес-логика.

## Куда идти дальше

- Если нужно понять общий поток данных и место слоя в решении, откройте [../ARCHITECTURE.md](../ARCHITECTURE.md).
- Если нужен JSON-контракт API для frontend, откройте [ENTITY_API.md](ENTITY_API.md).
- Если нужно разобраться в SQL builder, вернитесь к [../Titanic.Db/README.md](../Titanic.Db/README.md).

## Связанные документы

- [../ARCHITECTURE.md](../ARCHITECTURE.md)
- [../Titanic.Common/README.md](../Titanic.Common/README.md)
- [../Titanic.Db/README.md](../Titanic.Db/README.md)
- [ENTITY_API.md](ENTITY_API.md)


