#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

public interface INavigatorResponder
{
    public bool IsNavigating { get; set; }
    public void Clear();
    public void OnNavigate(string screenName, string hiddenScreen, object data);
}

[CreateAssetMenu]
public class Navigator : ScriptableObject
{
    public bool AllowDuplicateScreens;

    public List<NavigatorScreen> screens = new();

    private readonly Stack<(string key, object data, Type type)> navigationStack = new();

    private readonly HashSet<INavigatorResponder> responders = new();

    // First argument is the target screen, second is previous, third is abstract data
    public Action<string, string, object> OnNavigate;

    private Dictionary<Type, string> screenNameMap = new();

    public string CurrentScreen => navigationStack.Count > 0 ? navigationStack.Peek().key : null;
    public Type CurrentScreenType => navigationStack.Count > 0 ? navigationStack.Peek().type : null;
    public object CurrentScreenData => navigationStack.Count > 0 ? navigationStack.Peek().data : null;

    public bool IsNavigating
    {
        get
        {
            foreach (var responder in responders)
                if (responder != null && responder.IsNavigating)
                    return true;

            return false;
        }
    }

    public void BindResponder(INavigatorResponder responder)
    {
        responders.Add(responder);

        responder.OnNavigate(CurrentScreen, null, CurrentScreenData);
    }

    public void UnbindResponder(INavigatorResponder responder)
    {
        responders.Remove(responder);
    }

    public bool IsShowingScreen<T>() where T : NavigatorScreen
    {
        return CurrentScreenType == typeof(T);
    }

    public void Clear()
    {
        if (navigationStack.Count == 0) return;
        var currentScreen = navigationStack.Peek();
        navigationStack.Clear();
        OnNavigate?.Invoke(null, currentScreen.key, null);

        foreach (var responder in responders)
        {
            if (responder == null) continue;
            responder.Clear();
            responder.OnNavigate(currentScreen.key, null, null);
        }
    }

    public bool Navigate(string nextScreen, object data = null, bool force = false)
    {
        foreach (var s in screens)
            if (s.GetType().Name == nextScreen)
                Navigate(s.GetType(), data, force);

        return false;
    }

    public bool Navigate(Type nextScreen, object data = null, bool force = false)
    {
        if (IsNavigating) return false;

        if (CurrentScreen != null && nextScreen == CurrentScreenType && !AllowDuplicateScreens)
        {
            Debug.LogWarning(string.Format($"Navigating to the same screen {CurrentScreen}. Ignoring event."));
            return false;
        }

        Type hiddenScreen = null;

        if (navigationStack.Count > 0) hiddenScreen = navigationStack.Peek().type;

        var screen = screens.Find(scr => scr.GetType() == nextScreen);

        if (screen == null)
        {
            Debug.LogError($"Error: Screen {nextScreen} does not exist in the navigator.");
            return false;
        }

        var nextScreenName = nextScreen.Name;
        var hiddenScreenName = hiddenScreen?.Name;

        navigationStack.Push((nextScreenName, data, nextScreen));

        OnNavigate?.Invoke(nextScreenName, hiddenScreenName, data);
        foreach (var responder in responders)
        {
            if (responder == null) continue;
            responder.OnNavigate(nextScreenName, hiddenScreenName, data);
        }

        return true;
    }

    public bool Navigate<T>(object data = null) where T : NavigatorScreen
    {
        return Navigate(typeof(T), data);
    }

    public void NavigateBack()
    {
        if (IsNavigating) return;

        if (navigationStack.Count == 0)
        {
            Debug.LogWarning("Can't navigate back! We have no more screens!");
            return;
        }

        string hiddenScreen = null;

        if (navigationStack.Count > 0)
        {
            var toHide = navigationStack.Pop();
            hiddenScreen = toHide.key;
        }

        if (navigationStack.Count > 0)
        {
            var nextScreen = navigationStack.Peek();
            OnNavigate?.Invoke(nextScreen.key, hiddenScreen, nextScreen.data);
            foreach (var responder in responders)
            {
                if (responder == null) continue;
                responder.OnNavigate(nextScreen.key, hiddenScreen, nextScreen.data);
            }
        }
        else
        {
            OnNavigate?.Invoke(null, hiddenScreen, null);
            foreach (var responder in responders)
            {
                if (responder == null) continue;
                responder.OnNavigate(null, hiddenScreen, null);
            }
        }
    }
}