using Application.Range;
using Application.Services.Providers.Date;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Application.Models;

public class State(IDateProvider dateProvider, ILogger<State> logger)
{
    public ConcurrentBag<ErrorOccurrence> LastErrors { get; } = [];
    public IReadOnlyCollection<Brick> Bricks { get; set; } = Array.Empty<Brick>();
    public DateTime? LastTradeTime { get; set; }

    public void CheckResponseStatus(ResponseType type, ResponseStatus responseStatus)
    {
        if (responseStatus.ResponseCode == Res.SOk)
        {
            return;
        }

        LastErrors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus));

        logger.LogError("Grpc server error {@data}", new
        {
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}
