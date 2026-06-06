# Titanic.Common

## Назначение сборки

`Titanic.Common` - общий инфраструктурный пакет для проектов Titanic. Он содержит пользовательский контекст, базовые типы культуры, in-memory авторизацию по HTTP-заголовку и ASP.NET Core extension-методы, которые используются поверх `Titanic.Db` и `Titanic.Entity`.

Пакет не зависит от `Titanic.Db` и `Titanic.Entity`, поэтому может использоваться как базовый слой для web-инфраструктуры и пользовательского контекста.

## Содержание

Пользовательский контекст:

- `UserConnection` - контекст пользователя. Содержит `UserId` и `UserCulture`.
- `UserCulture` - культура пользователя. Содержит `Id` и `Name`.

Авторизация:

- `IAuthorizationCollection` - контракт коллекции авторизаций по ключу.
- `EntityAuthorizationCollection` - in-memory коллекция ключей авторизации для Entity API. Хранит `UserConnection` и время последнего обращения.
- `EntityAuthorizationRequirement` - authorization requirement для Entity API.
- `EntityAuthorizationHandler` - обработчик авторизации по заголовку `X-Entity-Key`.
- `MockEntityAuthorizationHandler` - mock-обработчик для локальной отладки и тестов.
- `BaseHeaderAuthorizationHandler<TCollection, TRequirement>` - базовый ASP.NET Core authorization handler, который читает ключ из HTTP-заголовка.
- `BaseMockHeaderAuthorizationHandler<TCollection, TRequirement>` - базовый mock handler, принимающий заголовок с префиксом `Mock_`.

ASP.NET Core:

- `WebApplicationExtensions.AddHeaderAuthorization<THandler, TCollection, TRequirement>(...)` - регистрирует коллекцию авторизаций, handler и policy.
- `WebApplicationExtensions.AddEmptyAuthentication()` - добавляет пустую схему аутентификации.
- `WebApplicationExtensions.AddTitanicInfrastructure()` - добавляет endpoint API explorer и Swagger.

Внутренние типы:

- `EmptyAuthenticationHandler` - internal authentication handler для пустой схемы.

## Использование

Создание пользовательского контекста:

```csharp
using Titanic.Common.Session;

var userConnection = new UserConnection
{
    UserId = Guid.NewGuid(),
    Culture = new UserCulture
    {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Name = "ru-RU"
    }
};
```

Регистрация общей инфраструктуры в ASP.NET Core:

```csharp
using Titanic.Common.WebApplication;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddTitanicInfrastructure()
    .AddEmptyAuthentication();
```

Регистрация header-authorization policy:

```csharp
using Titanic.Common.Services.Authorization.Entity;
using Titanic.Common.WebApplication;

builder.AddHeaderAuthorization<
    EntityAuthorizationHandler,
    EntityAuthorizationCollection,
    EntityAuthorizationRequirement>("Entity");
```

Mock-авторизация для локальной проверки:

```csharp
builder.AddHeaderAuthorization<
    MockEntityAuthorizationHandler,
    EntityAuthorizationCollection,
    EntityAuthorizationRequirement>("Entity");
```

Пример заголовка для mock handler:

```http
X-Entity-Key: Mock_2026-06-06T12:00:00Z
```

Работа с `EntityAuthorizationCollection` напрямую:

```csharp
using Titanic.Common.Services.Authorization.Entity;
using Titanic.Common.Session;

var collection = new EntityAuthorizationCollection();

collection.AddAuthorization("local-key", new UserConnection
{
    UserId = Guid.NewGuid(),
    Culture = new UserCulture
    {
        Id = Guid.NewGuid(),
        Name = "ru-RU"
    }
});

var isAuthorized = collection.CheckAuthorization("local-key");
collection.RemoveAuthorization("local-key");
```

Пример совместного запуска с Entity API:

```csharp
using Titanic.Common.Services.Authorization.Entity;
using Titanic.Common.WebApplication;
using Titanic.Db.WebApplication;
using Titanic.Entity.WebApplication;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddTitanicInfrastructure()
    .AddEmptyAuthentication()
    .AddTitanicDb("TitanicDb")
    .AddTitanicEntityApi("TitanicEntity")
    .AddHeaderAuthorization<
        MockEntityAuthorizationHandler,
        EntityAuthorizationCollection,
        EntityAuthorizationRequirement>("Entity");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapTitanicEntityApi();
app.Run();
```

## Зависимости

NuGet и framework dependencies:

- `Microsoft.AspNetCore.App` - framework reference для authentication, authorization и web extension-методов.
- `Swashbuckle.AspNetCore` `10.1.4` - Swagger integration через `AddTitanicInfrastructure()`.

Project references отсутствуют.

## Примечания

- `UserConnection` является обязательным контекстом для `Titanic.Entity`.
- `EntityAuthorizationCollection` - это in-memory cache авторизаций. Внешний источник авторизации в базовой реализации не подключён.
- `MockEntityAuthorizationHandler` предназначен только для тестов и локальной проверки.
- `AddEmptyAuthentication()` не создаёт реального пользователя; он нужен, чтобы ASP.NET authorization pipeline мог работать в отладочных сценариях.
