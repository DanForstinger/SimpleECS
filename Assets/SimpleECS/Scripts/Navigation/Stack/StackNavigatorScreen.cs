using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StackNavigatorScreen : NavigatorScreen
{
    private Transition[] transitions = null;
    
    public override void OnShow(string previousScreen, object data)
    {
        
    }

    public override void OnHide(string nextScreen)
    {

    }

    public override IEnumerator OnShowAsync(string previousScreen, object data)
    {
        if (transitions == null)
        {
            transitions = GetComponentsInChildren<Transition>();
        }

        foreach (var transition in transitions)
        {
            StartCoroutine(transition.TransitionIn());
        }

        var isTransitioning = true;

        while (isTransitioning)
        {
            isTransitioning = false;
            
            foreach (var transition in transitions)
            {
                isTransitioning |= transition.isTransitioning;
            }

            yield return null;
        }
    }

    public override IEnumerator OnHideAsync(string nextScreen)
    {
        if (transitions == null)
        {
            transitions = GetComponentsInChildren<Transition>();
        }

        Debug.Log($"Starting hide transitions for {gameObject.name}.");
        foreach (var transition in transitions)
        {
            StartCoroutine(transition.TransitionOut());
        }

        var isTransitioning = true;

        while (isTransitioning)
        {
            isTransitioning = false;
            
            foreach (var transition in transitions)
            {
                isTransitioning |= transition.isTransitioning;
            }

            yield return null;
        }
        
        Debug.Log($"Done hide transitions for {gameObject.name}.");
    }
}
