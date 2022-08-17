using Skender.Stock.Indicators;

namespace Application.Helpers
{
    public static class QuoteRangeExtensions
    {
        public static IEnumerable<IQuote> GetRange(this IEnumerable<IQuote> quotes, decimal size)
        {
            var array = quotes.ToArray();

            if (!array.Any())
                return Array.Empty<IQuote>();

            var range = new List<IQuote>();

            var c = Create(quotes.First());

            range.Add(c);

            for (int i = 1; i < array.Length; i++)
            {
                var q = array[i];

                while (q.High - c.Open > size)
                {
                    c.Close = c.Open + size;
                    c.High = c.Close;

                    c = new()
                    {
                        Date = q.Date,
                        Close = c.Close,
                        High = c.Close,
                        Low = c.Close,
                        Open = c.Close,
                        Volume = 0
                    };

                    range.Add(c);
                }

                while (c.Open - q.Low > size)
                {
                    c.Close = c.Open - size;
                    c.Low = c.Close;

                    c = new()
                    {
                        Date = q.Date,
                        Close = c.Close,
                        High = c.Close,
                        Low = c.Close,
                        Open = c.Close,
                        Volume = 0
                    };

                    range.Add(c);
                }

                if (q.High > c.High)
                    c.High = q.High;

                if (q.Low < c.Low)
                    c.Low = q.Low;

                c.Close = q.Close;
            }

            return range;
        }

        private static CustomQuote Create(IQuote quote)
            => new()
            {
                Date = quote.Date,
                Close = quote.Open,
                High = quote.Open,
                Low = quote.Open,
                Open = quote.Open,
                Volume = 0
            };
    }
}
