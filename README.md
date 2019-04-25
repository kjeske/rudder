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

App component is an initial component for the whole application. It can look like that:

```razor
@inject Store<AppState> store

<StoreProvider Store="store">
    <Router AppAssembly="typeof(Startup).Assembly" />
</StoreProvider>
```

The Router attribute comes from MVC and is present in the Blazor template as well. In order to make Rudex working, the Router attribute has to be surrounded by StoreProvider component with Store parameter. Store is a heart of the whole library - it keeps the application state and all the mechanisms to dispatch messages. Thanks to StoreProvider component, store instance will be available to the descendant components, when needed.
