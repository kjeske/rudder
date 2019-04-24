using System.Diagnostics.Contracts;

namespace Rudder
{
    /// <summary>
    /// Provides a mapper of application state to the component state
    /// </summary>
    /// <typeparam name="TState">Application state</typeparam>
    /// <typeparam name="TComponentState">ComponentState</typeparam>
    public interface IStateMap<in TState, out TComponentState>
        where TComponentState : struct
    {
        /// <summary>
        /// Maps the application state to the component state
        /// </summary>
        /// <param name="state">Application state</param>
        /// <returns>Mapped component state</returns>
        [Pure]
        TComponentState MapState(TState state);
    }
}
