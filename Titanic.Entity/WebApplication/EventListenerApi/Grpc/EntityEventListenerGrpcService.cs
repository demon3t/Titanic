using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Titanic.Entity.Events.Grpc
{
    /// <summary>
    /// gRPC-сервис dispatch-вызовов API обработчика событий.
    /// </summary>
    public sealed class EntityEventListenerGrpcService : EntityEventListenerGrpc.EntityEventListenerGrpcBase
    {
        #region Members

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> Dispatch(EntityEventGrpcRequest request, ServerCallContext context)
        {
            var result = EntityEventListenerRequestExecutor.Execute(
                EntityEventListenerRequestExecutor.FromGrpc(request));

            var response = new EntityEventGrpcResponse
            {
                Success = result.Success,
                Canceled = result.Canceled,
                CancelReason = result.CancelReason ?? string.Empty,
                ErrorMessage = result.ErrorMessage ?? string.Empty
            };

            foreach (var value in result.Values)
            {
                response.Values.Add(value.Key, ToGrpcValue(value.Value));
            }

            return Task.FromResult(response);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ToGrpcValue.
        /// </summary>
        private static Value ToGrpcValue(object? value)
        {
            return value switch
            {
                null => Value.ForNull(),
                bool boolValue => Value.ForBool(boolValue),
                string stringValue => Value.ForString(stringValue),
                Guid guidValue => Value.ForString(guidValue.ToString()),
                DateTime dateTimeValue => Value.ForString(dateTimeValue.ToString("O")),
                DateTimeOffset dateTimeOffsetValue => Value.ForString(dateTimeOffsetValue.ToString("O")),
                byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                    => Value.ForNumber(Convert.ToDouble(value)),
                _ => Value.ForString(value.ToString() ?? string.Empty)
            };
        }

        #endregion Members
    }
}
