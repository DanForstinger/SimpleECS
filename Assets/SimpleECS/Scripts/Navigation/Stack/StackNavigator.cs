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


[CreateAssetMenu(fileName = "StackNavigator", menuName = "Navigation/StackNavigator")]
public class StackNavigator : Navigator<StackNavigatorScreen>
{
}
#if UNITY_EDITOR
[CustomEditor(typeof(StackNavigator))]
public class StackNavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var navigationRoute = (StackNavigator)target;

        var boldText = new GUIStyle(GUI.skin.label);
        boldText.fontStyle = FontStyle.Bold;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Screens", boldText);
        EditorGUILayout.Space();

        for (int i = 0; i < navigationRoute.screens.Count; ++i)
        {
            navigationRoute.screens[i] = EditorGUILayout.ObjectField(navigationRoute.screens[i], typeof(StackNavigatorScreen), false) as StackNavigatorScreen;
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
