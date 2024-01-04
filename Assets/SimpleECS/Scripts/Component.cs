using System;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimpleECS
{
    public class Component
    {
        // The parent entity that owns this component.
        public int entity;

        [NonSerialized] private Action<Type, int> PropertyChanged;

        public void SubscribePropertyChanged(Action<Type, int> callback)
        {
            PropertyChanged += callback;
        }
        
        // This method is used to notify the world of property changes.
        public void UpdateProperty<T>(ref T property, T value) where T : struct
        {
            var type = GetType();
            property = value;
            PropertyChanged?.Invoke(type, entity);
        }
    }
}
