using System.Runtime.Serialization;

namespace Application.Services.Providers.Cycle
{
    public class BacktestFinishException : Exception
    {
        public BacktestFinishException()
        {
        }

        public BacktestFinishException(string? message) : base(message)
        {
        }

        public BacktestFinishException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BacktestFinishException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
