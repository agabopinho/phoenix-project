using Application.Range;
using Application.Services.Providers.Date;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Application.Models;

public class State(IDateProvider dateProvider, ILogger<State> logger)
{
    private ConcurrentBag<ErrorOccurrence> _lastErrors = [];

    public IReadOnlyCollection<Brick> Bricks { get; set; } = [];
    public DateTime? LastTradeTime { get; set; }

    public IEnumerable<ErrorOccurrence> GetErrors() => _lastErrors;

    public void CheckResponseStatus(ResponseType type, ResponseStatus responseStatus)
    {
        if (responseStatus.ResponseCode == Res.SOk)
        {
            return;
        }

        _lastErrors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus));

        logger.LogError("Grpc server error {@data}", new
        {
            ResponseType = type,
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}
