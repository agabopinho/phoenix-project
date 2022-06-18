using Application.Services.Enums;

namespace Application.Objects
{
    public class State
    {
        public State(DateTime updateAt, double close)
        {
            UpdateAt = updateAt;
            Close = close;
        }

        public DateTime UpdateAt { get; private set; }
        public double Close { get; private set; }
        public Moviment? Moviment { get; private set; }

        public void SetMoviment(Moviment moviment) => Moviment = moviment;
    }
}
