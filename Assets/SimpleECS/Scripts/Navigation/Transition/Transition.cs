using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Transition : MonoBehaviour
{
    public bool isTransitioning { get; private set; }

    public IEnumerator TransitionIn()
    {
        isTransitioning = true;
        yield return OnTransitionIn();
        isTransitioning = false;
    }

    public IEnumerator TransitionOut()
    {
        isTransitioning = true;
        yield return OnTransitionOut();
        isTransitioning = false;
    }
    
    protected abstract IEnumerator OnTransitionIn();

    protected abstract IEnumerator OnTransitionOut();
}
