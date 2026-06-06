# Entity ORM HTTP API

Документ описывает HTTP-контракт Entity ORM из проекта `Titanic.Entity`. Его цель - дать фронтенду или другому AI достаточно информации, чтобы написать UI-компоненты, клиентский SDK и сценарии взаимодействия с Entity ORM через HTTP.

## Назначение

Entity ORM API позволяет:

- читать данные сущностей через Entity Schema Query;
- создавать и обновлять записи через единую операцию `Save`;
- удалять записи через `Delete`;
- выполнять несколько операций одним batch-запросом;
- работать с обычными колонками, ссылочными колонками, `DisplayValue`, фильтрами, сортировками, paging, группировками и агрегациями.

API является тонкой HTTP-оберткой над Entity ORM. Он не должен содержать прикладную бизнес-логику UI. UI отправляет декларативную модель запроса, а backend строит SQL через `Titanic.Db` и возвращает словарь значений колонок.

## Публикация API

Endpoint-ы поднимаются автоматически для каждого `BaseEntityManager`, у которого в конфигурации включено `Api.AutoRegisterEndpoint`.

```json
{
  "TitanicEntity": {
    "Managers": [
      {
        "Name": "posgreTest",
        "DbProviderName": "posgreTest",
        "ManagerType": "Titanic.EntityApi.EntityManagers.PosgreSqlManager, Titanic.EntityApi",
        "Api": {
          "AutoRegisterEndpoint": true,
          "Path": "/entity/posgreTest",
          "AuthorizationHeaderName": "X-Entity-Key",
          "AuthorizationProviderType": "Titanic.Entity.WebApplication.Api.HeaderEntityApiAuthorizationProvider, Titanic.Entity",
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

Регистрация в приложении:

```csharp
builder.AddTitanicEntityApi("TitanicEntity");

var app = builder.Build();
app.MapTitanicEntityApi();
```

## Base URL

`Api.Path` задается в конфиге менеджера. В примерах ниже используется:

```text
/entity/posgreTest
```

Фронт должен хранить этот путь как настройку окружения.

## Endpoint-ы

| Метод | URL | Назначение |
| --- | --- | --- |
| `POST` | `{Api.Path}` | Выполнить одну операцию `Select`, `Save` или `Delete`. |
| `POST` | `{Api.Path}/batch` | Выполнить несколько операций одним HTTP-запросом. |

Legacy endpoint-ы вида `{Api.Path}/select`, `{Api.Path}/save`, `{Api.Path}/update`, `{Api.Path}/delete` не используются.

## Авторизация и UserConnection

Каждый запрос проходит через `IEntityApiAuthorizationProvider`. Провайдер авторизации должен вернуть `UserConnection`; дальше все операции Entity ORM выполняются только с этим `UserConnection`.

Минимальный тестовый provider `HeaderEntityApiAuthorizationProvider` читает ключ из заголовка, имя которого задано в `Api.AuthorizationHeaderName`.

Пример HTTP-заголовков:

```http
Content-Type: application/json
Accept: application/json
X-Entity-Key: postman-local-user
X-Entity-Culture: 22222222-2222-2222-2222-222222222222
```

`X-Entity-Culture` поддерживается стандартным header-provider как отладочный способ передать культуру. В production лучше реализовать собственный `IEntityApiAuthorizationProvider`, который сам проверяет пользователя и заполняет `UserConnection.Culture`.

## Числовые enum-значения

В запросах лучше передавать enum-значения числами. Это стабильнее для UI-клиента и дешевле для сериализации. Backend также поддерживает строковые значения для совместимости, но примеры ниже используют числа.

### EntityApiOperationType

| Число | Имя | Назначение |
| --- | --- | --- |
| `0` | `Unknown` | Операция не задана или не распознана. |
| `1` | `Select` | Прочитать сущности через EntitySchemaQuery. |
| `2` | `Save` | Создать или обновить сущность через `Entity.Save()`. |
| `3` | `Delete` | Удалить строки корневой сущности по обязательному фильтру. |

### EntityApiBatchExecutionMode

| Число | Имя | Назначение |
| --- | --- | --- |
| `0` | `Sequential` | Выполнять batch-операции по порядку. |
| `1` | `Parallel` | Выполнять независимые batch-операции параллельно. |

### EntityLogicalOperation

| Число | Имя | Назначение |
| --- | --- | --- |
| `0` | `And` | Объединить фильтры через AND. |
| `1` | `Or` | Объединить фильтры через OR. |

### EntityAggregationType

| Число | Имя | Назначение |
| --- | --- | --- |
| `0` | `None` | Обычная колонка без агрегации. |
| `1` | `Count` | COUNT. |
| `2` | `Sum` | SUM. |
| `3` | `Avg` | AVG. |
| `4` | `Min` | MIN. |
| `5` | `Max` | MAX. |

### ConditionOperator

| Число | Имя | Назначение |
| --- | --- | --- |
| `0` | `Equal` | Равно. |
| `1` | `NotEqual` | Не равно. |
| `2` | `GreaterThan` | Больше. |
| `3` | `GreaterThanOrEqual` | Больше или равно. |
| `4` | `LessThan` | Меньше. |
| `5` | `LessThanOrEqual` | Меньше или равно. |
| `6` | `In` | Входит в набор. Для UI напрямую обычно не использовать. |
| `7` | `NotIn` | Не входит в набор. Для UI напрямую обычно не использовать. |
| `8` | `Like` | SQL LIKE. |
| `9` | `NotLike` | SQL NOT LIKE. |
| `10` | `ILike` | PostgreSQL ILIKE, если поддерживается провайдером. |
| `11` | `IsNull` | Значение отсутствует. |
| `12` | `IsNotNull` | Значение заполнено. |
## Единая модель операции

`POST {Api.Path}` принимает объект `EntityApiRequest`.

```ts
type EntityApiOperationType = 0 | 1 | 2 | 3;

