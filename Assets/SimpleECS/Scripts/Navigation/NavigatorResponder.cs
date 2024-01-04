using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public abstract class NavigatorResponder<TScreen, TNavigator> : MonoBehaviour where TScreen : NavigatorScreen
    where TNavigator : Navigator<TScreen>
{
    [SerializeField] protected TNavigator navigator;
    [SerializeField] private Transform container;

    protected Dictionary<string, TScreen> screenMap = new Dictionary<string, TScreen>();

    protected bool isNavigating = false;

    private void Awake()
    {
        // Initialize screens that already exist in the scene.
        var screens = GetComponentsInChildren<TScreen>();

        foreach (var screen in screens)
        {
            var navScreenPrefab = navigator.screens.Find(navScreen => navScreen.name == screen.gameObject.name);
            if (navScreenPrefab != null)
            {
                screenMap.Add(navScreenPrefab.name, screen);
                screen.gameObject.SetActive(false);
            }
        }

        // spawn all screens ahead of time.
        foreach (var screen in navigator.screens)
        {
            if (screenMap.ContainsKey(screen.name)) return;

            // If it doesn't exist, spawn it
            var obj = Instantiate(screen.gameObject, container);
            obj.transform.localPosition = Vector3.zero;
            var screenComp = obj.GetComponent<TScreen>();
            screenMap.Add(screen.name, screenComp);
            screenComp.gameObject.SetActive(false);
        }
        
        // spawn all screens not in the map
        navigator.Clear();
    }

    protected virtual void OnEnable()
    {
        isNavigating = false;
        navigator.OnNavigate += OnNavigate;
        navigator.OnNavigateBack += OnNavigateBack;
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();

        navigator.OnNavigate -= OnNavigate;
        navigator.OnNavigateBack -= OnNavigateBack;
    }

    protected virtual void OnNavigate(string screenName, string hiddenScreen, object data)
    {
        Debug.Log("Navigating to " + screenName);

        StartCoroutine(ExecNavigate(screenName, hiddenScreen, data));
    }
    
    protected virtual void OnNavigateBack(string screenName, string hiddenScreen, object data)
    {
        Debug.Log("Navigating BACK to " + screenName);

        StartCoroutine(ExecNavigate(screenName, hiddenScreen, data));
    }

    private IEnumerator ExecNavigate(string nextScreenName, string hiddenScreenName, object data)
    {
        while (isNavigating)
        {
            Debug.Log("STUCK IN NAVIGATOR LOOP");
            yield return null;
        }

        isNavigating = true;

        // Handle if we are clearing the nav stack.
        if (nextScreenName == null)
        {
            foreach (var screen in screenMap)
            {
                if (hiddenScreenName != null && screen.Key == hiddenScreenName)
                {
                    continue;
                }

                if (screen.Value.gameObject.activeSelf)
                {
                    StartCoroutine(HideAndDisableScreen(screen.Value, null));
                }
            }
        }

        // Handle hidden screen
        if (hiddenScreenName != null && screenMap.ContainsKey(hiddenScreenName))
        {
            var hiddenScreen = screenMap[hiddenScreenName];
            yield return HideAndDisableScreen(hiddenScreen, nextScreenName);
        }

        // if we have another screen, navigate to it!
        if (nextScreenName != null)
        {
            TScreen nextScreen = screenMap[nextScreenName];

            nextScreen.gameObject.SetActive(true);
            
            nextScreen.OnShow(hiddenScreenName, data);

            yield return nextScreen.OnShowAsync(hiddenScreenName, data);
        }

        isNavigating = false;
    }

    protected IEnumerator HideAndDisableScreen(TScreen screen, string nextScreenName)
    {
        // Call on hide with the next screen name
        screen.OnHide(nextScreenName);

        // Exec the async behaviour
        yield return screen.OnHideAsync(nextScreenName);

        screen.gameObject.SetActive(false);
    }
}