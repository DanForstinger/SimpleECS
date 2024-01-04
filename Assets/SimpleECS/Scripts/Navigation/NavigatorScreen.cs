using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public abstract class NavigatorScreen : MonoBehaviour
{
    // These can be used when there is no need to delay or wait for transition
    public abstract void OnShow(string previousScreen, object data);
    public abstract void OnHide(string nextScreen);
    
    // These can be used to delay showing of the next screen for a transition or data fetch to complete.
    public abstract IEnumerator OnShowAsync(string previousScreen, object data);
    public abstract IEnumerator OnHideAsync(string nextScreen);
    
}