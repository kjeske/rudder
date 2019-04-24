using System.Threading.Tasks;

namespace Rudder
{
    /// <summary>
    /// Encapsulates a method that has a single parameter and returns a Task.
    /// </summary>
    /// <typeparam name="T1">Parameter type</typeparam>
    /// <param name="arg1">Parameter value</param>
    public delegate Task AsyncAction<T1>(T1 arg1);
}
