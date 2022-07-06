using Application.Services;
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

        public static bool IsSame(this Signal signal, Signal other)
            => signal.IsSignalBuy() && other.IsSignalBuy() ||
               signal.IsSignalSell() && other.IsSignalSell();

        public static bool IsNone(this SignalType signal)
         => signal == SignalType.None;

        public static bool IsSignalBuy(this SignalType signal)
          => signal == SignalType.Buy;

        public static bool IsSignalSell(this SignalType signal)
            => signal == SignalType.Sell;

        public static bool IsSame(this SignalType signal, SignalType other)
            => signal == other;
    }
}
