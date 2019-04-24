# Rudder
Rudder is a state container for server-side Blazor. It's still in a preview, same as .NET Core 3 which is the required framework version.

The main features of Rudder:
* One state object per whole application
* The interactions from the user interface dispatch actions
* Changes to the main app state are done only in state flows. State flows are responsible for taking the dispatched actions and depending of the type and data of those actions changing the state accordingly.
* Business logic is triggered in logic flows. Logic flows are responsible for taking the dispatched actions, executing business logic and optionally dispatching another actions during that process.

# Purpose
Rudder makes it easier to maintain the application state and user interactions. Rudder brings:
* Clean separation of concerns. State flows change the state, logic flows handle the side effects, components render the view and dispatch actions.
* Easier testing
* Global state makes it easier to serialize and persist in the storage for recovery purposes
* Views don't contain complex logic and are responsible mostly for rendering and dispatching simple actions objects

# How it works
1. There is one state object that represents the whole application UI state in all the modules that are available for the user.
1. Application state is wrapped in a `Store` class that apart from keeping the state is also responsible for dispatching the actions. Actions are objects that describe events together with corresponding event data.
1. Views are rendered using the Blazor components. In Rudder we can distinguish two types of components:
   - Components that have access to the `Store` - called StateComponents, equivalent of "container components".
   - Components without the access  to the `Store` - just regular components, equivalent of "presentational components".
1. User interactions, like `onclick`, are handled in components and then can be transformed into appropriate action objects that describe the user intention (for example ReloadList action) and then dispatched.
1. Every dispatched action triggers:
   * State flows that are responsible for changing the state accordingly to the processing action. Example:
     - If currently processing action is `Actions.GetItems.Request`, then set `IsFetching` state property to `true`.
   * Logic flows that are responsible for handling the business logic and dispatching further actions during that process. Example:
     - If currently processing action is `Actions.GetItems.Invoke` then:
       - Dispatch `Actions.GetItems.Request` action
       - Load items from the database
       - Dispatch `Actions.GetItems.Success` action with items data if the request was successful.
       - Dispatch `Actions.GetItems.Failure` action with error message if the request was not successful.
1. Every application state change triggers a call to each visible `StateComponent` in order to check if the state that they use has changed as well (components use only a part the application state). If so, such component will be rerendered.

# Sample application

Sample application can be found here: https://github.com/kjeske/rudder-example

# Getting started with new application

Rudder works with applications using Server-Side Blazor, which is a part of ASP<span></span>.NET Core in .NET Core 3.0 Preview 5+

## Installation
Add the Rudder NuGet package to your application.

Command line:
```
dotnet add package Rudder --version 1.0.0-*
```
Package Manager Console:
```
Install-Package Rudder -IncludePrerelease
```

## AppState
We will start with creating first draft of our application's state in a class. It will be used to keep the data used by the UI.

```C#
public class AppState
{
    public ImmutableList<string> Items { get; set; }

    public bool IsFetching { get; set; }
}
```

Remarks:
* ImmutableList is recommended when working with lists in the application state as it gives many advantages of immutable operations and help with doing state changes without mutating the original object.

## Initial state
We need to define our initial shape of application state. It can be used to prepare the state basing on currently logged in user and data from the database.

```C#
public class InitialState : IInitialState<AppState>
{
    public Task<AppState> GetInitialState()
    {
        var state = new AppState
        {
            Items = new[] { "Item 1" }.ToImmutableList()
        };

        return Task.FromResult(state);
    }
}

```

## App.razor
In the Blazor app entry point (usually app.razor component) we need to wrap existing logic with StoreContainer component in order to initialize the store.

```razor
<StoreContainer TState="AppState">
    <Router AppAssembly="typeof(Startup).Assembly" />
</StoreContainer>

```

## Actions

Actions describe events that float thorough the system and are handled by state flows and logic flows. State flows change the state and logic flows handle the side effects and are mainly responsible for business logic.

Actions are defined by custom classes or structs which can pass data when needed. The structure of the actions is arbitrary.

```C#
public static class Actions
{
    public static class LoadItems
    {
        public struct Invoke { }

        public struct Request { }

        public struct Success
        {
            public string[] Items { get; set; }
        }

        public struct Failure
        {
            public string ErrorMessage { get; set; }
        }
    }
}
```
Remarks:
* `LoadItems.Invoke` action is to trigger fetching the items from some storage
* `LoadItems.Request` action is to inform that the request to get the data has started
* `LoadItems.Success` action is to inform that getting the data is finished. It contains Items property with retrieved data

## First custom component

Let's create a custom component with access to the `Store` and name it Items.razor. It will show the elements from our state and have a button to load those elements. When the elements are being fetched, the loading indication should be shown.

We will start with creating the `Items.razor` component:

```razor
@inherits StateComponent<AppState, ItemsState>

@functions {
    Task LoadItems() => Put(new Actions.LoadItems.Invoke());
}

<h1>App</h1>

@if (State.IsFetching)
{
    <p>Is loading</p>
}
else
{
    foreach (var item in State.Items)
    {
        <p>item</p>
    }

    <button onclick=@LoadItems>Load items</button>
}
```

Next we need to declare a part of our `AppState` that will be used by this component. We will map `AppState` to `ItemsState` so we will have access to only needed properties. Our component state has to be a `struct` in order to enable shallow comparison for rerendering purposes.