interface EntityApiRequest {
  name?: string | null;
  operation: EntityApiOperationType;
  query?: ESQJsonModel | null;
  tableName?: string | null;
  entityTypeName?: string | null;
  values?: Record<string, unknown>;
}
```

Правила:

- `operation = 1` (`Select`) требует поле `query`.
- `operation = 2` (`Save`) требует `tableName` или `entityTypeName`, а также `values`.
- `operation = 3` (`Delete`) требует `tableName`, `entityTypeName` или `query`, а также хотя бы один фильтр в `values` или `query.filters`.
- Отдельного `Update` значения нет. Для обновления используется `operation = 2` (`Save`) с непустым primary key в `values`.
- `tableName` задается без схемы, например `departments`, `employees`.
- `entityTypeName` можно использовать вместо `tableName`, если UI работает с зарегистрированными CLR Entity-моделями.

## Ответ одной операции

Для `POST {Api.Path}` успешный ответ возвращает сразу `result`, без обертки `EntityApiOperationResult`.

Для `Select` результат - массив строк:

```json
[
  {
    "Id": { "value": 1, "displayValue": null },
    "Name": { "value": "Engineering", "displayValue": null },
    "Department": { "value": 10, "displayValue": "Development" }
  }
]
```

Для `Save` результат - сохраненная сущность:

```json
{
  "Name": { "value": "Engineering", "displayValue": null },
  "Description": { "value": "Created from UI", "displayValue": null },
  "Id": { "value": 11313, "displayValue": null }
}
```

Для `Delete` результат:

```json
{
  "deleted": true,
  "affected": 1
}
```

Тип значения колонки:

```ts
interface EntityApiColumnValueResponse<T = unknown> {
  value: T | null;
  displayValue: unknown | null;
}

