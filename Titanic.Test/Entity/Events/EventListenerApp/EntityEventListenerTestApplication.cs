using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Titanic.Entity.WebApplication;

namespace Titanic.Test.Entity
{
    /// <summary>
    /// Тестовое приложение внешнего listener API, поднимаемое на реальных localhost-портах.
    /// </summary>
    internal sealed class EntityEventListenerTestApplication : IAsyncDisposable
    {
        #region Fields

        private readonly WebApplication _app;

        #endregion Fields

        #region Constructors

        private EntityEventListenerTestApplication(WebApplication app, int httpPort, int grpcPort)
        {
            _app = app;
            HttpBaseAddress = new Uri($"http://127.0.0.1:{httpPort}");
            GrpcBaseAddress = new Uri($"http://127.0.0.1:{grpcPort}");
            GrpcListenerUri = $"grpc://127.0.0.1:{grpcPort}";
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Базовый HTTP-адрес listener API.
        /// </summary>
        public Uri HttpBaseAddress { get; }

        /// <summary>
        /// Базовый HTTP/2-адрес gRPC listener API.
        /// </summary>
        public Uri GrpcBaseAddress { get; }

        /// <summary>
        /// URI gRPC listener API для настройки remote provider-а.
        /// </summary>
        public string GrpcListenerUri { get; }

        #endregion Properties

        #region Members

        /// <summary>
        /// Запускает тестовое listener-приложение с переданной конфигурацией.
        /// </summary>
        /// <param name="configure">Делегат настройки builder-а приложения.</param>
        /// <returns>Запущенное listener-приложение.</returns>
        public static async Task<EntityEventListenerTestApplication> StartAsync(Action<WebApplicationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var httpPort = GetFreeTcpPort();
            var grpcPort = GetFreeTcpPort();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, httpPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
                options.Listen(IPAddress.Loopback, grpcPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            configure(builder);

            var app = builder.Build();
            app.MapTitanicEntityEventListenerApi();
            await app.StartAsync();
            return new EntityEventListenerTestApplication(app, httpPort, grpcPort);
        }

        /// <summary>
        /// Создаёт HTTP-клиент для прямых запросов к listener API.
        /// </summary>
        /// <returns>HTTP-клиент с настроенным базовым адресом.</returns>
        public HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                BaseAddress = HttpBaseAddress
            };
        }

        /// <summary>
        /// Создаёт абсолютный HTTP URI listener endpoint-а.
        /// </summary>
        /// <param name="path">Путь listener endpoint-а.</param>
        /// <returns>Абсолютный HTTP URI.</returns>
        public string GetHttpListenerUri(string path)
        {
            return new Uri(HttpBaseAddress, path.TrimStart('/')).ToString();
        }

        /// <summary>
        /// Создаёт абсолютный WebSocket URI listener endpoint-а.
        /// </summary>
        /// <param name="path">Путь listener endpoint-а.</param>
        /// <returns>Абсолютный WebSocket URI.</returns>
        public string GetWebSocketListenerUri(string path)
        {
            var builder = new UriBuilder(new Uri(HttpBaseAddress, path.TrimStart('/')))
            {
                Scheme = HttpBaseAddress.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
                Port = HttpBaseAddress.Port
            };

            return builder.Uri.ToString();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _app.DisposeAsync();
        }

        /// <summary>
        /// Получает свободный localhost-порт для тестового приложения.
        /// </summary>
        /// <returns>Номер свободного TCP-порта.</returns>
        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        #endregion Members
    }
}
