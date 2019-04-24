using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Ruddex
{
    public class StoreProvider<T> : ComponentBase, IDisposable
    {
        [Parameter]
        private Store<T> Store { get; set; }

        [Parameter]
        private RenderFragment ChildContent { get; set; }

        public void Dispose() => Store.Unsubscribe(UpdateState);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);

            builder.OpenComponent<CascadingValue<Store<T>>>(0);
            builder.AddAttribute(1, "Name", "Store");
            builder.AddAttribute(1, "Value", Store);
            builder.AddAttribute(7, "ChildContent", ChildContent);
            builder.CloseComponent();
        }

        protected override void OnInit()
        {
            Store.Subscribe(UpdateState);
            Store.Put(new AppInit());
        }

        private void UpdateState(T state) => Invoke(StateHasChanged);
    }
}