> In such a small application like this example a reason for narrowing down the component state might not be visible, but in bigger applications with nice structure the components are small and use only a small part of the main state.

ItemsState.cs:
```C#
public struct ItemsState : IStateMap<AppState, ItemsState>
{
    public ImmutableList<string> Items { get; set; }

    public bool IsFetching { get; set; }

    public ItemsState Map(AppState state) => new ItemsState
    {
        Items = state.Items,
        IsFetching = state.IsFetching
    }
}
```

Remarks:
* Our component inherits from `StateComponent<AppState, ItemsState>`. It's needed in "container" components that have direct access to component state or dispatch actions.
* Having `StateComponent<AppState, ItemsState>` as a base class gives us access to:
    - `State` property - our narrowed down component state
    - `Put` method - method used to dispatch actions
* The `LoadItems` method is attached to the `Load items` button. Its role is to dispatch an instance of the Actions.LoadItems.Invoke action.
* `ItemsState.Map` method is called whenever `AppState` changes. Component will be rerendered when the result of that state mapping is different from the previous mapping. Map method has to be [pure](https://en.wikipedia.org/wiki/Pure_function).

## State flows

State flows are classes providing functionality to change the state accordingly to the coming actions.

```C#
public class ItemsStateFlow : IStateFlow<AppState>
{
    public AppState Handle(AppState appState, object actionValue)
    {
        switch (actionValue)
        {
            case Actions.LoadItems.Request _:
                return appState.With(state => {
                    state.IsFetching = true;
                });

            case Actions.LoadItems.Success action:
                return appState.With(state => {
                    state.IsFetching = false;
                    state.Items = action.Items.ToImmutableList();
                });

            default:
                return appState;
        }
    }
}
```
Remarks:
* State flow has to implement IStateFlow<AppState> where AppState is our application state object type
* Whenever action is dispatched in the system, the `Handle` method in all the state flows will be called
* The `Handle` method is responsible for reacting on the actions and changing the state where needed
* The appState parameter value should be treated as read-only and shouldn't be mutated. Any intent to change the state should create a new copy of the state with that change. Extension method `With` is introduced in order to simplify copying the objects. It performs a shallow copy of an object.
* There can be many state flows, each responsible for its own area

## Logic Flows

Logic flows are used to run the side-effects and dispatch actions during that process. It's the right place to communicate with services, databases or external services.

```C#
public class ItemsLogicFlow : ILogicFlow
{
    private readonly Store<AppState> _store;
    private readonly IAppService _appService;

    public ItemsFlow(Store<AppState> store, IAppService appService)
    {
        _store = store;
        _appService = appService;
    }

    public async Task OnNext(object actionValue)
    {
        switch (actionValue)
        {
            case Actions.LoadItems.Invoke _:
                await LoadItems();
                break;
        }
    }

    private async Task LoadItems()
    {
        await _store.Put(new Actions.LoadItems.Request());

        try
        {
            var result = await _appService.GetItems();
            await _store.Put(new Actions.LoadItems.Success { Items = result.ToArray() });
        }
        catch (Exception exception)
        {
            await _store.Put(new Actions.LoadItems.Failure { ErrorMessage = exception.Message });
        }
    }
}
```

* Logic Flows have to implement `ILogicFlow`
* The asynchronous OnNext method is responsible for handling the actions and executing corresponding logic
* During handling one action (LoadItems.Invoke) we can dispatch many other actions, like Request or Success that will be handled by state flows

AppService is an example service that fetches the data from database. For the training purposes we can implement it like below.

```C#
public interface IAppService
{
    Task<IEnumerable<string>> GetItems();
}

public class AppService : IAppService
{
    public async Task<IEnumerable<string>> GetItems()
    {
        await Task.Delay(1000);
        return new[] { "Item1", "Item2" };
    }
}
```

## Startup

Having all the pieces in place we can configure Rudder in the application startup.

In `Startup.cs`:
```C#
public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddRudder<AppState>(options =>
    {
        options.AddStateInitializer<InitialState>();
        options.AddStateFlows();
        options.AddLogicFlows();
    });
}
```

* `options.AddStateInitializer` is used to register our state initializer that we prepared before.
* `options.AddStateFlows` is used to add all the state flows from the calling assembly. Optionally we can add more assemblies to scan as param array parameter.
* `options.AddLogicFlows` is used to add all the logic flows from the calling assembly. Optionally we can add more assemblies to scan as param array parameter.
* It's possible to create middleware that will run after dispatching each action. Such middleware has to implement IStoreMiddleware and can be registered using `options.AddMiddleware` method.

## Logging
Optionally we can add JavaScript console logging of dispatched actions. In order to do it we need to register the middleware like below.

```C#
public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddRudder<AppState>(options =>
    {
        options.AddStateInitializer<InitialState>();
        options.AddStateFlows();
        options.AddLogicFlows();

        #if DEBUG
        options.AddJsLogging(); // Logging middleware
        #endif
    });
}
```

Having the logging middleware enabled, we can observe the dispatched actions together with corresponding data in the web browser's console during application runtime:

![Logging](https://raw.githubusercontent.com/kjeske/rudder/master/docs/img/logging.png)

## License

Rudder is Copyright © 2019 Krzysztof Jeske and other contributors under the [MIT license](https://raw.githubusercontent.com/kjeske/rudder/master/LICENSE.txt)