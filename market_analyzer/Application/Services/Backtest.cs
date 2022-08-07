namespace Application.Services
{
    public record class Transaction(DateTime Time, double Price, double Volume);
    
    public record class Balance(double OpenVolume, double Profit);

    public record BookPrice(DateTime Time, double Bid, double Ask);

    public record class BacktestSummary(
       double MinLot = 0,
       double MaxLot = 0,
       double MinVolume = 0,
       double MaxVolume = 0,
       double MinProfit = 0,
       double MaxProfit = 0);

    public class Backtest
    {
        private readonly List<BacktestPosition> _positions = new();

        public IEnumerable<BacktestPosition> Positions => _positions;

        public BacktestSummary Summary { get; private set; } = new();

        public Transaction Execute(BookPrice currentPrice, double volume)
        {
            var current = OpenPosition();

            if (current is null)
                _positions.Add(current = new BacktestPosition());

            var price = volume > 0 ? currentPrice.Ask : currentPrice.Bid;
            var transaction = new Transaction(currentPrice.Time, price, volume);

            current.Add(transaction);

            return transaction;
        }

        public BacktestPosition? OpenPosition()
            => _positions.FirstOrDefault(it => it.Volume() != 0);

        public Balance Balance(BookPrice currentPrice)
        {
            var openVolume = 0d;
            var sumProfit = 0d;

            foreach (var position in _positions)
            {
                openVolume += position.Volume();
                sumProfit += position.Profit(currentPrice);
            }

            var balance = new Balance(openVolume, sumProfit);

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

            var maxLot = _positions.Any() ? _positions.Max(it => it.Transactions.Max(t => t.Volume)) : 0;
            var minLot = _positions.Any() ? _positions.Min(it => it.Transactions.Min(t => t.Volume)) : 0;

            if (minLot < Summary.MinLot)
                Summary = Summary with { MinLot = minLot };

            if (maxLot > Summary.MaxLot)
                Summary = Summary with { MaxLot = maxLot };
        }
    }

    public class BacktestPosition
    {
        private readonly List<Transaction> _transactions = new();

        public IEnumerable<Transaction> Transactions => _transactions;

        public void Add(Transaction transaction)
        {
            if (_transactions.Any() && Volume() == 0)
                throw new InvalidOperationException("Position closed.");

            _transactions.Add(transaction);
        }

        public double Volume()
            => _transactions.Sum(it => it.Volume);

        public double Profit(BookPrice currentPrice)
        {
            var volume = Volume();
            var closePrice = volume > 0 ? currentPrice.Bid : currentPrice.Ask;

            var close = closePrice * volume * -1;
            var open = _transactions.Sum(it => it.Price * it.Volume);

            return (open + close) * -1;
        }

        public double Price()
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
}
