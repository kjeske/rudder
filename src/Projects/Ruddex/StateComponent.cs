using Microsoft.AspNetCore.Components;

namespace Ruddex
{
    public abstract class StateComponent<TState> : ComponentBase
    {
        protected void Put(object action) => Store.Put(action);

        protected TState State => Store.State;

        [CascadingParameter(Name = "Store")]
        private Store<TState> Store { get; set; }
    }
}
