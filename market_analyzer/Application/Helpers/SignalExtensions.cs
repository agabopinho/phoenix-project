using OoplesFinance.StockIndicators.Enums;

namespace Application.Helpers;

public static class SignalExtensions
{
    public static bool IsNone(this Signal signal)
      => signal == Signal.None;

    public static bool IsSignalBuy(this Signal signal)
      => signal is Signal.Buy or Signal.StrongBuy;

    public static bool IsSignalSell(this Signal signal)
        => signal is Signal.Sell or Signal.StrongSell;

    public static bool IsSame(this Signal signal, Signal other)
        => signal.IsSignalBuy() && other.IsSignalBuy() ||
           signal.IsSignalSell() && other.IsSignalSell();
}
