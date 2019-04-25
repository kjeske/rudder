using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ruddex
{
    public sealed class Store<TState>
    {
        private readonly IEnumerable<IReducer<TState>> _reducers;
        private readonly Func<IEnumerable<ISaga>> _sagas;
        private readonly List<Action<TState>> _subscribers = new List<Action<TState>>();
        private static readonly object Lock = new object();

        public Store(Func<TState> getInitialState, IEnumerable<IReducer<TState>> reducers, Func<IEnumerable<ISaga>> sagas)
        {
            State = getInitialState();
            _reducers = reducers;
            _sagas = sagas;
        }

        public TState State { get; private set; }

        public void Put(object action)
        {
            lock (Lock)
            {
                State = _reducers.Aggregate(State, (state, reducer) => reducer.Handle(state, action));
                NotifyStateSubscribers();
                RunSagas(action);
            }
        }

        public void Subscribe(Action<TState> action) =>
            _subscribers.Add(action);

        public void Unsubscribe(Action<TState> action) =>
            _subscribers.Remove(action);

        private void RunSagas(object action) =>
            Task.Run(async () => await Task.WhenAll(_sagas().Select(saga => saga.OnNext(action))));

        private void NotifyStateSubscribers() =>
            _subscribers.ForEach(subscriber => subscriber(State));
    }
}
