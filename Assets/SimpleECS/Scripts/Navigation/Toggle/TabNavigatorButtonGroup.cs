using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This component controls a toggleable set of controls.
// It expects an animator with a single bool, isToggled, to control toggle state.
// To ensure proper functionality, make sure the first button in the group is the first screen in your navigator.
public class TabNavigatorButtonGroup : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    [SerializeField] private TabNavigator navigator;
    
    private Animator[] animators;
    private Action[] callbacks;
    
    void Awake()
    {
        animators = new Animator[buttons.Length];
        callbacks = new Action[buttons.Length];
        
        for (int i = 0; i < buttons.Length; ++i)
        {
            var animator = buttons[i].GetComponent<Animator>();
            if (animator == null) Debug.LogError("Created a toggle without required animator!");

            animators[i] = animator;
        }
    }

    void Start()
    {
        // Reset navigator on enable.
        navigator.Navigate(0, null);
    }

    void OnEnable()
    { 
        OnTogglePressed(buttons[0]);
        
        for (int i = 0; i < buttons.Length; ++i)
        {
            var btn = buttons[i];
            callbacks[i] = () => OnTogglePressed(btn);
            btn.onClick.AddListener(callbacks[i].Invoke);
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < buttons.Length; ++i)
        {
            buttons[i].onClick.RemoveListener(callbacks[i].Invoke);
        }
    }

    void OnTogglePressed(Button toggled)
    {
        for (int i = 0; i < buttons.Length; ++i)
        {
            var button = buttons[i];
            animators[i].SetBool("toggled", button == toggled);
        }
    }
}
