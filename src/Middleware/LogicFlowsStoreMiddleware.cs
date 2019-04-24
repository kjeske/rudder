using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rudder.Middleware
{
    /// <summary>
    /// Provides logic flows execution
    /// </summary>
    public class LogicFlowsStoreMiddleware : IStoreMiddleware
    {
        private readonly Func<IEnumerable<ILogicFlow>> _flows;

        public LogicFlowsStoreMiddleware(Func<IEnumerable<ILogicFlow>> flows)
        {
            _flows = flows;
        }

        public async Task Run<T>(T action) =>
            await Task.WhenAll(_flows().Select(flow => flow.OnNext(action)));
    }
}