type EntityApiEntity = Record<string, EntityApiColumnValueResponse>;
```

## Ошибки одной операции

Если операция неуспешна, API возвращает JSON с HTTP status code ошибки:

```json
{
  "error": "Column \"Email\" not found in table \"departments\".",
  "operation": 1
}
```

Типовые статусы:

| Статус | Причина |
| --- | --- |
| `400` | Неверная операция, отсутствует `query`, неизвестная таблица или колонка, невалидные значения. |
| `403` | Авторизация не прошла или provider не вернул `UserConnection`. |
| `404` | Endpoint не опубликован, обычно `Api.AutoRegisterEndpoint = false` или неверный `Api.Path`. |
| `500` | Необработанная ошибка инфраструктуры, подключения к БД или провайдера. |

## Entity Schema Query JSON

`ESQJsonModel` описывает запрос чтения.

```ts
interface ESQJsonModel {
  tableName?: string | null;
  entityTypeName?: string | null;
  columns?: ESQColumnJsonModel[];
  filters?: ESQFilterCollectionJsonModel;
  groupBy?: string[];
  orders?: ESQOrderJsonModel[];
  isDistinct?: boolean;
  allColumns?: boolean;
  skipRowCount?: number | null;
  skipRow?: number | null;
  rowCount?: number | null;
}

interface ESQColumnJsonModel {
  path: string;
  alias?: string | null;
  aggregationType?: EntityAggregationType;
}

interface ESQFilterCollectionJsonModel {
  isEnabled?: boolean;
  logicalOperation?: EntityLogicalOperation;
  items?: ESQFilterJsonModel[];
}

interface ESQFilterJsonModel {
  path?: string;
  comparisonType?: ConditionOperator;
  value?: unknown;
  secondValue?: unknown;
  isEnabled?: boolean;
  isNot?: boolean;
  logicalOperation?: EntityLogicalOperation;
  items?: ESQFilterJsonModel[];
}

interface ESQOrderJsonModel {
  path: string;
  desc?: boolean;
}

type EntityLogicalOperation = 0 | 1;
type EntityAggregationType = 0 | 1 | 2 | 3 | 4 | 5;
```

## Операторы фильтров

Поддерживаемые значения `comparisonType` зависят от `Titanic.Db.Enums.ConditionOperator`. Для UI нужно закладывать такие основные операторы:

| Число | Оператор | Значение фильтра | Назначение |
| --- | --- | --- | --- |
| `0` | `Equal` | `value` | Равно. |
| `1` | `NotEqual` | `value` | Не равно. |
| `2` | `GreaterThan` | `value` | Больше. |
| `3` | `GreaterThanOrEqual` | `value` | Больше или равно. |
| `4` | `LessThan` | `value` | Меньше. |
| `5` | `LessThanOrEqual` | `value` | Меньше или равно. |
| `8` | `Like` | `value` | SQL LIKE. |
| `10` | `ILike` | `value` | Case-insensitive LIKE, если поддерживается провайдером. |
| `9` | `NotLike` | `value` | NOT LIKE. |
| `11` | `IsNull` | без `value` | Значение отсутствует. |
| `12` | `IsNotNull` | без `value` | Значение заполнено. |
| `6` | `In` | subquery | Сейчас рассчитано на backend-subquery, для UI напрямую обычно не использовать. |
| `7` | `NotIn` | subquery | Сейчас рассчитано на backend-subquery, для UI напрямую обычно не использовать. |

Для диапазона используется `secondValue`. В текущей реализации наличие `secondValue` формирует `Between`-условие независимо от `comparisonType`.

```json
{
  "path": "Salary",
  "comparisonType": 3,
  "value": 1000,
  "secondValue": 5000
}
```

## Вложенные фильтры

`filters.items` может содержать не только leaf-фильтры, но и вложенные группы. Группа определяется наличием поля `items`; для нее можно задать `logicalOperation` и `isEnabled`.

Пример условия:

```text
Name LIKE 'A%' AND (Email LIKE '%@t.com' OR Salary >= 1000)
```

JSON:

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "columns": [
      { "path": "Name" }
    ],
    "filters": {
      "logicalOperation": 0,
      "items": [
        { "path": "Name", "comparisonType": 8, "value": "A%" },
        {
          "logicalOperation": 1,
          "items": [
            { "path": "Email", "comparisonType": 8, "value": "%@t.com" },
            { "path": "Salary", "comparisonType": 3, "value": 1000 }
          ]
        }
      ]
    }
  }
}
```

