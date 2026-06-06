# Titanic.Entity

## Назначение сборки

`Titanic.Entity` - слой Entity ORM поверх `Titanic.Db`. Проект описывает таблицы через CLR-модели и атрибуты, строит Entity Schema Query, материализует строки БД в `Titanic.Entity.Orm.Entity`, поддерживает `Save()` и `Delete()`, а также умеет автоматически публиковать HTTP API для работы с Entity ORM.

CLR-класс в этом проекте используется как metadata-модель таблицы, а не как объект результата. Результат чтения хранится в `Entity` как словарь `имя колонки или алиас -> ColumnValue`, поэтому одна запись может содержать значения корневой таблицы, связанных таблиц, `DisplayValue` и агрегаты.

## Основные принципы

- `Titanic.Entity` не должен быть жестко привязан к конкретной прикладной сущности.
- Получение `BaseEntityManager` выполняется по типу менеджера, а не по строковому имени.
- `UserConnection` обязателен для создания `EntitySchemaQuery`, `EntitySelectBuilder`, `Entity`, `Save()` и `Delete()`.
- Данные пользователя, включая культуру локализации, должны приходить из `UserConnection`, обычно через сервис авторизации API.
- `Save()` сам определяет insert или update по состоянию `Entity.IsNew` и наличию первичного ключа.
- SQL строится через ORM `Titanic.Db`; пользовательский код не должен собирать SQL строками.
- Локализация определяется свойством колонки `isLocalized: true`, а не JSON-моделью запроса.

## Содержание

### Менеджеры

- `EntityManager` - статическая точка входа для регистрации менеджеров, получения менеджера по типу, создания `EntitySchemaQuery`, `EntitySelectBuilder` и `Entity`.
- `BaseEntityManager` - базовая обертка над `BaseDbProvider`; хранит провайдер, настройки API, runtime-опции и проверку схемы БД.
- `EntityDbManager` - стандартная реализация `BaseEntityManager` без дополнительной прикладной логики.

### Запросы

- `EntitySchemaQuery` - основная модель запроса сущностей из БД.
- `EntitySchemaQuery<TEntity>` - generic-обертка над `EntitySchemaQuery`.
- `ESQ` и `ESQ<TEntity>` - устаревшие alias-типы для совместимости. Новый код должен использовать `EntitySchemaQuery`.
- `EntitySelectBuilder` - ORM SELECT builder по путям колонок.
- `EntityQueryColumnCollection`, `EntityQueryColumn` - коллекция и описание колонок ESQ.
- `EntityQueryFilterCollection`, `EntityQueryFilter` - коллекция и описание фильтров ESQ.
- `EntityWhereItem` - fluent-условия для `EntitySelectBuilder`.
- `EntityAggregationType` - типы агрегации: `Count`, `Sum`, `Avg`, `Min`, `Max`.
- `EntityLogicalOperation` - логика объединения фильтров: `And`, `Or`.
- `ESQJsonModel` - JSON-модель запроса для передачи ESQ по HTTP.

### Сущность и значения колонок

- `Entity` - ORM-сущность, представляющая одну запись БД.
- `ColumnValue` - базовый класс значения колонки с `Value` и `DisplayValue`.
- `ScalarColumnValue` - значение обычной скалярной колонки.
- `StringColumnValue` - значение строковой колонки.
- `ReferenceColumnValue` - значение ссылочной колонки с `DisplayValue` связанной записи.

### Атрибуты моделей

- `EntityAttribute` - задает имя таблицы или view.
- `PrimaryColumnAttribute` - задает первичную колонку.
- `DisplayColumnAttribute` - задает отображаемую колонку сущности.
- `ColumnAttribute` - задает обычную колонку и ее тип.
- `StringColumnAttribute` - задает обычную текстовую колонку без явного `DataValueType.String`.
- `ReferenceColumnAttribute` - задает ссылочную колонку на другую таблицу.
- `DisableLocalizationAttribute` - отключает локализацию для сущности или конкретной колонки.
- `EntityManagerConnectionAttribute` - задает имя подключения/менеджера для пользовательской обертки менеджера.

### Структура metadata

