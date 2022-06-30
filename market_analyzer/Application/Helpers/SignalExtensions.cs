using OoplesFinance.StockIndicators.Enums;

namespace Application.Helpers
{
    public static class SignalExtensions
    {
        public static bool IsNone(this Signal signal)
          => signal == Signal.None;

        public static bool IsSignalBuy(this Signal signal)
          => signal == Signal.Buy || signal == Signal.StrongBuy;

        public static bool IsSignalSell(this Signal signal)
            => signal == Signal.Sell || signal == Signal.StrongSell;
    }
}