Правила для UI:

- leaf-фильтр содержит `path` и `comparisonType`;
- группа содержит `items` и опциональный `logicalOperation`;
- группу можно вкладывать в другую группу без отдельного discriminator-поля;
- `isEnabled = false` отключает leaf-фильтр или всю группу;
- пустая группа не добавляет условие в SQL.
## Пути колонок

`path` - ORM-путь, а не SQL.

Примеры:

| Path | Что означает |
| --- | --- |
| `Id` | Колонка корневой сущности. |
| `Name` | Колонка корневой сущности. |
| `DepartmentId` | Ссылочная колонка; ответ может содержать `displayValue`. |
| `DepartmentId.Name` | Колонка `Name` связанной сущности через LEFT JOIN. |
| `[EmployeeId:Id:Id].City` | Обратная связь через RIGHT JOIN descriptor. |

Формат обратной связи:

```text
[RelationColumn:RelatedPrimaryColumn:MainColumn].Column
```

Например для корневой таблицы `employees` путь `[EmployeeId:Id:Id].City` ищет Entity-модель, где колонка `EmployeeId` ссылается на `employees`, и строит связь:

```sql
RIGHT JOIN addresses ON addresses.employee_id = employees.id
```

UI не должен давать пользователю вводить SQL. UI должен работать только с доступными ORM-путями, которые известны из metadata/конфигурации формы.

## Выбор всех колонок

Есть три способа выбрать все колонки корневой схемы:

```json
{
  "operation": 1,
  "query": {
    "tableName": "departments",
    "allColumns": true,
    "rowCount": 20
  }
}
```

```json
{
  "operation": 1,
  "query": {
    "tableName": "departments",
    "columns": [],
    "rowCount": 20
  }
}
```

```json
{
  "operation": 1,
  "query": {
    "tableName": "departments",
    "columns": [
      { "path": "*" }
    ],
    "rowCount": 20
  }
}
```

Для UI-таблиц лучше явно указывать нужные колонки. `allColumns` удобен для отладки и простых карточек.

## Select: простой список

```http
POST /entity/posgreTest
Content-Type: application/json
X-Entity-Key: postman-local-user
```

```json
{
  "operation": 1,
  "query": {
    "tableName": "departments",
    "rowCount": 20,
    "columns": [
      { "path": "Id" },
      { "path": "Name" },
      { "path": "Description" }
    ],
    "filters": {
      "isEnabled": true,
      "logicalOperation": 0,
      "items": [
        { "path": "Name", "comparisonType": 8, "value": "%Department%" }
      ]
    },
    "orders": [
      { "path": "Name", "desc": false }
    ]
  }
}
```

## Select: paging

`rowCount` используется как `LIMIT`, `skipRow` или `skipRowCount` - как `OFFSET`.

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "rowCount": 25,
    "skipRow": 50,
    "columns": [
      { "path": "Id" },
      { "path": "Name" },
      { "path": "Email" }
    ],
    "orders": [
      { "path": "Name", "desc": false }
    ]
  }
}
```

UI pagination:

```ts
const rowCount = pageSize;
const skipRow = pageIndex * pageSize;
```

Backend дополнительно ограничивает чтение через `Options.MaxReadRowCount`.

## Select: ссылки и DisplayValue

Если выбрать ссылочную колонку, Entity ORM может вернуть `displayValue` из display-колонки связанной сущности.

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "rowCount": 10,
    "columns": [
      { "path": "Id" },
      { "path": "Name" },
      { "path": "DepartmentId", "alias": "Department" }
    ],
    "orders": [
      { "path": "Name" }
    ]
  }
}
```