- `Structure` - сканирует сборки и хранит metadata Entity-моделей.
- `EntityStructure` - описание таблицы.
- `ColumnStructure` - описание колонки.
- `EntitySchemaValidator` - проверяет наличие таблиц и колонок в БД при инициализации менеджера.

### Entity API

- `ServiceCollectionExtensions` - регистрация Entity ORM сервисов в DI.
- `WebApplicationExtensions` - регистрация Entity ORM и публикация endpoint-ов в ASP.NET Core.
- `EntityManagerConfig`, `EntityManagerSettings`, `EntityManagerApiSettings`, `EntityManagerOptions` - конфигурация менеджеров, API и runtime-опций.
- `EntityApiRequest` - единая HTTP-модель операции.
- `EntityApiBatchRequest` - batch-модель для нескольких операций.
- `EntityApiOperationType` - операции `Select`, `Save`, `Delete`.
- `EntityApiBatchExecutionMode` - режимы `Sequential` и `Parallel`.
- `IEntityApiAuthorizationProvider` - контракт авторизации API и получения `UserConnection`.
- `HeaderEntityApiAuthorizationProvider` - простая авторизация по HTTP-заголовку.
- `EntityApiOperationResult`, `EntityApiBatchResponse`, `EntityApiColumnValueResponse` - модели ответа.

## Описание модели

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

[Entity("employees")]
public sealed class EmployeeEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name")]
    public string Name { get; set; } = string.Empty;

    [StringColumn("email")]
    public string Email { get; set; } = string.Empty;

    [ReferenceColumn("department_id", "departments", DataValueType.Guid)]
    public int? DepartmentId { get; set; }
}
```

Имя таблицы в `[Entity("...")]` задается без схемы. Если колонка локализуемая, таблица локализации формируется по паттерну `sys_[имя_основной_таблицы]_lcz`, например `departments -> sys_departments_lcz`.

## Регистрация в ASP.NET Core

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

## Конфигурация

```json
{
  "TitanicEntity": {
    "Managers": [
      {
        "Name": "posgreTest",
        "DbProviderName": "posgreTest",
        "ManagerType": "MyApp.EntityManagers.PostgresEntityManager, MyApp",
        "Api": {
          "AutoRegisterEndpoint": true,
          "Path": "/entity/posgreTest",
          "AuthorizationHeaderName": "X-Entity-Key",
          "AuthorizationProviderType": "Titanic.Entity.WebApplication.Api.HeaderEntityApiAuthorizationProvider, Titanic.Entity",
          "DefaultBatchExecutionMode": "Sequential"
        },
        "ValidateDatabaseSchemaOnCompile": true,
        "Options": {
          "MaxReadRowCount": 20000,
          "Values": {}
        }
      }
    ]
  }
}
```

Ключевые настройки:

- `Name` - диагностическое имя менеджера в конфигурации.
- `DbProviderName` - имя провайдера из `Titanic.Db.DbManager`.
- `ManagerType` - тип пользовательской обертки над `BaseEntityManager`; если не задан, используется `EntityDbManager`.
- `Api.AutoRegisterEndpoint` - публиковать ли API endpoint при вызове `MapTitanicEntityApi()`.
- `Api.Path` - базовый HTTP route менеджера.
- `Api.AuthorizationHeaderName` - заголовок авторизации.
- `Api.AuthorizationProviderType` - тип провайдера авторизации, который реализует `IEntityApiAuthorizationProvider`.
- `Api.DefaultBatchExecutionMode` - режим batch-запросов по умолчанию.
- `ValidateDatabaseSchemaOnCompile` - проверять наличие таблиц и колонок в БД при инициализации менеджера.
- `Options.MaxReadRowCount` - максимальное количество строк, которое разрешено читать одним ESQ-запросом.
- `Options.Values` - дополнительные строковые настройки, не связанные с пользовательским контекстом.

`DefaultManagerName`, `CultureHeaderName` и `DefaultCultureId` в `TitanicEntity` не используются. Культура должна находиться в `UserConnection`, который возвращает сервис авторизации.

## Получение менеджера

Менеджер нужно получать по типу:

```csharp
var manager = EntityManager.GetManager<PostgresEntityManager>();
```

Если зарегистрирован ровно один менеджер, можно использовать короткие методы `EntityManager.Query<TEntity>(userConnection)` и `EntityManager.Create<TEntity>(userConnection)`. Если менеджеров несколько, используйте типизированные overload-ы:

```csharp
var query = EntityManager.Query<PostgresEntityManager, EmployeeEntity>(userConnection);
var entity = EntityManager.Create<PostgresEntityManager, DepartmentEntity>(userConnection);
```

## Чтение через EntitySchemaQuery

```csharp
using Titanic.Db.Enums;
using Titanic.Entity;

