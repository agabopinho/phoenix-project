namespace Application.Objects
{
    public class MarketDataResult
    {
        public MarketDataResult(string symbol)
        {
            Symbol = symbol.ToUpper();
        }

        public MarketDataResult(string symbol, QuoteInfo info, IEnumerable<CustomQuote> quotes)
        {
            Symbol = symbol.ToUpper();
            SetResult(info, quotes);
        }

        private void SetResult(QuoteInfo info, IEnumerable<CustomQuote> quotes)
        {
            Info = info;
            Quotes = quotes;
            HasResult = true;
        }

        public string Symbol { get; }
        public QuoteInfo? Info { get; private set; }
        public IEnumerable<CustomQuote>? Quotes { get; private set; }
        public bool HasResult { get; private set; } = false;
    }
}