Пример ответа:

```json
[
  {
    "Id": { "value": 1, "displayValue": null },
    "Name": { "value": "Ivan", "displayValue": null },
    "Department": { "value": 10, "displayValue": "Engineering" }
  }
]
```

Для lookup-компонента UI обычно нужно показывать `displayValue`, а сохранять `value`.

## Select: явный JOIN по пути

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "rowCount": 10,
    "columns": [
      { "path": "Id" },
      { "path": "Name" },
      { "path": "DepartmentId.Name", "alias": "DepartmentName" }
    ],
    "filters": {
      "logicalOperation": 0,
      "items": [
        { "path": "DepartmentId.Name", "comparisonType": 0, "value": "Engineering" }
      ]
    },
    "orders": [
      { "path": "DepartmentId.Name" },
      { "path": "Name" }
    ]
  }
}
```

## Select: агрегации и группировка

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "columns": [
      { "path": "DepartmentId.Name", "alias": "DepartmentName" },
      { "path": "Id", "alias": "EmployeeCount", "aggregationType": 1 },
      { "path": "Salary", "alias": "AverageSalary", "aggregationType": 3 },
      { "path": "Salary", "alias": "TotalSalary", "aggregationType": 2 }
    ],
    "groupBy": [
      "DepartmentId.Name"
    ],
    "orders": [
      { "path": "DepartmentId.Name" }
    ]
  }
}
```

`Count(*)`:

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "columns": [
      { "path": "*", "alias": "EmployeeCount", "aggregationType": 1 }
    ]
  }
}
```

## Select: distinct

```json
{
  "operation": 1,
  "query": {
    "tableName": "employees",
    "isDistinct": true,
    "columns": [
      { "path": "DepartmentId.Name", "alias": "DepartmentName" }
    ],
    "orders": [
      { "path": "DepartmentId.Name" }
    ]
  }
}
```

## Локализация

UI не передает имя таблицы или колонки локализации. Локализация определяется backend metadata:

```csharp
[DisplayColumn("name", isLocalized: true)]
public string Name { get; set; } = string.Empty;
```

Если колонка локализуемая, Entity ORM сам присоединяет таблицу локализации по паттерну:

```text
sys_[table_name]_lcz
```

Например:

```text
departments -> sys_departments_lcz
```

Культура берется из `UserConnection`, который создает authorization provider. Для стандартного header-provider можно передать:

```http
X-Entity-Culture: 22222222-2222-2222-2222-222222222222
```

Если локализованное значение не найдено или пустое, backend возвращает значение основной таблицы.

## Безопасность Save и Delete

Write-операции не должны выполняться без фильтра, чтобы случайно не изменить или не удалить много строк.

Правила:

- `Save` без primary key считается созданием новой записи.
- `Save` с непустым primary key считается обновлением записи и выполняет update по primary key.
- `Save` с переданным, но пустым primary key возвращает `400 BadRequest`.
- `Delete` требует хотя бы один непустой фильтр.
- `Delete` принимает простые equality-фильтры через `values`.
- `Delete` принимает произвольные ORM-фильтры через `query.filters`, включая фильтры по связанным таблицам.
- `Delete` без фильтра возвращает `400 BadRequest`.
- Произвольный update по набору фильтров через HTTP API не поддерживается намеренно; для обновления используется `Save` по primary key.

Пример ошибки delete без фильтра:

```json
{
  "error": "Delete operation requires at least one non-empty filter.",
  "operation": 3
}
```
## Save: создание записи

```json
{
  "operation": 2,
  "tableName": "departments",
  "values": {
    "Name": "Postman Department",
    "Description": "Created from UI"
  }
}
```

Если первичный ключ не передан, `Save()` выполняет insert. В ответе должна появиться первичная колонка, если провайдер поддерживает `RETURNING`.

## Save: обновление записи

```json
{
  "operation": 2,
  "tableName": "departments",
  "values": {
    "Id": 11313,
    "Name": "Updated Department",
    "Description": "Updated from UI"
  }
}
```

Если первичный ключ передан, `Save()` сначала выполняет update. Если update не затронул строки, Entity ORM может выполнить insert с указанным первичным ключом.

Для UI это означает:

- форма создания отправляет `Save` без primary key;
- форма редактирования отправляет `Save` с primary key;
- отдельная операция `Update` не нужна.

## Delete

Удаление по primary key:

```json
{
  "operation": 3,
  "tableName": "departments",
  "values": {
    "Id": 11313
  }
}
```

Удаление по любой колонке корневой сущности:

```json
{
  "operation": 3,
  "tableName": "departments",
  "values": {
    "Name": "Temporary Department"
  }
}
```

Удаление по ESQ-фильтру, включая связанные таблицы:

```json
{
  "operation": 3,
  "query": {
    "tableName": "employees",
    "filters": {
      "items": [
        { "path": "DepartmentId.Name", "comparisonType": 0, "value": "Archived Department" }
      ]
    }
  }
}
```

HTTP `Delete` сначала читает коллекцию сущностей по переданному фильтру, затем удаляет найденные сущности по одной через `Entity.Delete()`. Сейчас удаление найденных сущностей может выполняться параллельно. Связанные таблицы не удаляются автоматически.

## Batch

`POST {Api.Path}/batch` принимает `EntityApiBatchRequest`. Каждый элемент batch может иметь строковое `name`, чтобы фронт явно сопоставил ответ с исходным запросом. Если `name` не задано, backend назначит случайный `Guid` в строковом виде и вернет его в результате операции.

```ts
interface EntityApiBatchRequest {
  executionMode?: 0 | 1 | null;
  requests: EntityApiRequest[];
}

