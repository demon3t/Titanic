# Titanic.Db

## Назначение сборки

`Titanic.Db` - базовая сборка доступа к данным. Она предоставляет fluent SQL builder, абстракции SQL-диалекта, провайдеры подключения и тонкий слой выполнения запросов.

Сборка отвечает за нижний слой работы с БД:

- построение SQL через типизированные builder-объекты;
- параметризацию значений через `Column.Parameter(...)`;
- выполнение `SELECT`, `INSERT`, `UPDATE`, `DELETE` и DDL-команд;
- регистрацию провайдеров БД через `DbManager` и DI;
- PostgreSQL-реализацию провайдера и SQL-движка.

Это не Entity Framework Core и не LINQ provider. В проекте нет `DbContext`, репозиториев и миграций EF Core. Entity ORM находится выше, в `Titanic.Entity`, и использует `Titanic.Db` как SQL-движок.

## Содержание

Основные пользовательские классы:

- `DbManager` - статический реестр провайдеров и database wrappers. Используется для получения `BaseDbProvider` или `BaseDatabase` по имени.
- `Database` / `BaseDatabase` - высокоуровневая обертка над провайдером. Создает `Select`, `InsertSelect`, `Update`, `Delete`, `Table` и выполняет запросы.
- `BaseDbProvider` - базовый класс провайдера БД. Отвечает за создание команд, выполнение SQL, `Execute`, `ExecuteScalar`, `ExecuteReader`, `Query`.
- `PostgresProvider` - реализация провайдера PostgreSQL.
- `BaseDbEngine` - базовый SQL renderer для диалекта БД.
- `PostgresEngine` - PostgreSQL renderer: quote identifiers, операторы, функции, join keywords, SQL parts.
- `Select` - fluent builder `SELECT`.
- `InsertSelect` - builder `INSERT VALUES`, `INSERT FROM SELECT`, `ON CONFLICT`.
- `Update` - builder `UPDATE`.
- `Delete` - builder `DELETE`.
- `Table` - DDL helper: создание, удаление, изменение таблиц, индексы, `Exists`, `RowCount`.
- `Column` - фабрика выражений колонок, параметров, констант, `*`, подзапросов.
- `Func` - фабрика SQL-функций: `Count`, `Sum`, `Avg`, `Min`, `Max`, `Coalesce`, `Upper`, `Lower`, `Case`, `Custom`.
- `QueryBuildResult` - результат `Build()`: SQL и список параметров.
- `QueryParameter` - имя и значение SQL-параметра.
- `DbReader` / `IDbReader` - сервис чтения и выполнения запросов через зарегистрированный `DbManager`.
- `DbDataReaderExtensions` - extension-методы для чтения значений из `DbDataReader`.
- `DbConfig`, `DbProviderConfig`, `ProviderTypeConfig`, `ConnectionPoolConfig` - конфигурация провайдеров.
- `ServiceCollectionExtensions` - регистрация `Titanic.Db` в `IServiceCollection`.
- `WebAppDbExtensions` - регистрация `Titanic.Db` через `WebApplicationBuilder`.

Публичные builder-классы, которые обычно используются через цепочки вызовов:

- `WhereItem<TQuery>` - fluent-условия `IsEqual`, `IsGreaterThan`, `IsLike`, `IsNull`, `Between`, `In`.
- `WhereBuilder<TParent>` - группировка условий через `AndOpen`, `OrOpen`, `Close`, `End`.
- `JoinItem` - построение `JOIN ... ON ...`.
- `ColumnItem` - продолжение после `Column(...)`, включая `As(...)`.
- `FromItem` - продолжение после `From(...)`, включая `As(...)`.
- `PagingItem` - `Limit(...).Take(...)`, `Skip(...)`, `Page(...)`.
- `HavingExpression` - fluent `HAVING`.
- `CaseItem`, `CaseWhenItem`, `CaseThenItem` - fluent `CASE WHEN`.

Enums:

- `ConditionOperator` - SQL-операторы условий.
- `JoinType` - типы join.
- `SqlFunction` - известные SQL-функции.
- `DatabaseType` - встроенные типы БД.
- `DataValueType` - базовые типы значений, используемые также `Titanic.Entity`.

### Entity Framework Core Context

В текущей реализации `Titanic.Db` не содержит EF Core `DbContext`. Слой работает через собственные SQL builder-ы и `BaseDbProvider`.

Если нужен EF Core рядом с текущим builder-ом, его лучше добавлять отдельным проектом или отдельным адаптером, чтобы не смешивать два подхода доступа к данным.

Пример возможного контекста:

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
}
```

### Репозитории

В проекте нет готового repository layer. Рекомендуемый текущий подход - использовать `DbManager`, `IDbReader`, `Database` или `BaseDbProvider` напрямую в сервисах приложения.

Если нужен репозиторий, делайте его в прикладном проекте, поверх `Titanic.Db`:

```csharp
public sealed class DepartmentRepository
{
    private readonly IDbReader _reader;

    public DepartmentRepository(IDbReader reader)
    {
        _reader = reader;
    }

