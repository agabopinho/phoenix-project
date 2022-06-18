using Application.Services.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Objects
{
    public class StateManager : IStateManager
    {
        private readonly Dictionary<DateTime, State> _states = new();
        private readonly TimeSpan _slidingTime;
        private readonly ILogger<StateManager> _logger;

        public StateManager(TimeSpan slidingTime, ILogger<StateManager> logger)
        {
            _slidingTime = slidingTime;
            _logger = logger;
        }

        public IReadOnlyDictionary<DateTime, State> States => _states;

        public long CountUp => _states.Count(it => it.Value.Moviment.Equals(Moviment.Up));

        public long CountDown => _states.Count(it => it.Value.Moviment.Equals(Moviment.Down));

        public long Absolute => Math.Abs(CountUp - CountDown);

        public Moviment Side =>
            CountUp.Equals(CountDown) ?
                Moviment.Idle :
                CountUp > CountDown ? Moviment.Up : Moviment.Down;

        public IDictionary<string, object> PrintInformation()
            => new Dictionary<string, object>
            {
                {  "LastUpdateAt", States.Last().Value.UpdateAt },
                {  "CountUp", CountUp },
                {  "CountDown", CountDown},
                { "Absolute", Absolute },
                { "Side", Side }
            };

        public void Update(State state)
        {
            if (!_states.Any())
            {
                state.SetMoviment(Moviment.Idle);

                AddState(state);

                return;
            }

            if (_states.ContainsKey(state.UpdateAt))
            {
                _logger.LogInformation("State already exists.");

                return;
            }

            var moviment = GetMoviment(state);

            if (!moviment.Equals(Moviment.Idle))
            {
                state.SetMoviment(moviment);

                AddState(state);
                Sliding();
            }
        }

        private Moviment GetMoviment(State state)
        {
            var lastClose = _states.Last().Value.Close;

            if (state.Close.Equals(lastClose))
                return Moviment.Idle;

            return state.Close > lastClose ? Moviment.Up : Moviment.Down;
        }

        private void AddState(State state)
        {
            if (state.Moviment is null)
                throw new InvalidOperationException();

            _states.Add(state.UpdateAt, state);
        }

        private void Sliding()
        {
            var outOfPeriod = _states.Last().Value.UpdateAt - _slidingTime;
            foreach (var key in _states.Keys.Where(key => key < outOfPeriod).ToArray())
                _states.Remove(key);
        }
    }
}
