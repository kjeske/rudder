using Microsoft.AspNetCore.Components;

namespace Rudder
{
    /// <summary>
    /// Component base class with access to the state and actions' dispatcher
    /// </summary>
    /// <typeparam name="TState">Application state type</typeparam>
    /// <typeparam name="TComponentState">Component state type</typeparam>
    public abstract class StateComponent<TState> : ComponentBase, IDisposable
        where TState : class
    {
        private readonly List<Action> _unsubscribeActions = new();

        [Inject]
        private Store<TState> Store { get; set; }

        /// <summary>
        /// Dispatches an action
        /// </summary>
        /// <typeparam name="T">Action type</typeparam>
        /// <param name="action">Action instance</param>
        protected Task PutAsync<T>(T action) => Store.PutAsync(action);

        /// <summary>
        /// Dispatches an action
        /// </summary>
        /// <typeparam name="T">Action type</typeparam>
        /// <param name="action">Action instance</param>
        protected void Put<T>(T action) => Store.Put(action);

        protected void UseState<T>(Func<TState, T> mapper) =>
            _unsubscribeActions.Add(Store.Subscribe(mapper, UpdateState));

        private void UpdateState() =>
            Task.Run(() => InvokeAsync(StateHasChanged));

        public void Dispose() =>
            _unsubscribeActions.ForEach(unsubscribe => unsubscribe());
    }
}