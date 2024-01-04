using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackNavigatorActions : MonoBehaviour
{
    public StackNavigator navigation;

    public void GoBack()
    {
        navigation.NavigateBack();
    }

    public void GoTo(string name)
    {
        navigation.Navigate(name, null);
    }
}