var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("Id")
    .AddColumn("Name")
    .AddColumn("DepartmentId", "Department")
    .AddColumn("DepartmentId.Name", "DepartmentName")
    .AddFilter(ConditionOperator.Like, "Email", "%@company.com")
    .OrderBy("Name")
    .GetEntityCollection();

foreach (var row in rows)
{
    var name = row.Get<string>("Name");
    var departmentId = row.Get<int?>("Department");
    var departmentName = row.GetDisplayValue<string>("Department");
}
```

Если колонка является ссылочной и у связанной сущности есть `DisplayColumn`, при выборе самой ссылки Entity ORM добавляет неявное получение `DisplayValue`.

## Выбор всех колонок

Если в ESQ не задать колонки или выставить `AllColumns = true`, будут выбраны все колонки корневой схемы.

```csharp
var query = EntityManager.Query<PostgresEntityManager, DepartmentEntity>(userConnection);
query.AllColumns = true;
query.RowCount = 100;

var rows = query.GetEntityCollection();
```

## Пути колонок и связи

Обычный путь через reference-колонку строит `LEFT JOIN`:

```csharp
var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("DepartmentId.Name", "DepartmentName")
    .GetEntityCollection();
```

Для обратной связи используется descriptor:

```text
[RelationColumn:RelatedPrimaryColumn:MainColumn].Column
```

Пример:

```csharp
var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("Name")
    .AddColumn("[EmployeeId:Id:Id].City", "City")
    .GetEntityCollection();
```

Для этого примера Entity ORM находит модель таблицы, где колонка `EmployeeId` ссылается на `employees`, и строит `RIGHT JOIN` вида `addresses.employee_id = employees.id`.

## Фильтры

```csharp
var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("Name")
    .AddColumn("DepartmentId.Name", "DepartmentName")
    .AddFilter(ConditionOperator.Equal, "DepartmentId.Name", "Engineering")
    .AddFilter(ConditionOperator.Like, "Email", "%@company.com")
    .GetEntityCollection();
```

Фильтры используют те же ORM-пути, что и колонки.

## Paging

```csharp
var query = EntityManager.Query<PostgresEntityManager, EmployeeEntity>(userConnection);
query.AddColumn("Name");
query.RowCount = 20;
query.SkipRow = 40;

var thirdPage = query.GetEntityCollection();
```

`RowCount` используется как `LIMIT`, `SkipRow` - как `OFFSET`. Итоговый `RowCount` ограничивается `Options.MaxReadRowCount`, если он задан в настройках менеджера.

## Агрегации

```csharp
var rows = EntityManager
    .Query<PostgresEntityManager, EmployeeEntity>(userConnection)
    .AddColumn("DepartmentId.Name", "DepartmentName")
    .AddAggregationColumn("Id", EntityAggregationType.Count, "EmployeeCount")
    .AddAggregationColumn("Salary", EntityAggregationType.Avg, "AverageSalary")
    .GroupBy("DepartmentId.Name")
    .GetEntityCollection();
```

## Создание и обновление Entity

```csharp
var department = EntityManager
    .Create<PostgresEntityManager, DepartmentEntity>(userConnection)
    .Set("Name", "Engineering")
    .Set("Description", "Platform team");

department.Save();

var id = department.Get<int>("Id");

department.Set("Description", "Updated description");
department.Save();
```

`Save()` выполняет insert, если первичный ключ не заполнен. Если первичный ключ есть, `Save()` сначала пробует update; если строка не найдена, выполняет insert с указанным первичным ключом.

## Удаление Entity

```csharp
var entity = EntityManager
    .Create<PostgresEntityManager, DepartmentEntity>(userConnection)
    .Set("Id", id);