interface EntityApiBatchResponse {
  executionMode: 0 | 1;
  results: EntityApiOperationResult[];
}

interface EntityApiOperationResult {
  name?: string | null;
  operation: EntityApiOperationType;
  success: boolean;
  statusCode: number;
  result?: unknown;
  errorMessage?: string | null;
}
```

Пример:

```json
{
  "executionMode": 0,
  "requests": [
    {
      "name": "createDepartment",
      "operation": 2,
      "tableName": "departments",
      "values": {
        "Name": "Batch Department"
      }
    },
    {
      "name": "loadDepartments",
      "operation": 1,
      "query": {
        "tableName": "departments",
        "rowCount": 10,
        "columns": [
          { "path": "Id" },
          { "path": "Name" }
        ],
        "orders": [
          { "path": "Name" }
        ]
      }
    }
  ]
}
```

Имена операций:

- `name` должно быть строкой.
- `name` нужно задавать с фронта, если результат важен для конкретного UI-компонента.
- если `name` не задано или пустое, backend назначит строковый `Guid`.
- `name` возвращается в `EntityApiOperationResult.name` для каждой операции batch.

Режимы:

- `0` (`Sequential`) - операции выполняются по порядку. Использовать, если следующие операции зависят от предыдущих.
- `1` (`Parallel`) - операции запускаются параллельно. Использовать только для независимых запросов.

Если `executionMode` не передан, используется `Api.DefaultBatchExecutionMode` из конфигурации менеджера.

## Рекомендации для UI

### Таблица со списком

Минимальная модель состояния:

```ts
interface EntityListState {
  tableName: string;
  columns: ESQColumnJsonModel[];
  filters: ESQFilterCollectionJsonModel;
  orders: ESQOrderJsonModel[];
  pageIndex: number;
  pageSize: number;
}
```

Построение запроса:

```ts
function buildListRequest(state: EntityListState): EntityApiRequest {
  return {
    operation: 1,
    query: {
      tableName: state.tableName,
      columns: state.columns,
      filters: state.filters,
      orders: state.orders,
      rowCount: state.pageSize,
      skipRow: state.pageIndex * state.pageSize
    }
  };
}
```

### Карточка записи

Для загрузки карточки:

```json
{
  "operation": 1,
  "query": {
    "tableName": "departments",
    "rowCount": 1,
    "allColumns": true,
    "filters": {
      "items": [
        { "path": "Id", "comparisonType": 0, "value": 11313 }
      ]
    }
  }
}
```

Для сохранения карточки:

```ts
function buildSaveRequest(tableName: string, values: Record<string, unknown>): EntityApiRequest {
  return {
    operation: 2,
    tableName,
    values
  };
}
```

### Lookup field

Для lookup-компонента выбирайте primary key и display column:

```json
{
  "operation": 1,
  "query": {
    "tableName": "departments",
    "rowCount": 20,
    "columns": [
      { "path": "Id" },
      { "path": "Name" }
    ],
    "filters": {
      "items": [
        { "path": "Name", "comparisonType": 8, "value": "%eng%" }
      ]
    },
    "orders": [
      { "path": "Name" }
    ]
  }
}
```

В форме для ссылочной колонки храните `value`, показывайте `displayValue` или отдельно загруженный `Name`.

### Обработка ответа в UI

```ts
function getCellValue(row: EntityApiEntity, alias: string): unknown {
  return row[alias]?.displayValue ?? row[alias]?.value ?? null;
}
```

Для редактирования лучше брать `value`, для отображения - `displayValue ?? value`.

## TypeScript клиент

```ts
export class EntityOrmClient {
  constructor(
    private readonly baseUrl: string,
    private readonly apiPath: string,
    private readonly getHeaders: () => Record<string, string>
  ) {}

