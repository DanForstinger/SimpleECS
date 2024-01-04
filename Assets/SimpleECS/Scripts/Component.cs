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
        public string entity;

        [NonSerialized] private Action<Type, string> PropertyChanged;

        public void SubscribePropertyChanged(Action<Type, string> callback)
        {
            PropertyChanged += callback;
        }
        
        // This method is used to notify the world of property changes.
        public void UpdateProperty<T>(ref T property, T value, bool skipEqualityCompare = false)
        {
            if (!skipEqualityCompare && EqualityComparer<T>.Default.Equals(property , value)) return;
            
            var type = GetType();
            property = value;
            PropertyChanged?.Invoke(type, entity);
        }
    }
}
