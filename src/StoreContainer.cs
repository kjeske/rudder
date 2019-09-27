using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Rudder
{
    /// <summary>
    /// Component used for store initialization
    /// </summary>
    /// <typeparam name="TState">State type</typeparam>
    public class StoreContainer<TState> : IComponent, IHandleAfterRender where TState : class
    {
        private RenderHandle _renderHandle;

        [Inject]
        private Store<TState> Store { get; set; }

        [Inject]
        private IInitialState<TState> InitialState { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        void IComponent.Attach(RenderHandle renderHandle)
        {
            if (!_renderHandle.IsInitialized)
            {
                _renderHandle = renderHandle;
            }
        }

        async Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            await Store.Initialize(InitialState.GetInitialState);
            _renderHandle.Render(builder => builder.AddContent(0, ChildContent));
        }

        Task IHandleAfterRender.OnAfterRenderAsync() =>
            Store.Put(new StateInitialized());
    }
}
