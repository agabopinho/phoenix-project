using Grpc.Terminal.Enums;

namespace Application.Services;

public record class TerminalError(DateTime Time, ResponseType Type, ResponseStatus Status);
