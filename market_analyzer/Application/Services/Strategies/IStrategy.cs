using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public interface IStrategy
    {
        int LookbackPeriods { get; }

        double SignalVolume(IEnumerable<IQuote> quotes);

        public interface IWithPosition : IStrategy
        {
            StrategyPosition? Position { set; }
        }
    }

    public record class StrategyPosition(double Price, double Volume, double Profit);
}
