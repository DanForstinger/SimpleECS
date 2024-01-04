using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabNavigatorActions : MonoBehaviour
{
    public TabNavigator navigation;

    public void GoBack()
    {
        navigation.NavigateBack();
    }

    public void GoTo(string name)
    {
        navigation.Navigate(name, null);
    }
}
