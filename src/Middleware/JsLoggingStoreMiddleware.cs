using Microsoft.JSInterop;

namespace Rudder.Middleware
{
    /// <summary>
    /// Provides JavaScript logging for dispatched actions in a console
    /// </summary>
    public class JsLoggingStoreMiddleware : IStoreMiddleware
    {
        private readonly IJSRuntime _jsRuntime;

        public JsLoggingStoreMiddleware(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task Run<TAction>(TAction action)
        {
            try
            {
                await _jsRuntime.InvokeAsync<object>("console.log", $"%c{GetTypeName(typeof(TAction))}", "font-weight: bold;", action);
            }
            catch
            {
                // ignored
            }
        }

        private static string GetTypeName(Type type) =>
            type.FullName.Substring(type.Namespace.Length + 1).Replace("+", ".");
    }
}