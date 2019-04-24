using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rudder.Middleware;

namespace Rudder
{
    /// <summary>
    /// Provides the configuration for Rudder library
    /// </summary>
    /// <typeparam name="TState">Application state type</typeparam>
    public interface IRudderOptions<TState> where TState : class
    {
        /// <summary>
        /// Registers a state flow in a container
        /// </summary>
        /// <typeparam name="T">IStateFlow implementation</typeparam>
        IRudderOptions<TState> AddStateFlow<T>() where T : class, IStateFlow<TState>;

        /// <summary>
        /// Registers all state flows from calling assembly and additional assemblies
        /// </summary>
        /// <typeparam name="TState">Application state type</typeparam>
        IRudderOptions<TState> AddStateFlows(params Assembly[] additionalAssemblies);

        /// <summary>
        /// Registers a logic flow in a container
        /// </summary>
        /// <typeparam name="T">ILogicFlow implementation</typeparam>
        IRudderOptions<TState> AddLogicFlow<T>() where T : class, ILogicFlow;

        /// <summary>
        /// Registers all logic flows from calling assembly and additional assemblies
        /// </summary>
        /// <typeparam name="TState">Application state type</typeparam>
        IRudderOptions<TState> AddLogicFlows(params Assembly[] additionalAssemblies);

        /// <summary>
        /// Adds JavaScript's console logging of processed actions (experimental)
        /// </summary>
        IRudderOptions<TState> AddJsLogging();

        /// <summary>
        /// Registers a StoreMiddleware
        /// </summary>
        /// <typeparam name="T">IStoreMiddleware implementation</typeparam>
        IRudderOptions<TState> AddMiddleware<T>() where T : class, IStoreMiddleware;

        IRudderOptions<TState> AddStateInitializer<T>() where T : class, IInitialState<TState>;
    }

    internal class RudderOptions<TState> : IRudderOptions<TState> where TState : class
    {
        private readonly IServiceCollection _services;
        private readonly Assembly _callingAssembly;

        internal RudderOptions(IServiceCollection services, Assembly callingAssembly)
        {
            _services = services;
            _callingAssembly = callingAssembly;
        }

        public IRudderOptions<TState> AddStateInitializer<T>() where T : class, IInitialState<TState>
        {
            _services.AddScoped<IInitialState<TState>, T>();

            return this;
        }

        public IRudderOptions<TState> AddStateFlow<T>() where T : class, IStateFlow<TState>
        {
            _services.AddScoped<IStateFlow<TState>, T>();

            return this;
        }

        public IRudderOptions<TState> AddStateFlows(params Assembly[] additionalAssemblies)
        {
            CombineAssemblies(additionalAssemblies)
                .SelectMany(GetTypesOf<IStateFlow<TState>>)
                .ToList()
                .ForEach(type => _services.AddScoped(typeof(IStateFlow<TState>), type));

            return this;
        }

        public IRudderOptions<TState> AddLogicFlow<T>() where T : class, ILogicFlow
        {
            _services.AddScoped<ILogicFlow, T>();

            return this;
        }

        public IRudderOptions<TState> AddLogicFlows(params Assembly[] additionalAssemblies)
        {
            CombineAssemblies(additionalAssemblies)
                .SelectMany(GetTypesOf<ILogicFlow>)
                .ToList()
                .ForEach(type => _services.AddScoped(typeof(ILogicFlow), type));

            return this;
        }

        public IRudderOptions<TState> AddMiddleware<T>() where T : class, IStoreMiddleware
        {
            _services.AddScoped<IStoreMiddleware, T>();
            return this;
        }

        public IRudderOptions<TState> AddJsLogging()
        {
            AddMiddleware<JsLoggingStoreMiddleware>();
            return this;
        }

        private IEnumerable<Assembly> CombineAssemblies(Assembly[] additionalAssemblies) =>
            new[] { _callingAssembly }.Union(additionalAssemblies).ToList();

        private List<Type> GetTypesOf<T>(Assembly assembly) =>
            assembly
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type))
                .ToList();
    }
}
