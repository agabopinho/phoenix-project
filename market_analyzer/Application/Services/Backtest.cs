using System.Linq;

namespace Application.Services
{
    public record class Transaction(DateTime Time, double Price, double Volume);

    public record class Balance(double OpenVolume, double Profit);

    public record BookPrice(DateTime Time, double Bid, double Ask)
    {
        public static BookPrice Empty => new(DateTime.MinValue, 0, 0);
    }

    public record class BacktestSummary(
       double MinLot = 0,
       double MaxLot = 0,
       double TotalLots = 0,
       double MinVolume = 0,
       double MaxVolume = 0,
       double MinProfit = 0,
       double MaxProfit = 0)
    {
    }

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

            var transactions = _positions.SelectMany(it => it.Transactions);

            Summary = Summary with
            {
                TotalLots = transactions.Sum(it => it.Volume > 0 ? it.Volume : it.Volume * -1)
            };
        }
    }

    public class BacktestPosition
    {
        private readonly List<Transaction> _transactions = new();

        private bool _computed = false;
        private double _volume;
        private double _profit;
        private double _price;

        public IReadOnlyCollection<Transaction> Transactions => _transactions;

        public bool Closed => _transactions.Any() && Volume() == 0;

        public void Add(Transaction transaction)
        {
            if (Closed)
                throw new InvalidOperationException("Position closed.");

            _transactions.Add(transaction);

            if (Closed)
            {
                Volume();
                Price();
                Profit(BookPrice.Empty);

                _computed = true;
            }
        }

        public double Volume()
        {
            if (_computed)
                return _volume;

            _volume = _transactions.Sum(it => it.Volume);
            return _volume;
        }

        public double Profit(BookPrice currentPrice)
        {
            if (_computed)
                return _profit;

            var volume = Volume();
            var closePrice = volume > 0 ? currentPrice.Bid : currentPrice.Ask;

            var close = closePrice * volume * -1;
            var open = _transactions.Sum(it => it.Price * it.Volume);

            _profit = (open + close) * -1;
            return _profit;
        }

        public double Price()
        {
            if (_computed)
                return _price;

            var sells = _transactions.Where(it => it.Volume < 0);
            var sellPrice = Math.Abs(sells.Sum(it => it.Price * it.Volume));
            var sellVolume = Math.Abs(sells.Sum(it => it.Volume));

            var buys = _transactions.Where(it => it.Volume > 0);
            var buyPrice = buys.Sum(it => it.Price * it.Volume);
            var buyVolume = buys.Sum(it => it.Volume);

            if (sellPrice > 0 && buyPrice > 0)
                _price = (sellPrice / sellVolume + buyPrice / buyVolume) / 2;
            else if (sellPrice > 0)
                _price = sellPrice / sellVolume;
            else
                _price = buyPrice / buyVolume;

            return _price;
        }
    }
}
