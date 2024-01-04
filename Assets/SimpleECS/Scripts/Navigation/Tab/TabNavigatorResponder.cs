using TMPro;
using UnityEngine;

public class TabNavigatorResponder : NavigatorResponder<TabNavigatorScreen, TabNavigator>
{
    // This represents the title of the screen.
    [SerializeField] private TextMeshProUGUI title;
    
    protected override void OnNavigate(string screenName, string hiddenScreen, object data)
    {
        base.OnNavigate(screenName, hiddenScreen, data);
        
        // Set the title
        if (title != null)
        {
            title.text = navigator[hiddenScreen].title;
        }
    }
}
