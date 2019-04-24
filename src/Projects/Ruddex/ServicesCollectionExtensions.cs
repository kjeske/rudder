using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Ruddex
{
    public static class ServicesCollectionExtensions
    {
        public static IServiceCollection AddRazorState<TState>(this IServiceCollection services, Func<TState> getInitialState, Type[] reducers, Type[] sagas)
        {
            sagas.ToList().ForEach(saga => services.AddScoped(typeof(ISaga), saga));
            reducers.ToList().ForEach(reducer => services.AddScoped(typeof(IReducer<TState>), reducer));
            services.AddScoped<Func<IEnumerable<ISaga>>>(provider => provider.GetServices<ISaga>);
            services.AddScoped<Store<TState>>();
            services.AddScoped(provider => getInitialState);
            return services;
        }
    }
}
