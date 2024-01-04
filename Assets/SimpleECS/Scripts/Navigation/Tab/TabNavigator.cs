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


[CreateAssetMenu(fileName = "TabNavigator", menuName = "Navigation/TabNavigator")]
public class TabNavigator : Navigator<TabNavigatorScreen>
{

}

#if UNITY_EDITOR
[CustomEditor(typeof(TabNavigator))]
public class TabNavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var navigationRoute = (TabNavigator)target;

        var boldText = new GUIStyle(GUI.skin.label);
        boldText.fontStyle = FontStyle.Bold;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Screens", boldText);
        EditorGUILayout.Space();

        for (int i = 0; i < navigationRoute.screens.Count; ++i)
        {
            navigationRoute.screens[i] = EditorGUILayout.ObjectField(navigationRoute.screens[i], typeof(TabNavigatorScreen), false) as TabNavigatorScreen;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Screen"))
        {
            navigationRoute.screens.Add(null);
        }

        if (GUILayout.Button("Remove Screen"))
        {
            navigationRoute.screens.RemoveAt(navigationRoute.screens.Count - 1);
        }

        EditorUtility.SetDirty(navigationRoute);
    }
}

#endif
