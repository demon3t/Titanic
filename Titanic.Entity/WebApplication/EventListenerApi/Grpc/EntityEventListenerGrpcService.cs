using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Titanic.Entity.Events.Grpc
{
    /// <summary>
    /// gRPC-сервис lifecycle-вызовов API обработчика событий.
    /// </summary>
    public sealed class EntityEventListenerGrpcService : EntityEventListenerGrpc.EntityEventListenerGrpcBase
    {
        #region Members

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> Create(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return Execute(request, EntityEventListenerRequestExecutor.Create);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnSaving(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Saving);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnSaved(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Saved);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnInserting(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Inserting);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnInserted(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Inserted);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnUpdating(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Updating);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnUpdated(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Updated);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnDeleting(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Deleting);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> OnDeleted(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return ExecuteStage(request, EntityEventStage.Deleted);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> Delete(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return Execute(request, EntityEventListenerRequestExecutor.Delete);
        }

        /// <inheritdoc />
        public override Task<EntityEventGrpcResponse> Dispatch(EntityEventGrpcRequest request, ServerCallContext context)
        {
            return Execute(request, EntityEventListenerRequestExecutor.Execute);
        }

        /// <summary>
        /// Выполняет конкретную стадию событийного pipeline.
        /// </summary>
        /// <param name="request">gRPC-запрос события.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>gRPC-ответ listener API.</returns>
        private static Task<EntityEventGrpcResponse> ExecuteStage(EntityEventGrpcRequest request, EntityEventStage stage)
        {
            return Execute(
                request,
                dispatchRequest => EntityEventListenerRequestExecutor.ExecuteStage(dispatchRequest, stage));
        }

        /// <summary>
        /// Выполняет действие listener API и преобразует ответ в gRPC-модель.
        /// </summary>
        /// <param name="request">gRPC-запрос события.</param>
        /// <param name="execute">Действие listener API.</param>
        /// <returns>gRPC-ответ listener API.</returns>
        private static Task<EntityEventGrpcResponse> Execute(
            EntityEventGrpcRequest request,
            Func<EntityEventDispatchRequest, EntityEventDispatchResponse> execute)
        {
            var result = execute(EntityEventListenerRequestExecutor.FromGrpc(request));
            return Task.FromResult(ToGrpcResponse(result));
        }

        /// <summary>
        /// Преобразует transport-ответ listener API в gRPC-модель.
        /// </summary>
        /// <param name="result">Transport-ответ listener API.</param>
        /// <returns>gRPC-ответ listener API.</returns>
        private static EntityEventGrpcResponse ToGrpcResponse(EntityEventDispatchResponse result)
        {
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

            return response;
        }

        /// <summary>
        /// Преобразует CLR-значение в protobuf Value.
        /// </summary>
        /// <param name="value">CLR-значение.</param>
        /// <returns>Protobuf-значение.</returns>
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
