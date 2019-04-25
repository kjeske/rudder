# Ruddex
Ruddex is a state container for server-side Blazor. It works similar to redux and uses similar naming in order to make it easier to understand the concept.

The main features of Ruddex:
* One state object per whole application
* The interactions from the user interface are dispatching actions (events)
* Some actions are used only to change the state of an app and some actions are used to trigger business logic.
* Changes to the main app state are done only in reducers. Reducers are classes which take the dispatched actions and depending of the type and data of those actions are changing the state accordinglyf.
* Business logic is triggered in sagas. Sagas are classes that take dispatched actions, do business logic and dispatch another actions when business logic starts and finishes.

# Getting started

## Installation
Add the Ruddex nuget package to your application. One of the methods is to execute following command from the command line in the project folder:
```
dotnet add package Ruddex
```

## AppState
We need to start with creating a first draft of our application's state. It will be a regular class that will be used to keep the data used by the UI.

```C#
public class AppState
{
    public static AppState GetInitialState() =>
        new AppState {
            Items = new List<string>(),
            IsFetching = false
        };

    public List<string> Items { get; set; }

    public bool IsFetching { get; set; }
}
```

We also added GetInitialState static method that will be needed later during app startup setup.

## App component

App.razor component is an initial component for the whole application. Let's change it to:

```razor
@inject Store<AppState> store

<StoreProvider Store="store">
    <Router AppAssembly="typeof(Startup).Assembly" />
</StoreProvider>
```

The Router attribute comes from MVC and is present in the Blazor template as well. In order to make Rudex working, the Router attribute has to be surrounded by StoreProvider component with Store parameter. Store is a heart of the whole library - it keeps the application state and all the mechanisms to dispatch messages. Thanks to StoreProvider component, store instance will be available to the descendant components, when needed.

At that stage we also define the application state type, which is AppState. The Store<AppState> is injected using dependency injection. The registration will be setup in further steps.

## Actions

Actions are events that float thorugh the system and are handled by reducers and sagas. Reducers are changing the state and sagas are handling the side effects and are mainly responsible for business logic.

Actions can be of any type, but preferrably they should be custom classes in order to pass some additional data when needed.

```C#
public class Actions
{
    public class LoadItems
    {
        public class Request { }

        public class Success
        {
            public List<string> Items { get; set; }
        }
    }
}
```

* `LoadItems` action is to trigger fetching the items from some storage, for example database
* `LoadItems.Request` action is to inform that the request to get the data has started
* `LoadItems.Success` action is to inform that getting the data is finished. It contains Items property with retrieved data
* The structure of the actions classes is arbitrary.

## First custom component

Let's create a custom component and name it Items.razor. It will show the elements from our state and have a button to load those elements. When the elements are being fetched, the loading indication should be shown.

```razor
@inherits StateComponent<AppState>

<h1>App</h1>

@if (State.IsFetching)
{
    <p>Is loading</p>
} else {
    foreach (var item in State.Items)
    {
        <p>item</p>
    }

    <button onclick="LoadItems">Load items</button>
}

@functions {
    void LoadItems() => Put(new Actions.LoadItems());
}
```

There are several things to mention here:
* Whenever we want to use our state or dispatch an action,  we need to inherit from StateComponent<AppState>
* Having StateComponent<AppState> as a base class gives us access to:
    - `State` property - our AppState instance
    - `Put` method - method used to dispatch actions
* The `LoadItems` method is attached to the `Load items` button. It's role is to dispatch an instance of the Actions.LoadItems action.

## Reducers

As mentioned before, reducers are classes used to change the state accordingly to coming actions.

```C#
public class ItemsReducer : IReducer<AppState>
{
    public AppState Handle(AppState appState, object actionValue)
    {
        switch (actionValue)
        {
            case Actions.LoadItems.Request _:
                appState.IsFetching = true;
                return appState;

            case Actions.LoadItems.Success action:
                appState.IsFetching = false;
                appState.Items = action.Items;
                return appState;

            default:
                return appState;
        }
    }
}
```

* Reducer has to implement IReducer<AppState> where AppState is our application state object type
* Whenever action is dispatched in the system, the reducer's Hanlder method will be called
* The `Handle` method is responsible to react on the actions and change the state where needed
* There can be many reducers, each responsible for its own area
