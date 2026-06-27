using System.Globalization;
using Google.Protobuf;
using Titanic.Common.Session;
using Titanic.Entity.Events.Grpc;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Преобразует общий dispatch-контракт событийного слоя в gRPC-модель и обратно.
    /// </summary>
    internal static class EntityEventGrpcContractMapper
    {
        #region Members

        /// <summary>
        /// Преобразует dispatch-запрос в gRPC-модель.
        /// </summary>
        /// <param name="request">Внутренний dispatch-запрос.</param>
        /// <returns>gRPC-запрос.</returns>
        internal static EntityEventGrpcRequest ToGrpcRequest(EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = new EntityEventGrpcRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
                DispatchId = request.DispatchId,
                Stage = ToGrpcStage(request.Stage),
                IsNew = request.IsNew,
                Entity = ToGrpcSnapshot(request.Entity),
                UserConnection = new EntityEventGrpcUserConnection
                {
                    UserId = request.UserConnection.UserId.ToString(),
                    Culture = new EntityEventGrpcUserCulture
                    {
                        Id = request.UserConnection.Culture.Id.ToString(),
                        Name = request.UserConnection.Culture.Name
                    }
                }
            };

            foreach (var value in request.Values)
            {
                result.Values.Add(value.Key, ToGrpcValue(value.Value));
            }

            foreach (var value in request.OldValues)
            {
                result.OldValues.Add(value.Key, ToGrpcValue(value.Value));
            }

            foreach (var stage in request.Stages)
            {
                result.Stages.Add(ToGrpcStage(stage));
            }

            return result;
        }

        /// <summary>
        /// Преобразует gRPC-запрос во внутренний dispatch-контракт.
        /// </summary>
        /// <param name="request">gRPC-запрос.</param>
        /// <returns>Внутренний dispatch-запрос.</returns>
        internal static EntityEventDispatchRequest FromGrpcRequest(EntityEventGrpcRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new EntityEventDispatchRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
                DispatchId = request.DispatchId,
                Stage = FromGrpcStage(request.Stage),
                Stages = request.Stages
                    .Select(FromGrpcStage)
                    .ToList(),
                IsNew = request.IsNew,
                Entity = FromGrpcSnapshot(request.Entity),
                UserConnection = new UserConnection
                {
                    UserId = Guid.Parse(request.UserConnection.UserId),
                    Culture = new UserCulture
                    {
                        Id = Guid.Parse(request.UserConnection.Culture.Id),
                        Name = request.UserConnection.Culture.Name
                    }
                },
                Values = request.Values.ToDictionary(
                    x => x.Key,
                    x => FromGrpcValue(x.Value),
                    StringComparer.OrdinalIgnoreCase),
                OldValues = request.OldValues.ToDictionary(
                    x => x.Key,
                    x => FromGrpcValue(x.Value),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Преобразует dispatch-ответ во внешний gRPC-контракт.
        /// </summary>
        /// <param name="response">Внутренний dispatch-ответ.</param>
        /// <returns>gRPC-ответ.</returns>
        internal static EntityEventGrpcResponse ToGrpcResponse(EntityEventDispatchResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            var result = new EntityEventGrpcResponse
            {
                Success = response.Success,
                Canceled = response.Canceled,
                CancelReason = response.CancelReason ?? string.Empty,
                ErrorMessage = response.ErrorMessage ?? string.Empty,
                Entity = ToGrpcSnapshot(response.Entity)
            };

            foreach (var value in response.Values)
            {
                result.Values.Add(value.Key, ToGrpcValue(value.Value));
            }

            return result;
        }

        /// <summary>
        /// Преобразует gRPC-ответ во внутренний dispatch-контракт.
        /// </summary>
        /// <param name="response">gRPC-ответ.</param>
        /// <returns>Внутренний dispatch-ответ.</returns>
        internal static EntityEventDispatchResponse FromGrpcResponse(EntityEventGrpcResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            return new EntityEventDispatchResponse
            {
                Success = response.Success,
                Canceled = response.Canceled,
                CancelReason = string.IsNullOrWhiteSpace(response.CancelReason) ? null : response.CancelReason,
                ErrorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage) ? null : response.ErrorMessage,
                Entity = FromGrpcSnapshot(response.Entity),
                Values = response.Values.ToDictionary(
                    x => x.Key,
                    x => FromGrpcValue(x.Value),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Преобразует стадию общего контракта в gRPC enum.
        /// </summary>
        /// <param name="stage">Стадия pipeline.</param>
        /// <returns>Стадия gRPC-контракта.</returns>
        internal static EntityEventGrpcStage ToGrpcStage(EntityEventStage stage)
        {
            return stage switch
            {
                EntityEventStage.Saving => EntityEventGrpcStage.Saving,
                EntityEventStage.Saved => EntityEventGrpcStage.Saved,
                EntityEventStage.Inserting => EntityEventGrpcStage.Inserting,
                EntityEventStage.Inserted => EntityEventGrpcStage.Inserted,
                EntityEventStage.Updating => EntityEventGrpcStage.Updating,
                EntityEventStage.Updated => EntityEventGrpcStage.Updated,
                EntityEventStage.Deleting => EntityEventGrpcStage.Deleting,
                EntityEventStage.Deleted => EntityEventGrpcStage.Deleted,
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported entity event stage.")
            };
        }

        /// <summary>
        /// Преобразует gRPC enum стадии в общий контракт событийного слоя.
        /// </summary>
        /// <param name="stage">Стадия gRPC-контракта.</param>
        /// <returns>Стадия общего dispatch-контракта.</returns>
        internal static EntityEventStage FromGrpcStage(EntityEventGrpcStage stage)
        {
            return stage switch
            {
                EntityEventGrpcStage.Saving => EntityEventStage.Saving,
                EntityEventGrpcStage.Saved => EntityEventStage.Saved,
                EntityEventGrpcStage.Inserting => EntityEventStage.Inserting,
                EntityEventGrpcStage.Inserted => EntityEventStage.Inserted,
                EntityEventGrpcStage.Updating => EntityEventStage.Updating,
                EntityEventGrpcStage.Updated => EntityEventStage.Updated,
                EntityEventGrpcStage.Deleting => EntityEventStage.Deleting,
                EntityEventGrpcStage.Deleted => EntityEventStage.Deleted,
                _ => throw new InvalidOperationException($"Unknown gRPC entity event stage '{stage}'.")
            };
        }

        /// <summary>
        /// Преобразует snapshot Entity в gRPC-модель без промежуточного JSON.
        /// </summary>
        /// <param name="snapshot">Внутренний snapshot Entity.</param>
        /// <returns>gRPC snapshot Entity.</returns>
        internal static EntityEventGrpcEntitySnapshot ToGrpcSnapshot(EntityEventEntitySnapshot? snapshot)
        {
            var result = new EntityEventGrpcEntitySnapshot();
            if (snapshot == null)
            {
                return result;
            }

            result.TableName = snapshot.TableName;
            result.IsNew = snapshot.IsNew;

            foreach (var path in snapshot.Paths)
            {
                result.Paths.Add(path.Key, path.Value);
            }

            foreach (var column in snapshot.Columns)
            {
                result.Columns.Add(column.Key, new EntityEventGrpcColumnSnapshot
                {
                    Alias = column.Value.Alias,
                    DataValueType = column.Value.DataValueType,
                    IsReference = column.Value.IsReference,
                    Value = ToGrpcValue(column.Value.Value),
                    DisplayValue = ToGrpcValue(column.Value.DisplayValue)
                });
            }

            foreach (var oldValue in snapshot.OldValues)
            {
                result.OldValues.Add(oldValue.Key, ToGrpcValue(oldValue.Value));
            }

            return result;
        }

        /// <summary>
        /// Преобразует gRPC snapshot Entity во внутреннюю модель и нормализует её.
        /// </summary>
        /// <param name="snapshot">gRPC snapshot Entity.</param>
        /// <returns>Нормализованный внутренний snapshot Entity.</returns>
        internal static EntityEventEntitySnapshot? FromGrpcSnapshot(EntityEventGrpcEntitySnapshot? snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.TableName))
            {
                return null;
            }

            return EntityEventSnapshotSerializer.Normalize(new EntityEventEntitySnapshot
            {
                TableName = snapshot.TableName,
                IsNew = snapshot.IsNew,
                Paths = snapshot.Paths.ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase),
                Columns = snapshot.Columns.ToDictionary(
                    x => x.Key,
                    x => new EntityEventColumnSnapshot
                    {
                        Alias = x.Value.Alias,
                        DataValueType = x.Value.DataValueType,
                        IsReference = x.Value.IsReference,
                        Value = FromGrpcValue(x.Value.Value),
                        DisplayValue = FromGrpcValue(x.Value.DisplayValue)
                    },
                    StringComparer.OrdinalIgnoreCase),
                OldValues = snapshot.OldValues.ToDictionary(
                    x => x.Key,
                    x => FromGrpcValue(x.Value),
                    StringComparer.OrdinalIgnoreCase)
            });
        }

        /// <summary>
        /// Преобразует CLR-значение в типизированное protobuf-значение.
        /// </summary>
        /// <param name="value">CLR-значение.</param>
        /// <returns>Типизированное protobuf-значение.</returns>
        internal static EntityEventGrpcValue ToGrpcValue(object? value)
        {
            return value switch
            {
                null => new EntityEventGrpcValue { NullValue = true },
                bool boolValue => new EntityEventGrpcValue { BoolValue = boolValue },
                string stringValue => new EntityEventGrpcValue { StringValue = stringValue },
                char charValue => new EntityEventGrpcValue { StringValue = charValue.ToString() },
                byte byteValue => new EntityEventGrpcValue { Int32Value = byteValue },
                sbyte sbyteValue => new EntityEventGrpcValue { Int32Value = sbyteValue },
                short shortValue => new EntityEventGrpcValue { Int32Value = shortValue },
                ushort ushortValue => new EntityEventGrpcValue { Int32Value = ushortValue },
                int intValue => new EntityEventGrpcValue { Int32Value = intValue },
                uint uintValue when uintValue <= int.MaxValue => new EntityEventGrpcValue { Int32Value = (int)uintValue },
                uint uintValue => new EntityEventGrpcValue { Int64Value = uintValue },
                long longValue => new EntityEventGrpcValue { Int64Value = longValue },
                ulong ulongValue when ulongValue <= long.MaxValue => new EntityEventGrpcValue { Int64Value = (long)ulongValue },
                ulong ulongValue => new EntityEventGrpcValue { StringValue = ulongValue.ToString(CultureInfo.InvariantCulture) },
                float floatValue => new EntityEventGrpcValue { DoubleValue = floatValue },
                double doubleValue => new EntityEventGrpcValue { DoubleValue = doubleValue },
                decimal decimalValue => new EntityEventGrpcValue { DecimalValue = decimalValue.ToString(CultureInfo.InvariantCulture) },
                Guid guidValue => new EntityEventGrpcValue { GuidValue = guidValue.ToString() },
                DateTime dateTimeValue => new EntityEventGrpcValue { DateTimeValue = dateTimeValue.ToString("O", CultureInfo.InvariantCulture) },
                DateTimeOffset dateTimeOffsetValue => new EntityEventGrpcValue { DateTimeOffsetValue = dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture) },
                byte[] bytesValue => new EntityEventGrpcValue { BytesValue = ByteString.CopyFrom(bytesValue) },
                _ => new EntityEventGrpcValue { StringValue = value.ToString() ?? string.Empty }
            };
        }

        /// <summary>
        /// Преобразует типизированное protobuf-значение в CLR-значение.
        /// </summary>
        /// <param name="value">Типизированное protobuf-значение.</param>
        /// <returns>CLR-значение.</returns>
        internal static object? FromGrpcValue(EntityEventGrpcValue? value)
        {
            if (value == null)
            {
                return null;
            }

            return value.KindCase switch
            {
                EntityEventGrpcValue.KindOneofCase.NullValue => null,
                EntityEventGrpcValue.KindOneofCase.BoolValue => value.BoolValue,
                EntityEventGrpcValue.KindOneofCase.StringValue => value.StringValue,
                EntityEventGrpcValue.KindOneofCase.Int32Value => value.Int32Value,
                EntityEventGrpcValue.KindOneofCase.Int64Value => value.Int64Value,
                EntityEventGrpcValue.KindOneofCase.DoubleValue => value.DoubleValue,
                EntityEventGrpcValue.KindOneofCase.DecimalValue => decimal.Parse(value.DecimalValue, CultureInfo.InvariantCulture),
                EntityEventGrpcValue.KindOneofCase.GuidValue => Guid.Parse(value.GuidValue),
                EntityEventGrpcValue.KindOneofCase.DateTimeValue => DateTime.Parse(
                    value.DateTimeValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                EntityEventGrpcValue.KindOneofCase.DateTimeOffsetValue => DateTimeOffset.Parse(
                    value.DateTimeOffsetValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                EntityEventGrpcValue.KindOneofCase.BytesValue => value.BytesValue.ToByteArray(),
                _ => null
            };
        }

        #endregion Members
    }
}
