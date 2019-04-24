using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Rudder
{
    /// <summary>
    /// Component base class with access to the state and actions' dispatcher
    /// </summary>
    /// <typeparam name="TState">Application state type</typeparam>
    /// <typeparam name="TComponentState">Component state type</typeparam>
    public abstract class StateComponent<TState, TComponentState> : ComponentBase, IDisposable
        where TState : class
        where TComponentState : struct, IStateMap<TState, TComponentState>
    {
        private static readonly TComponentState StateMapper = new TComponentState();

        private TComponentState _state;

        [Inject]
        private Store<TState> Store { get; set; }

        /// <summary>
        /// Component state
        /// </summary>
        protected ref TComponentState State => ref _state;

        /// <summary>
        /// Dispatches an action
        /// </summary>
        /// <typeparam name="T">Action type</typeparam>
        /// <param name="action">Action instance</param>
        protected Task Put<T>(T action) => Store.Put(action);

        protected override void OnInit()
        {
            _state = StateMapper.MapState(Store.State);
            Store.Subscribe(UpdateState);
        }

        private void UpdateState(TState state)
        {
            var newState = StateMapper.MapState(state);

            if (!newState.Equals(State))
            {
                _state = newState;
                Invoke(StateHasChanged);
            }
        }

        void IDisposable.Dispose() => Store.Unsubscribe(UpdateState);
    }
}
