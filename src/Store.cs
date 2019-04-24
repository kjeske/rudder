using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Rudder.Middleware;

namespace Rudder
{
    /// <summary>
    /// Provides the application state and dispatcher for actions
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public sealed class Store<TState> where TState : class
    {
        private readonly IEnumerable<IStateFlow<TState>> _stateFlows;
        private readonly List<IStoreMiddleware> _middlewareList;
        private ImmutableList<Action<TState>> _subscribers = ImmutableList<Action<TState>>.Empty;

        public Store(IEnumerable<IStateFlow<TState>> stateFlows, IEnumerable<IStoreMiddleware> middlewareList)
        {
            _stateFlows = stateFlows;
            _middlewareList = middlewareList.ToList();
        }

        /// <summary>
        /// Application state
        /// </summary>
        public TState State { get; private set; }

        internal async Task Initialize(Func<Task<TState>> func)
        {
            if (State == null)
            {
                State = await func();
            }
        }

        /// <summary>
        /// Actions dispatcher
        /// </summary>
        /// <typeparam name="T">Action type</typeparam>
        /// <param name="action">Action instance</param>
        public async Task Put<T>(T action)
        {
            var newState = _stateFlows.Aggregate(State, (state, stateFlow) => stateFlow.Handle(state, action));

            if (!newState.Equals(State))
            {
                State = newState;
                NotifyStateSubscribers();
            }

            await Task.WhenAll(_middlewareList.Select(middleware => middleware.Run(action)).ToArray());
        }

        /// <summary>
        /// Subscribes for a state change
        /// </summary>
        /// <param name="callback">State change callback</param>
        public void Subscribe(Action<TState> callback) =>
            _subscribers = _subscribers.Add(callback);

        /// <summary>
        /// Unsubscribes from a state change
        /// </summary>
        /// <param name="callback">State change callback</param>
        public void Unsubscribe(Action<TState> callback) =>
            _subscribers = _subscribers.Remove(callback);

        private void NotifyStateSubscribers() =>
            _subscribers.ForEach(subscriber => subscriber(State));
    }
}
