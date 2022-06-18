using Application.Services.Enums;

namespace Application.Objects
{
    public interface IStateManager
    {
        Moviment Side { get; }

        IDictionary<string, object> PrintInformation();
        void Update(State state);
    }
}
