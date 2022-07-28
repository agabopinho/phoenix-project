namespace Application.Services
{
    public record class Transaction(DateTime Time, decimal Price, decimal Volume);

    public class Position
    {
        private readonly List<Transaction> _transactions = new();

        public IEnumerable<Transaction> Transactions => _transactions;

        public void Add(Transaction transaction)
        {
            if (_transactions.Any() && BalanceVolume() == 0)
                throw new InvalidOperationException("Position closed.");

            _transactions.Add(transaction);
        }

        public decimal BalanceVolume()
            => _transactions.Sum(it => it.Volume);

        public decimal Profit(decimal closePrice)
        {
            var open = _transactions.Sum(it => it.Price * it.Volume);
            var close = closePrice * BalanceVolume() * -1;

            return (open + close) * -1;
        }

        public decimal OpenPrice()
        {
            var sells = _transactions.Where(it => it.Volume < 0);
            var sellPrice = Math.Abs(sells.Sum(it => it.Price * it.Volume));
            var sellVolume = Math.Abs(sells.Sum(it => it.Volume));

            var buys = _transactions.Where(it => it.Volume > 0);
            var buyPrice = buys.Sum(it => it.Price * it.Volume);
            var buyVolume = buys.Sum(it => it.Volume);

            if (sellPrice > 0 && buyPrice > 0)
                return (sellPrice / sellVolume + buyPrice / buyVolume) / 2;

            if (sellPrice > 0)
                return sellPrice / sellVolume;

            return buyPrice / buyVolume;
        }
    }

    public record class Balance(decimal OpenVolume, decimal OpenPrice, decimal Profit);

    public record BookPrice(decimal Bid, decimal Ask);

    public record class BacktestSummary(
        decimal MinLot = 0,
        decimal MaxLot = 0,
        decimal MinVolume = 0,
        decimal MaxVolume = 0,
        decimal MinProfit = 0,
        decimal MaxProfit = 0);

    public class Backtest
    {

        private readonly List<Position> _positions = new();

        public IEnumerable<Position> Positions => _positions;

        public BacktestSummary Summary { get; private set; } = new();

        public void Add(Transaction transaction)
        {
            var current = OpenPosition();

            if (current is null)
                _positions.Add(current = new Position());

            current.Add(transaction);
        }

        public Position? OpenPosition()
            => _positions.FirstOrDefault(it => it.BalanceVolume() != 0);

        public Balance Balance(BookPrice lastPrice)
        {
            var openVolume = 0M;
            var openPrice = 0M;
            var profit = 0M;

            foreach (var position in _positions)
            {
                var balanceVolume = position.BalanceVolume();
                var closePrice = 0M;

                if (balanceVolume != 0)
                {
                    openPrice = position.OpenPrice();
                    closePrice = balanceVolume > 0 ? lastPrice.Bid : lastPrice.Ask;
                }

                profit += position.Profit(closePrice);
                openVolume += balanceVolume;
            }

            var balance = new Balance(openVolume, openPrice, profit);

            UpdateSummary(balance);

            return balance;
        }

        private void UpdateSummary(Balance balance)
        {
            if (balance.OpenVolume < Summary.MinVolume)
                Summary = Summary with { MinVolume = balance.OpenVolume };

            if (balance.OpenVolume > Summary.MaxVolume)
                Summary = Summary with { MaxVolume = balance.OpenVolume };

            if (balance.Profit < Summary.MinProfit)
                Summary = Summary with { MinProfit = balance.Profit };

            if (balance.Profit > Summary.MaxProfit)
                Summary = Summary with { MaxProfit = balance.Profit };

            var maxLot = _positions.Max(it => it.Transactions.Max(t => t.Volume));
            var minLot = _positions.Min(it => it.Transactions.Min(t => t.Volume));

            if (minLot < Summary.MinLot)
                Summary = Summary with { MinLot = minLot };

            if (maxLot > Summary.MaxLot)
                Summary = Summary with { MaxLot = maxLot };
        }
    }
}
