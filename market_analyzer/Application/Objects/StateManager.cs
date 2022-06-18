using Application.Services.Enums;

namespace Application.Objects
{
    public class StateManager
    {
        private readonly Dictionary<DateTime, State> _states = new();

        public StateManager()
        {
        }

        public IReadOnlyDictionary<DateTime, State> States => _states;

        public long CountUp => _states.Count(it => it.Value.Moviment.Equals(Moviment.Up));

        public long CountDown => _states.Count(it => it.Value.Moviment.Equals(Moviment.Down));

        public long Absolute => Math.Abs(CountUp - CountDown);

        public Moviment Side =>
            CountUp.Equals(CountDown) ?
                Moviment.Idle :
                CountUp > CountDown ? Moviment.Up : Moviment.Down;

        public void Update(State state)
        {
            if (!_states.Any())
            {
                state.SetMoviment(Moviment.Idle);

                AddState(state);

                return;
            }

            if (_states.ContainsKey(state.UpdateAt))
                return;

            var moviment = GetMoviment(state);

            if (!moviment.Equals(Moviment.Idle))
            {
                state.SetMoviment(moviment);

                AddState(state);
            }
        }

        private void AddState(State state)
        {
            if (state.Moviment is null)
                throw new InvalidOperationException();

            _states.Add(state.UpdateAt, state);
        }

        private Moviment GetMoviment(State state)
        {
            var lastClose = _states.Last().Value.Close;

            if (state.Close.Equals(lastClose))
                return Moviment.Idle;

            return state.Close > lastClose ? Moviment.Up : Moviment.Down;
        }
    }
}
