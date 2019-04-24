using System.Threading.Tasks;

namespace Rudder
{
    /// <summary>
    /// Provides handler for an action that will execute the business logic and dispatch further action if needed
    /// </summary>
    public interface ILogicFlow
    {
        /// <summary>
        /// Handler for in incoming action for executing the business logic
        /// </summary>
        /// <param name="action">Action that is being currently processed</param>
        Task OnNext(object action);
    }
}
