namespace Application.Objects
{
    public class MarketDataResult
    {
        public MarketDataResult(string symbol)
        {
            Symbol = symbol.ToUpper();
        }

        public MarketDataResult(string symbol, RatesInfo ratesInfo, IEnumerable<Rate> rates)
        {
            Symbol = symbol.ToUpper();
            SetResult(ratesInfo, rates);
        }

        private void SetResult(RatesInfo ratesInfo, IEnumerable<Rate> rates)
        {
            RatesInfo = ratesInfo;
            Rates = rates;
            HasResult = true;
        }

        public string Symbol { get; }
        public RatesInfo? RatesInfo { get; private set; }
        public IEnumerable<Rate>? Rates { get; private set; }
        public bool HasResult { get; private set; } = false;
    }
}
