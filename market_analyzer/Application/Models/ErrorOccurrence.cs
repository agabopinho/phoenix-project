using Grpc.Terminal.Enums;

namespace Application.Models;

public record class ErrorOccurrence(DateTime Time, ResponseType Type, ResponseStatus Status, string? Comment = null);
