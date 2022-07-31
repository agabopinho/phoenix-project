using Application.Helpers;

namespace Application.Services.Strategies
{
    public interface IStrategy
    {
        int LookbackPeriods { get; }

        double SignalVolume(IEnumerable<CustomQuote> quotes);
    }
}
