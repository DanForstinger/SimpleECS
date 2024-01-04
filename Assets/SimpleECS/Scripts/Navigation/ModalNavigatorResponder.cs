#region

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

public class ModalNavigatorResponder : MonoBehaviour, INavigatorResponder
{
    [SerializeField] protected Navigator navigator;
    [SerializeField] private Transform container;

    private bool continueNavigating;
    private Coroutine navigationCoroutine;

    private Queue<(string, string, object)> navigationQueue = new();

    protected Dictionary<string, NavigatorScreen> screenMap = new();

    private void Awake()
    {
        IsNavigating = false;
        continueNavigating = false;
        navigationQueue = new Queue<(string, string, object)>();
        // Initialize screens that already exist in the scene.
        var screens = GetComponentsInChildren<NavigatorScreen>();

        foreach (var screen in screens)
        {
            var navScreenPrefab = navigator.screens.Find(navScreen => navScreen.name == screen.gameObject.name);
            if (navScreenPrefab != null)
            {
                screenMap.Add(navScreenPrefab.GetType().Name, screen);
                screen.gameObject.SetActive(false);
            }
        }

        // spawn all screens ahead of time.
        foreach (var screen in navigator.screens)
        {
            // If it doesn't exist, spawn it
            if (screenMap.ContainsKey(screen.GetType().Name)) return;


            // we want to start the objects deactivated...
            // so that things that reference a world etc don't break.
            screen.gameObject.SetActive(false);

            var obj = Instantiate(screen.gameObject, container);

            screen.gameObject.SetActive(true);

            obj.transform.localPosition = Vector3.zero;
            var screenComp = obj.GetComponent<NavigatorScreen>();
            screenMap.Add(screenComp.GetType().Name, screenComp);
            screenComp.gameObject.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        IsNavigating = false;
        continueNavigating = false;
        navigationCoroutine = null;
        navigator.BindResponder(this);
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();

        navigator.UnbindResponder(this);
    }

    public bool IsNavigating { get; set; }

    public void Clear()
    {
        continueNavigating = false;
        IsNavigating = false;
        if (navigationCoroutine != null) StopCoroutine(navigationCoroutine);
        navigationCoroutine = null;
        navigationQueue = new Queue<(string, string, object)>();
    }

    public void OnNavigate(string screenName, string hiddenScreen, object data)
    {
        if (screenName == null || screenName == typeof(EmptyOverlay).Name)
        {
            continueNavigating = true;
        }
        else
        {
            navigationQueue.Enqueue((screenName, hiddenScreen, data));

            if (navigationCoroutine == null) navigationCoroutine = StartCoroutine(ExecNavigate());
        }
    }

    private IEnumerator ExecNavigate()
    {
        while (navigationQueue.Count > 0)
        {
            continueNavigating = false;
            var entry = navigationQueue.Dequeue();
            var nextScreenName = entry.Item1;
            var hiddenScreenName = entry.Item2;
            var data = entry.Item3;

            // Handle if we are clearing the nav stack.
            if (nextScreenName == null)
                foreach (var screen in screenMap)
                {
                    if (hiddenScreenName != null && screen.Key == hiddenScreenName) continue;

                    if (screen.Value.gameObject.activeSelf) StartCoroutine(HideAndDisableScreen(screen.Value, null));
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
                var nextScreen = screenMap[nextScreenName];

                nextScreen.gameObject.SetActive(true);

                nextScreen.OnShow(hiddenScreenName, data);

                yield return nextScreen.OnShowAsync(hiddenScreenName, data);
            }

            while (!continueNavigating)
                yield return null;

            continueNavigating = false;

            if (navigationQueue.Count > 0)
            {
                var wait = new WaitForSeconds(0.25f);
                yield return wait;
            }
        }

        // hide all screens when coroutine finishes.
        foreach (var screen in screenMap)
            if (screen.Value.gameObject.activeSelf)
                StartCoroutine(HideAndDisableScreen(screen.Value, null));

        navigationCoroutine = null;
    }

    protected IEnumerator HideAndDisableScreen(NavigatorScreen screen, string nextScreenName)
    {
        // Call on hide with the next screen name
        screen.OnHide(nextScreenName);

        // Exec the async behaviour
        yield return screen.OnHideAsync(nextScreenName);

        screen.gameObject.SetActive(false);
    }
}