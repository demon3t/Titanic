# Titanic.Db

## Роль в архитектуре

`Titanic.Db` — SQL builder и слой выполнения запросов для решения `Titanic`.

Он находится между общей инфраструктурой из `Titanic.Common` и ORM-слоем `Titanic.Entity`:

```text
Titanic.Common -> Titanic.Db -> Titanic.Entity
```

Пакет отвечает за низкоуровневую работу с данными и может использоваться как самостоятельно, так и как основа для Entity ORM.

## Когда использовать

Используйте `Titanic.Db`, если нужно:

- строить SQL типизированно, без ручной сборки строк;
- управлять `SELECT`, `INSERT`, `UPDATE`, `DELETE` и DDL на уровне fluent API;
- контролировать `JOIN`, фильтры, группировки, сортировки и paging;
- подключать конкретный провайдер БД и SQL engine;
- использовать слой доступа к данным отдельно от Entity ORM.

Если вам уже нужны сущности, metadata и HTTP API, следующим слоем будет [Titanic.Entity](../Titanic.Entity/README.md).

## Основные возможности

- fluent builders для `SELECT`, `INSERT`, `UPDATE`, `DELETE`;
- expression model и SQL AST;
- SQL-функции, `CASE`, агрегаты, подзапросы;
- `JOIN`, `GROUP BY`, `HAVING`, `ORDER BY`, paging;
- DDL helper `Table`;
- провайдеры БД и SQL renderer;
- PostgreSQL-реализация из коробки.

## Ключевые типы

### Управление провайдерами и доступом к БД

- `DbManager` — реестр провайдеров и database wrappers.
- `Database` / `BaseDatabase` — высокоуровневая обёртка для создания запросов и выполнения операций.
- `BaseDbProvider` — базовый провайдер БД.
- `PostgresProvider` — PostgreSQL-провайдер.
- `BaseDbEngine` — базовый SQL renderer.
- `PostgresEngine` — renderer для PostgreSQL.

### Построение запросов

- `Select` — builder `SELECT`.
- `InsertSelect` — builder `INSERT` и `INSERT FROM SELECT`.
- `Update` — builder `UPDATE`.
- `Delete` — builder `DELETE`.
- `Table` — DDL helper для таблиц, колонок и индексов.

### Вспомогательные API

- `Column` — фабрика колонок, параметров, констант и подзапросов.
- `Func` — фабрика SQL-функций.
- `QueryExpression` — выражения условий и операторов.
- `QueryBuildResult` — результат `Build()` с SQL и параметрами.
- `QueryParameter` — модель SQL-параметра.
- `DbReader` / `IDbReader` — выполнение запросов и чтение данных.

### Fluent builders условий

- `WhereItem<TQuery>` — условия `IsEqual`, `IsGreaterThan`, `IsLike`, `IsNull`, `Between`, `In`.
- `WhereBuilder<TParent>` — группировка условий через `AndOpen`, `OrOpen`, `Close`, `End`.
- `JoinItem` — построение `JOIN ... ON ...`.
- `PagingItem` — `Limit`, `Take`, `Skip`, `Page`.
- `HavingExpression` — fluent `HAVING`.
- `CaseItem`, `CaseWhenItem`, `CaseThenItem` — fluent `CASE`.

## Граница публичного API

Публичным контрактом пакета считаются:

- точки входа `DbManager`, `ServiceCollectionExtensions`, `WebAppDbExtensions`;
- базовые классы для расширения провайдеров и SQL-диалектов: `BaseDbProvider`, `BaseDbEngine`, `Database`, `BaseDatabase`;
- fluent builders, expression model, SQL enums и модели результата запроса;
- атрибуты таблиц, колонок, индексов и подключений;
- конфигурационные модели `DbConfig`, `DbProviderConfig`, `ProviderTypeConfig`, `ConnectionPoolConfig`;
- `DbReader` / `IDbReader` и extension-методы чтения значений.

Вспомогательные reflection-фабрики, runtime-детали регистрации провайдеров и объект пула подключений не являются внешним контрактом. Для подключения провайдера используйте конфигурацию, `DbManager` или DI extension-методы; для настройки пула — `ConnectionPoolConfig`.

## Пример использования

```csharp
var rows = provider.Select()
    .Column("e", "id")
    .Column("e", "name")
    .From("employees").As("e")
    .Where("e", "is_active").IsEqual(Column.Parameter(true))
    .OrderBy("e", "name")
    .ExecuteReader(r => new
    {
        Id = r.Get<int>("id"),
        Name = r.Get<string>("name")
    });
```

Пример DDL через `Table`:

```csharp
var db = DbManager.GetProvider();

db.Table("departments").CreateIfNotExists(
    new Table.ColumnDefinition("id", Table.ColumnType.Serial, primaryKey: true),
    new Table.ColumnDefinition("name", Table.ColumnType.Text, notNull: true),
    new Table.ColumnDefinition("description", Table.ColumnType.Text));
```

## Что пакет не делает

`Titanic.Db` не является Entity Framework Core и не содержит:

- `DbContext`;
- EF Core migrations;
- repository layer по умолчанию;
- Entity ORM-модель.

Если нужен ORM-слой, он находится в [Titanic.Entity](../Titanic.Entity/README.md).

## Зависимости

Основная project dependency:

- `Titanic.Common`

Технологические зависимости:

- `Npgsql` — PostgreSQL provider;
- `Microsoft.Extensions.*` — конфигурация, DI и options;
- `Microsoft.AspNetCore.App` — web extension-методы.

## Как читать дальше

После знакомства с `Titanic.Db` обычно переходят к одному из двух направлений:

- к прикладному коду, который использует SQL builder напрямую;
- к [Titanic.Entity/README.md](../Titanic.Entity/README.md), если поверх SQL нужен ORM и HTTP API.

## Связанные документы

- [../ARCHITECTURE.md](../ARCHITECTURE.md)
- [../Titanic.Common/README.md](../Titanic.Common/README.md)
- [../Titanic.Entity/README.md](../Titanic.Entity/README.md)
- [../Titanic.Entity/ENTITY_API.md](../Titanic.Entity/ENTITY_API.md)
