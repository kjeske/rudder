# Rudder
Rudder is a state container for Blazor.

The main features of Rudder:
* One state object per whole application
* The user interface interactions dispatch actions
* Changes to the main app state are done only in the state flows. State flows are responsible for taking the dispatched actions and depending of the type and data of those actions changing the state accordingly.
* Business logic is triggered in logic flows. Logic flows are responsible for taking the dispatched actions, executing business logic and optionally dispatching another actions during that process.
* Works with client-side and server-side Blazor.

[Example of a Blazor project using Rudder](https://github.com/kjeske/rudder-example)

# Purpose
Rudder makes it easier to maintain the application state and user interactions. Rudder brings:
* Clean separation of concerns. State flows change the state, logic flows handle the side effects, components render the view and dispatch actions.
* Easier testing
* Global state makes it easier to serialize and persist in the storage for recovery purposes
* Views don't contain complex logic and are responsible mostly for rendering and dispatching simple actions objects

# How it works
1. There is one state object that represents the whole application UI state.
1. Application state is wrapped in a `Store` class that apart from keeping the state is also responsible for dispatching the actions. Actions are objects that describe events together with corresponding event data.
1. Views are rendered using the Razor components. In Rudder we can distinguish two types of components:
   - Components that have access to the `Store` - called StateComponents.
   - Components without the access  to the `Store` - just regular components.
1. User interactions, like `onclick`, are handled in components and then can be transformed into appropriate action objects that describe the user intention (for example ReloadList action) and then dispatched.
1. Every dispatched action triggers:
   * State flows that are responsible for changing the state accordingly to the processing action. Example:
     - If currently processing action is `Actions.GetItems.Request`, then set `IsFetching` state property to `true`.
   * Logic flows that are responsible for handling the business logic and dispatching further actions during that process. Example:
     - If currently processing action is `Actions.GetItems` then:
       - Dispatch `Actions.GetItems.Request` action
       - Load items from the database
       - Dispatch `Actions.GetItems.Success` action with items data if the request was successful.
       - Dispatch `Actions.GetItems.Failure` action with error message if the request was not successful.
1. Every application state's change triggers the rerendering only of those `StateComponents` which subscribe to the changed property of the state.

# Getting started with new application

Rudder works with applications using Blazor, which is a part of .NET 6

## Installation
Add the Rudder NuGet package to your application.

Command line:
```
dotnet add package Rudder
```
Package Manager Console:
```
Install-Package Rudder
```

## AppState
We will start with creating first draft of our application's state in a class. It will be used to keep the data used by the UI.

```C#
public record AppState
{
    public IReadOnlyList<string> Items { get; init; }
    public bool IsFetching { get; init; }
}
```

## Initial state
We need to define our initial shape of application state. It can be used to prepare the state basing on currently logged in user and data from the database.

```C#
public class AppStateInitializer : IStateInitializer<AppState>
{
    public Task<AppState> GetInitialStateAsync()
    {
        var state = new AppState
        {
            Items = new[] { "Item 1" },
            IsFetching = false
        };

        return Task.FromResult(state);
    }
}

```

## App.razor
In the Blazor app entry point (usually App.razor component) we need to wrap existing logic with StoreContainer component in order to initialize the store.

```razor
<StoreContainer TState="AppState">
    <Router AppAssembly="typeof(Startup).Assembly" />
</StoreContainer>

```

## Actions

Actions describe events that float thorough the system and are handled by state flows and logic flows. State flows change the state and logic flows handle the side effects and are mainly responsible for business logic.

Actions are defined by records which can pass data when needed. The structure of the actions is arbitrary.

```C#
public static class Actions
{
    public record LoadItems
    {
        public record Request;
        public record Success(string[] Items);
        public record Failure(string ErrorMessage);
    }
}
```
Remarks:
* `LoadItems` action is to trigger fetching the items from some storage
* `LoadItems.Request` action is to inform that the request to get the data has started
* `LoadItems.Success` action is to inform that getting the data is finished. It contains Items property with retrieved data

## First state component

Let's create a state component with access to the `Store` and name it `Items.razor`. It will show the elements from our state and have a button to load those elements. When the elements are being fetched, the loading indication should be shown.

We will start with creating the `Items.razor` component:

```razor
@inherits StateComponent<AppState>

<h1>App</h1>

@if (isFetching)
{
    <p>Is loading</p>
}
else
{
    foreach (var item in items)
    {
        <p>item</p>
    }

    <button onclick="@LoadItems">Load items</button>
}

@code {
    string[] items;
    bool isFetching;

    void LoadItems() => Put(new Actions.LoadItems());
    
    protected override void OnInitialized()
    {
        UseState(state => items = state.Items);
        UseState(state => isFetching = state.IsFetching);
    }
}
```

## State flows

State flows are classes providing functionality to change the state accordingly to the coming actions.

```C#
public class ItemsStateFlow : IStateFlow<AppState>
{
    public AppState Handle(AppState appState, object actionValue) => actionValue switch
    {
        Actions.LoadItems.Request =>
            appState with { IsFetching = true },

        Actions.LoadItems.Success action =>
            appState with { IsFetching = false, Items = action.Items };

        _ => appState
    };
}
```
Remarks:
* State flow has to implement IStateFlow<AppState> where AppState is our application state object type
* Whenever action is dispatched in the system, the `Handle` method in all the state flows will be called
* The `Handle` method is responsible for reacting on the actions and changing the state where needed
* The appState parameter value is a read-only record and can't be mutated. Any intent to change the state should happen by using `with` statement, which creates a new copy of the state with a change.
* There can be many state flows, each responsible for its own part of the state.

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
            case Actions.LoadItems:
                await LoadItems();
                break;
        }
    }

    private async Task LoadItems()
    {
        _store.Put(new Actions.LoadItems.Request());

        try
        {
            var result = await _appService.GetItems();
            _store.Put(new Actions.LoadItems.Success { Items = result.ToArray() });
        }
        catch (Exception exception)
        {
            _store.Put(new Actions.LoadItems.Failure { ErrorMessage = exception.Message });
        }
    }
}
```

* Logic Flows have to implement `ILogicFlow`
* The asynchronous OnNext method is responsible for handling the actions and executing corresponding logic
* During handling one action (LoadItems.Invoke) we can dispatch many other actions, like Request or Success that will be handled by state flows

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

Rudder is Copyright © 2022 Krzysztof Jeske and other contributors under the [MIT license](https://raw.githubusercontent.com/kjeske/rudder/master/LICENSE.txt)