namespace Rudder
{
    /// <summary>
    /// Provides a handler for an action that will change the state accordingly to the processing action
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public interface IStateFlow<TState> where TState : class
    {
        /// <summary>
        /// Handler for an action that will change the state accordingly to the processing action
        /// </summary>
        /// <param name="state">Current application state</param>
        /// <param name="actionValue">Processing action</param>
        /// <returns></returns>
        TState Handle(TState state, object actionValue);
    }
}