    public IReadOnlyList<string> GetNames()
    {
        var query = DbManager.GetProvider()
            .Select()
            .Column("name")
            .From("departments").As("d")
            .OrderBy("d", "name");

        return _reader.ExecuteReader(query, r => r.Get<string>("name"));
    }
}
```

### Миграции

EF Core migrations в проекте отсутствуют. Для DDL сейчас есть `Table` и прямое выполнение SQL через provider.

Пример DDL через `Table`:

```csharp
var db = DbManager.GetProvider();

db.Table("departments").CreateIfNotExists(
    new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
    new Table.ColumnDefinition("name", Table.ColumnType.Text, notNull: true),
    new Table.ColumnDefinition("description", Table.ColumnType.Text));
```

Если нужны полноценные миграции, стоит добавить отдельный migration runner или EF Core migration project. Минимальный вариант - завести таблицу версий и набор SQL-скриптов, выполняемых через `BaseDbProvider.Execute(...)`.

## Зависимости

NuGet и framework dependencies:

- `Npgsql` `10.0.2` - PostgreSQL provider.
- `Microsoft.Extensions.Configuration.Binder` `9.0.5` - binding конфигурации.
- `Microsoft.Extensions.DependencyInjection.Abstractions` `9.0.5` - DI abstractions.
- `Microsoft.Extensions.Options.ConfigurationExtensions` `9.0.5` - options/config integration.
- `Microsoft.AspNetCore.App` - framework reference для web extension-методов.

Project references:

- `Titanic.Common`.

## Использование

Регистрация в ASP.NET Core:

```csharp
using Titanic.Db.WebApplication;

var builder = WebApplication.CreateBuilder(args);

builder.AddTitanicDb("TitanicDb");

var app = builder.Build();
app.Run();
```

Регистрация в `IServiceCollection`:

```csharp
using Titanic.Db;

services.AddTitanicDb(configuration, "TitanicDb");
```

Ручная инициализация:

```csharp
using Titanic.Db;
using Titanic.Db.Configuration;
using Titanic.Db.PosgreSql;

DbManager.Initialize(new DbConfig
{
    DefaultProviderName = "main",
    Providers =
    [
        new DbProviderConfig
        {
            Name = "main",
            ConnectionString = "",
            Types = new ProviderTypeConfig
            {
                ProviderType = "Titanic.Db.PosgreSql.PostgresProvider, Titanic.Db",
                EngineType = "Titanic.Db.PosgreSql.PostgresEngine, Titanic.Db"
            }
        }
    ]
});
```

Пример `SELECT`:

```csharp
using Titanic.Db;

var provider = DbManager.GetProvider("main");

var rows = provider.Select()
    .Column("e", "id")
    .Column("e", "name")
    .Column("d", "name").As("department_name")
    .From("employees").As("e")
    .LeftJoin("departments").As("d")
        .On("e", "department_id").IsEqual("d", "id")
    .Where("e", "is_active").IsEqual(Column.Parameter(true))
    .OrderBy("e", "name")
    .Limit(20)
    .Take(20)
    .ExecuteReader(r => new
    {
        Id = r.Get<int>("id"),
        Name = r.Get<string>("name"),
        Department = r.Get<string>("department_name")
    });
```

Пример `INSERT`:

```csharp
var id = provider.Insert("departments")
    .SetColumns("name", "description")
    .Values(
        Column.Parameter("Engineering"),
        Column.Parameter("Product development"))
    .Returning("id")
    .ExecuteScalar<int>();
```

Пример `UPDATE`:

```csharp
var affected = provider.Update("departments")
    .Table("departments", "d")
    .Set("description", Column.Parameter("Platform team"))
    .Where("d", "id").IsEqual(Column.Parameter(id))
    .Execute();
```

Пример `DELETE`:

```csharp
var deleted = provider.Delete("departments")
    .From("departments", "d")
    .Where("d", "id").IsEqual(Column.Parameter(id))
    .Execute();
```

## Конфигурация

Пример `appsettings.json`:

```json
{
  "TitanicDb": {
    "DefaultProviderName": "main",
    "Providers": [
      {
        "Name": "main",
        "ConnectionString": "",
        "Types": {
          "ProviderType": "Titanic.Db.PosgreSql.PostgresProvider, Titanic.Db",
          "EngineType": "Titanic.Db.PosgreSql.PostgresEngine, Titanic.Db"
        },
        "Pool": {
          "MaxPoolSize": 8,
          "MinPoolSize": 0,
          "ConnectionLifetimeSeconds": 0,
          "ConnectionIdleTimeoutSeconds": 0
        }
      }
    ]
  }
}
```

Ключевые настройки:

- `DefaultProviderName` - провайдер по умолчанию для `DbManager.GetProvider()`.
- `Providers[].Name` - имя провайдера в реестре.
- `Providers[].ConnectionString` - строка подключения.
- `Providers[].Types.ProviderType` - тип `BaseDbProvider`.
- `Providers[].Types.EngineType` - тип `BaseDbEngine`.
- `Providers[].Pool` - настройки пула подключений.

## Примечания

- Используйте `Column.Parameter(...)` для пользовательских значений.
- `Column.Const(...)` предназначен для SQL-констант, а не для пользовательского ввода.
- `QueryExpression` является низкоуровневым AST выражений; прикладной код обычно должен строить запросы через fluent API.
- `Select.Limit(...)` возвращает `PagingItem`; завершайте цепочку через `Take`, `Skip` или `Page`.
