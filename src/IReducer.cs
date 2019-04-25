namespace Ruddex
{
    public interface IReducer<TState>
    {
        TState Handle(TState appState, object actionValue);
    }
}
