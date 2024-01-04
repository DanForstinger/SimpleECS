using UnityEngine;
using System.Collections;

public class TabNavigatorScreen : NavigatorScreen
{
    public string title;
    public override void OnShow(string previousScreen, object data)
    {
        
    }

    public override void OnHide(string nextScreen)
    {

    }

    public override IEnumerator OnShowAsync(string previousScreen, object data)
    {
        yield break;
    }

    public override IEnumerator OnHideAsync(string nextScreen)
    {
        yield break;
    }
}
