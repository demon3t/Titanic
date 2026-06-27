using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Persistent WebSocket-клиент событийного слоя с последовательным request/response обменом.
    /// </summary>
    public sealed class EntityEventWebSocketClient : IAsyncDisposable
    {
        #region Fields

        /// <summary>
        /// Адрес удалённого WebSocket listener API.
        /// </summary>
        private readonly Uri _listenerUri;

        /// <summary>
        /// Синхронизирует последовательный request/response обмен по одному WebSocket-соединению.
        /// </summary>
        private readonly SemaphoreSlim _syncRoot = new(1, 1);

        /// <summary>
        /// Текущее активное WebSocket-соединение или <see langword="null" />, если его нужно создать заново.
        /// </summary>
        private ClientWebSocket? _socket;

        /// <summary>
        /// Признак того, что клиент уже освобождён и больше не должен принимать новые запросы.
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Создаёт клиент для указанного WebSocket listener API.
        /// </summary>
        /// <param name="listenerUri">URI listener API.</param>
        public EntityEventWebSocketClient(Uri listenerUri)
        {
            ArgumentNullException.ThrowIfNull(listenerUri);

            _listenerUri = listenerUri;
        }

        #endregion Constructors

        #region Members

        /// <summary>
        /// Отправляет transport-запрос и ждёт transport-ответ по тому же WebSocket-соединению.
        /// </summary>
        /// <param name="request">Transport-запрос.</param>
        /// <returns>Transport-ответ listener API.</returns>
        public EntityEventWebSocketResponse Send(EntityEventWebSocketRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return SendAsync(request).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await _syncRoot.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_socket != null)
                {
                    if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        await _socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Disposing event listener client.",
                            CancellationToken.None).ConfigureAwait(false);
                    }

                    _socket.Dispose();
                    _socket = null;
                }
            }
            finally
            {
                _syncRoot.Release();
                _syncRoot.Dispose();
            }
        }

        /// <summary>
        /// Асинхронно отправляет transport-запрос и возвращает transport-ответ.
        /// </summary>
        /// <param name="request">Transport-запрос.</param>
        /// <returns>Transport-ответ listener API.</returns>
        private async Task<EntityEventWebSocketResponse> SendAsync(EntityEventWebSocketRequest request)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _syncRoot.WaitAsync().ConfigureAwait(false);
            try
            {
                return await SendCoreAsync(request, retryOnConnectionFailure: true).ConfigureAwait(false);
            }
            finally
            {
                _syncRoot.Release();
            }
        }

        /// <summary>
        /// Выполняет один request/response цикл по WebSocket с опциональной переинициализацией соединения.
        /// </summary>
        /// <param name="request">Transport-запрос.</param>
        /// <param name="retryOnConnectionFailure">Нужно ли один раз повторить отправку после переподключения.</param>
        /// <returns>Transport-ответ listener API.</returns>
        private async Task<EntityEventWebSocketResponse> SendCoreAsync(
            EntityEventWebSocketRequest request,
            bool retryOnConnectionFailure)
        {
            try
            {
                var socket = await EnsureConnectedAsync().ConfigureAwait(false);
                var payload = JsonSerializer.Serialize(request, EntityEventWebSocketSerializer.JsonOptions);
                await SendTextAsync(socket, payload).ConfigureAwait(false);

                var responsePayload = await ReceiveTextAsync(socket).ConfigureAwait(false);
                var response = JsonSerializer.Deserialize<EntityEventWebSocketResponse>(
                    responsePayload,
                    EntityEventWebSocketSerializer.JsonOptions);

                return response ?? new EntityEventWebSocketResponse
                {
                    Action = request.Action,
                    Response = new EntityEventDispatchResponse
                    {
                        Success = false,
                        ErrorMessage = "WebSocket listener returned an empty response."
                    }
                };
            }
            catch (Exception ex) when (retryOnConnectionFailure && IsReconnectable(ex))
            {
                await ResetSocketAsync().ConfigureAwait(false);
                return await SendCoreAsync(request, retryOnConnectionFailure: false).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Возвращает активное WebSocket-соединение или создаёт новое.
        /// </summary>
        /// <returns>Активное WebSocket-соединение.</returns>
        private async Task<ClientWebSocket> EnsureConnectedAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_socket?.State == WebSocketState.Open)
            {
                return _socket;
            }

            await ResetSocketAsync().ConfigureAwait(false);

            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(_listenerUri, CancellationToken.None).ConfigureAwait(false);
            return _socket;
        }

        /// <summary>
        /// Освобождает текущее сокет-соединение без разрушения клиента.
        /// </summary>
        private async Task ResetSocketAsync()
        {
            if (_socket == null)
            {
                return;
            }

            try
            {
                if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await _socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Resetting event listener connection.",
                        CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
                // Игнорируем ошибку закрытия, потому что цель этого блока — освободить
                // повреждённое соединение и открыть новое на следующей попытке.
            }
            finally
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        /// <summary>
        /// Отправляет одно текстовое сообщение целиком.
        /// </summary>
        /// <param name="socket">Активный сокет.</param>
        /// <param name="payload">Текст transport-сообщения.</param>
        private static Task SendTextAsync(ClientWebSocket socket, string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            return socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
        }

        /// <summary>
        /// Получает одно текстовое сообщение целиком.
        /// </summary>
        /// <param name="socket">Активный сокет.</param>
        /// <returns>Полезная нагрузка transport-сообщения.</returns>
        private static async Task<string> ReceiveTextAsync(ClientWebSocket socket)
        {
            var buffer = new byte[4096];
            using var stream = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new WebSocketException("WebSocket listener closed the connection while reading the response.");
                }

                if (result.Count > 0)
                {
                    stream.Write(buffer, 0, result.Count);
                }

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Определяет, можно ли восстановить соединение повторным подключением.
        /// </summary>
        /// <param name="exception">Исключение transport-уровня.</param>
        /// <returns><see langword="true" />, если есть смысл переподключиться и повторить запрос один раз.</returns>
        private static bool IsReconnectable(Exception exception)
        {
            return exception is WebSocketException
                or IOException
                or OperationCanceledException;
        }

        #endregion Members
    }
}
