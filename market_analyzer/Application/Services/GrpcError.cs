using Grpc.Terminal.Enums;

namespace Application.Services;

public record class GrpcError(DateTime Time, ResponseType Type, ResponseStatus Status);