  async execute<T = unknown>(request: EntityApiRequest): Promise<T> {
    const response = await fetch(`${this.baseUrl}${this.apiPath}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Accept": "application/json",
        ...this.getHeaders()
      },
      body: JSON.stringify(request)
    });

    const payload = await response.json().catch(() => null);

    if (!response.ok) {
      const message = payload?.error ?? `Entity API request failed with ${response.status}`;
      throw new Error(message);
    }

    return payload as T;
  }

  async batch(request: EntityApiBatchRequest): Promise<EntityApiBatchResponse> {
    const response = await fetch(`${this.baseUrl}${this.apiPath}/batch`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Accept": "application/json",
        ...this.getHeaders()
      },
      body: JSON.stringify(request)
    });

    const payload = await response.json().catch(() => null);

    if (!response.ok) {
      const message = payload?.error ?? `Entity API batch failed with ${response.status}`;
      throw new Error(message);
    }

    return payload as EntityApiBatchResponse;
  }
}
```

## Валидация на фронте

Перед отправкой запроса UI должен проверять:

- `operation` входит в `Select`, `Save`, `Delete`;
- для `Select` заполнен `query.tableName` или `query.entityTypeName`;
- для `Save` и `Delete` заполнен `tableName` или `entityTypeName`;
- для `Delete` заполнен хотя бы один фильтр в `values` или `query.filters`;
- `rowCount` не превышает лимит, ожидаемый UI; backend все равно применит `Options.MaxReadRowCount`;
- фильтры не содержат пустой `path`;
- сортировки не содержат пустой `path`;
- пользователь не вводит raw SQL.

## Ограничения текущего API

- API не возвращает metadata схемы отдельным endpoint-ом. UI должен иметь metadata из своей конфигурации или другого backend endpoint-а.
- Нет отдельной операции `Update`; используется только `Save`.
- Batch `Parallel` не гарантирует порядок выполнения и не должен использоваться для зависимых операций.
- `In` и `NotIn` рассчитаны на backend subquery и не являются удобным UI-оператором в текущей JSON-модели.
- `Save` и `Delete` работают с корневой таблицей; связанные таблицы, прочитанные через JOIN, не сохраняются автоматически.
- Локализация настраивается в backend metadata и `UserConnection`, а не в JSON-запросе.






