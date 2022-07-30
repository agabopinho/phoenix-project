using Application.Helpers;

namespace Application.Services.Strategies
{
    public interface IStrategy
    {
        int LookbackPeriods { get; }

        decimal SignalVolume(IEnumerable<CustomQuote> quotes);
    }
}
