namespace Application.Services
{
    public record class Transaction(DateTime Time, decimal Price, decimal Volume);

    public class Position
    {
        private readonly List<Transaction> _transactions = new();

        public IEnumerable<Transaction> Transactions => _transactions.ToArray();

        public void Add(Transaction transaction)
            => _transactions.Add(transaction);

        public decimal Volume()
            => _transactions.Sum(it => it.Volume);

        public decimal Profit()
            => _transactions.Sum(it => it.Price * it.Volume) * -1;

        public decimal Profit(decimal marketPrice)
        {
            var open = _transactions.Sum(it => it.Price * it.Volume);
            var close = marketPrice * Volume() * -1;

            return (open + close) * -1;
        }

        public decimal Price()
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