var deleted = entity.Delete();
```

`Delete()` удаляет только строку корневой таблицы по primary key.

## HTTP API для UI

Подробный контракт HTTP API для фронтенда и клиентских SDK описан в [ENTITY_API.md](ENTITY_API.md).

## JSON API

Entity API публикует два endpoint-а на базовом пути менеджера:

- `POST {Api.Path}` - одна операция.
- `POST {Api.Path}/batch` - несколько операций.

Пример select:

```json
{
  "operation": "Select",
  "query": {
    "tableName": "employees",
    "rowCount": 10,
    "columns": [
      { "path": "Id" },
      { "path": "Name" },
      { "path": "DepartmentId", "alias": "Department" },
      { "path": "DepartmentId.Name", "alias": "DepartmentName" }
    ],
    "filters": {
      "isEnabled": true,
      "logicalOperation": "And",
      "items": [
        { "path": "Name", "comparisonType": "Like", "value": "%John%" }
      ]
    },
    "orders": [
      { "path": "Name", "desc": false }
    ]
  }
}
```

Пример save:

```json
{
  "operation": "Save",
  "tableName": "departments",
  "values": {
    "Name": "Engineering",
    "Description": "Created from API"
  }
}
```

Пример delete:

```json
{
  "operation": "Delete",
  "tableName": "departments",
  "values": {
    "Id": 10
  }
}
```

Пример batch:

```json
{
  "executionMode": "Sequential",
  "requests": [
    {
      "operation": "Save",
      "tableName": "departments",
      "values": {
        "Name": "Batch Department"
      }
    },
    {
      "operation": "Select",
      "query": {
        "tableName": "departments",
        "rowCount": 10,
        "columns": [
          { "path": "Id" },
          { "path": "Name" }
        ]
      }
    }
  ]
}
```

## Авторизация API и UserConnection

API не создает `UserConnection` самостоятельно. Он вызывает `IEntityApiAuthorizationProvider`, а провайдер авторизации должен вернуть `EntityApiAuthorizationResult` с заполненным `UserConnection`.

В тестовом/отладочном сценарии можно использовать `HeaderEntityApiAuthorizationProvider`. Для production-сценария нужно реализовать свой provider, который проверяет пользователя и заполняет `UserConnection`, включая культуру локализации.

## Локализация

Локализация включается на уровне колонки:

```csharp
[DisplayColumn("name", isLocalized: true)]
public string Name { get; set; } = string.Empty;
```

При чтении такой колонки Entity ORM добавляет `LEFT JOIN` к таблице локализации по паттерну:

```text
sys_[table_name]_lcz
```

Ожидаемые колонки таблицы локализации:

- `RecordId` - ссылка на запись основной таблицы.
- колонка культуры, определяемая backend-логикой через `UserConnection`.
- локализуемые колонки основной таблицы.

Если локализованное значение не найдено или пустое, возвращается значение из основной таблицы.

## Проверка схемы БД

Если `ValidateDatabaseSchemaOnCompile = true`, менеджер при инициализации проверяет, что таблицы и колонки, описанные Entity-моделями, существуют в БД.

Эта проверка должна использоваться для раннего обнаружения рассинхронизации модели и БД. Для локального запуска требуется доступная реальная БД с актуальной схемой.

## Зависимости

Project references:

- `Titanic.Common`
- `Titanic.Db`

Прямых NuGet-зависимостей в `Titanic.Entity.csproj` нет. Web-интеграция использует ASP.NET Core типы через framework/reference зависимости решения.

## Ограничения

- `Entity.Save()` сохраняет только колонки корневой таблицы.
- Колонки связанных таблиц, считанные через join, не обновляются автоматически через `Save()` корневой Entity.
- JSON-модель ESQ не должна передавать имя колонки локализации; локализация определяется backend-ом через metadata и `UserConnection`.
- Если зарегистрировано несколько EntityManager, нельзя использовать неявный singleton-менеджер; нужно вызывать методы с типом менеджера.

