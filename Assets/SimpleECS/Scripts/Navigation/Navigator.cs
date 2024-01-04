using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class Navigator<TScreen> : ScriptableObject where TScreen : NavigatorScreen
{
    public string CurrentScreen => navigationStack.Count > 0 ? navigationStack.Peek().key : null;

    public List<TScreen> screens = new List<TScreen>();

    // First argument is the target screen, second is previous, third is abstract data
    public Action<string, string, object> OnNavigate;
    public Action<string, string, object> OnNavigateBack;

    public Stack<(string key, object data)> navigationStack = new Stack<(string key, object data)>();

    public void Clear()
    {
        if (navigationStack.Count == 0) return;

        string currentScreen = navigationStack.Peek().key;
        navigationStack.Clear();
        OnNavigateBack?.Invoke(null, currentScreen, null);
    }
    
    public bool Navigate(string id, object data)
    {
        if (CurrentScreen == id)
        {
            Debug.LogWarning(string.Format("Navigating to the same screen {0}. Ignoring event.", id));
            return false;
        }
        
        return Navigate(this[id], data);
    }

    public bool Navigate(int index, object data)
    {
        return Navigate(this[index], data);
    }

    private bool Navigate(TScreen screen, object data)
    {
        string hiddenScreen = null;
        
        if (navigationStack.Count > 0)
        {
            hiddenScreen = navigationStack.Peek().key;
        }
        
        navigationStack.Push((screen.name, data));

        OnNavigate?.Invoke(screen.name, hiddenScreen, data);
        return true;
    }

    public void NavigateBack()
    {
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
            OnNavigateBack?.Invoke(nextScreen.key, hiddenScreen, nextScreen.data);
        }
        else
        {
            OnNavigateBack?.Invoke(null, hiddenScreen, null);
        }
    }

    public TScreen this[int index] => screens[index];

    public TScreen this[string name]
    {
        get
        {
            var index = IndexOf(name);
            return screens[index];
        }
    }

    private int IndexOf(string name)
    {
        return screens.IndexOf(screens.Find(screen => screen.name == name));
    }
}