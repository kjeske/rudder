using System.Diagnostics;
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
        private readonly List<Subscriber> _subscribers = new();
        private TState? _state;
        private readonly object _lockObject = new();

        public Store(IEnumerable<IStateFlow<TState>> stateFlows, IEnumerable<IStoreMiddleware> middlewareList)
        {
            _stateFlows = stateFlows;
            _middlewareList = middlewareList.ToList();
        }

        /// <summary>
        /// Application state
        /// </summary>
        public TState State => _state!;

        internal async Task Initialize(Func<Task<TState>> func) =>
            _state ??= await func();

        /// <summary>
        /// Actions dispatcher
        /// </summary>
        /// <typeparam name="T">Action type</typeparam>
        /// <param name="action">Action instance</param>
        public async Task PutAsync<T>(T action)
        {
            lock (_lockObject)
            {
                var newState = _stateFlows.Aggregate(State, (state, stateFlow) => stateFlow.Handle(state, action!));

                if (!newState.Equals(State))
                {
                    _state = newState;
                    NotifyStateSubscribers();
                }
            }

            await Task.WhenAll(_middlewareList.Select(middleware => middleware.Run(action)).ToArray());
        }

        /// <summary>
        /// Actions dispatcher
        /// </summary>
        /// <typeparam name="T">Action type</typeparam>
        /// <param name="action">Action instance</param>
        public void Put<T>(T action) =>
            Task.Run(() => PutAsync(action));

        /// <summary>
        /// Subscribes for a state change
        /// </summary>
        /// <param name="mapper">State change callback</param>
        /// <param name="notify">Notification of state changes</param>
        public Action Subscribe<T>(Func<TState, T> mapper, Action notify)
        {
            var lastState = mapper(State);

            var subscriber = new Subscriber(
                oldValue: lastState,
                notify: notify,
                mapState: state => mapper(state)
            );

            _subscribers.Add(subscriber);

            return () => _subscribers.Remove(subscriber);
        }

        private void NotifyStateSubscribers()
        {
            var notifiers = new List<Action>();

            foreach (var subscriber in _subscribers)
            {
                var newValue = subscriber.MapState(State);
                if (newValue is null && subscriber.OldValue is null || newValue is not null && newValue.Equals(subscriber.OldValue))
                {
                    continue;
                }

                subscriber.OldValue = newValue;
                notifiers.Add(subscriber.Notify);
            }

            notifiers.Distinct().ToList().ForEach(refresh => refresh());
        }

        private class Subscriber
        {
            public Subscriber(object? oldValue, Action notify, Func<TState, object?> mapState)
            {
                OldValue = oldValue;
                Notify = notify;
                MapState = mapState;
            }

            public Action Notify { get; init; }
            public Func<TState, object?> MapState { get; init; }
            public object? OldValue { get; set; }
        }
    }
}