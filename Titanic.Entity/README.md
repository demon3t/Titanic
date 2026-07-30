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
- `EntityAttribute`, `PrimaryColumnAttribute`, `DisplayColumnAttribute`, `ColumnAttribute`, `StringColumnAttribute`, `ReferenceColumnAttribute`, `DisableLocalizationAttribute` — атрибуты описания сущностей.

`EntityStructure`, `ColumnStructure`, `EntityStructureScope` и валидаторы схемы являются внутренними runtime-деталями. Внешний код должен получать доступную frontend-структуру через `GET {Api.Path}/structure` или работать с ORM через публичные builders.

### HTTP API

- `ServiceCollectionExtensions` — регистрация Entity ORM сервисов в DI.
- `WebApplicationExtensions` — регистрация и публикация endpoint-ов.
- `EntityManagerConfig`, `EntityManagerSettings`, `EntityManagerApiSettings`, `EntityManagerOptions` — конфигурация менеджеров и API.
- `EntityApiRequest`, `EntityApiBatchRequest` — модели HTTP-запросов.
- `EntityApiManagerStructureResponse` — модель ответа endpoint-а структуры менеджера.
- `EntityApiOperationType` — операции `Select`, `Save`, `Delete`.
- `EntityApiBatchExecutionMode` — режимы `Sequential` и `Parallel`.

## Граница публичного API

Публичным контрактом пакета считаются:

- `EntityManager`, `BaseEntityManager`, `EntityDbManager` и web/DI extension-методы;
- ORM builders: `EntitySchemaQuery`, `ESQ`, `EntitySelectBuilder`, `EntityWhereItem`;
- модели QueryModel и JSON-модели ESQ: колонки, фильтры, сортировки, логические операции, сравнения и агрегации;
- `Entity`, `ColumnValue` и наследники значений колонок;
- атрибуты Entity-моделей и event listener-ов;
- HTTP API-контракт: `EntityApiRequest`, `EntityApiBatchRequest`, response-модели, enum-ы операций и режимов, `IEntityApiAuthorizationProvider`;
- event API: базовые listener/provider-типы, transport-контракты и client factory-интерфейсы.

Legacy-модели отдельных операций `EntityApiSaveRequest` и `EntityApiDeleteRequest` оставлены только для совместимости и помечены `[Obsolete]`. Для HTTP-вызовов используйте единую модель `EntityApiRequest` с `operation = Save` или `operation = Delete`.

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

После `MapTitanicEntityApi()` для каждого менеджера с `Api.AutoRegisterEndpoint = true` публикуются endpoint-ы:

- `GET {Api.Path}/structure`
- `POST {Api.Path}`
- `POST {Api.Path}/batch`

## Событийный слой Entity

`Titanic.Entity` поддерживает событийный pipeline вокруг `Save()` и `Delete()`:

- `OnSaving`
- `OnSaved`
- `OnInserting`
- `OnInserted`
- `OnUpdating`
- `OnUpdated`
- `OnDeleting`
- `OnDeleted`

Событийный слой может работать в двух режимах:

- локально, в том же приложении, где поднят `EntityManager`;
- внешне, через отдельный listener-сервис.

Режим задаётся настройкой `EventListener` в конфигурации менеджера:

- пустое значение или отсутствие поля — локальный listener;
- заполненное значение — внешний listener.

Локальный режим означает, что обработчики ищутся по `[EntityEventListener("table_name")]` и вызываются внутри текущего процесса.

Внешний режим означает, что вызов событийного слоя должен идти через transport-слой. Для этого поддерживается единый логический контракт события:

- `managerName`
- `tableName`
- `dispatchId`
- `stage`
- `isNew`
- `userConnection`
- `values`

### HTTP-контракт внешнего listener-а

HTTP listener публикует отдельный endpoint на lifecycle-действие и каждую стадию pipeline:

```text
POST {EventListenerApi.Path}/create
POST {EventListenerApi.Path}/on-saving
POST {EventListenerApi.Path}/on-saved
POST {EventListenerApi.Path}/on-inserting
POST {EventListenerApi.Path}/on-inserted
POST {EventListenerApi.Path}/on-updating
POST {EventListenerApi.Path}/on-updated
POST {EventListenerApi.Path}/on-deleting
POST {EventListenerApi.Path}/on-deleted
POST {EventListenerApi.Path}/delete
```

`create` создаёт экземпляр remote listener-а на удалённой стороне, `delete` удаляет его после последней стадии. Один HTTP-вызов `on-*` соответствует одному этапу событийного pipeline.

Минимальный ответ:

```json
{
  "success": true,
  "canceled": false,
  "cancelReason": null,
  "errorCode": null,
  "errorMessage": null
}
```

Если listener отменяет операцию, внешний сервис должен вернуть `canceled = true`. Если обработчик падает, он должен вернуть `success = false` и текст ошибки.

### gRPC-контракт внешнего listener-а

Рекомендуемый service:

```text
EntityEventListenerGrpc.Create(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnSaving(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnSaved(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnInserting(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnInserted(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnUpdating(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnUpdated(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnDeleting(EntityEventGrpcRequest)
EntityEventListenerGrpc.OnDeleted(EntityEventGrpcRequest)
EntityEventListenerGrpc.Delete(EntityEventGrpcRequest)
```

gRPC-контракт повторяет ту же семантику, что и HTTP:

- `Create` создаёт remote listener на время обработки одной сущности;
- один `On*` вызов = одно событие;
- `Delete` удаляет remote listener после последней стадии;
- передаётся `managerName`, `tableName`, `stage`, `isNew`, `userConnection` и типизированные `values`;
- ответ сообщает, обработано ли событие, было ли оно отменено и есть ли ошибка.

Практический смысл такого разделения:

- локальный listener подходит для лёгкой бизнес-логики рядом с ORM;
- внешний listener подходит для тяжёлой или изолированной обработки, которую нужно вынести в отдельное приложение.

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
          "AuthorizationProviderType": "Titanic.Entity.WebApplication.Api.HeaderEntityApiAuthorizationProvider, Titanic.Entity",
          "DefaultBatchExecutionMode": "Sequential"
        },
        "EventListener": "",
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
- `Api.AuthorizationProviderType` — тип провайдера, который авторизует запрос и возвращает `UserConnection`.
- `EventListener` — способ вызова событийного слоя: пусто для локального режима, непустое значение для внешнего listener-сервиса.
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
