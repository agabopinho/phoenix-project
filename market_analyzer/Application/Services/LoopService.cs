using Application.Constants;
using Application.Objects;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;

namespace Application.Services
{
    public class LoopService : ILoopService
    {
        private readonly IQuoteService _ratesService;
        private readonly ILogger<LoopService> _logger;

        public LoopService(IQuoteService ratesService, ILogger<LoopService> logger)
        {
            _ratesService = ratesService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var marketDataResult = await _ratesService.GetQuotesAsync(Defaults.Symbol, cancellationToken);

            if (!marketDataResult.HasResult)
            {
                _logger.LogWarning("Market data did not produce result.");

                return;
            }

            var r = marketDataResult.Quotes!.Last();
            _logger.LogInformation("{@data}", new { r.Date, r.Close, Rates = true });

            var renkos = marketDataResult.Quotes!.GetRenko(25);

            var k = renkos.OrderBy(it => it.Date).Last();
            _logger.LogInformation("{@data}", new { k.Date, k });

            return;
        }
    }
}
