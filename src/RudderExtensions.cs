using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rudder.Middleware;

namespace Rudder
{
    public static class RudderExtensions
    {
        /// <summary>
        /// Configures Rudder library
        /// </summary>
        /// <typeparam name="TState">Application state type</typeparam>
        /// <param name="services">Services collection</param>
        /// <param name="options">Configuration options</param>
        /// <returns></returns>
        public static IServiceCollection AddRudder<TState>(this IServiceCollection services, Action<IRudderOptions<TState>> options)
            where TState : class
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options(new RudderOptions<TState>(services, Assembly.GetCallingAssembly()));

            return services
                .AddScoped<Func<IEnumerable<ILogicFlow>>>(provider => provider.GetServices<ILogicFlow>)
                .AddScoped<IStoreMiddleware, LogicFlowsStoreMiddleware>()
                .AddScoped<Store<TState>>();
        }
    }
}
