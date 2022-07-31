using Application.Helpers;
using Application.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Strategies
{
    public class MiniBovespa : IStrategy
    {
        private readonly OperationSettings _operationSettings;

        public MiniBovespa(OperationSettings operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 0;

        public double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            return 0;
        }
    }
}
