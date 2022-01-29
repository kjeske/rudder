using System.Threading.Tasks;

namespace Rudder
{
    /// <summary>
    /// Provides initial state for the application
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public interface IStateInitializer<TState>
    {
        /// <summary>
        /// Provides initial state for the application
        /// </summary>
        /// <returns>TState instance</returns>
        Task<TState> GetInitialStateAsync();
    }
}
