using System;
using UnityEngine;

namespace SimpleECS
{
    public abstract class EntityView<T> : MonoBehaviour where T : Component
    {
        public T component
        {
            get => world != null ? world.GetComponent<T>(entityId) : null;
        }

        [SerializeField] protected World world;

        private string entityId;
        
        private Action<Component> updateCallback;
        private Action<Component> destroyCallback;

        public void BindComponent(T component, World world)
        {
            this.entityId = component.entity;
            this.world = world;
            OnComponentBind(component, world);

            updateCallback = world.SubscribeUpdates<T>(HandleUpdate);
            destroyCallback = world.SubscribeDestroy<T>(HandleDestroy);
        }

        public void UnbindComponent(World world)
        {
            this.entityId = null;

            if (world != null)
            {
                world.UnsubscribeUpdates<T>(updateCallback);
                world.UnsubscribeDestroy<T>(destroyCallback);
            }

            OnComponentDestroy();
        }
        
        private void OnDestroy()
        {
            if (world != null)
            {
                world.UnsubscribeUpdates<T>(updateCallback);
                world.UnsubscribeDestroy<T>(destroyCallback);
            }
        }

        public abstract void OnComponentDestroy();
        
        public abstract void OnComponentUpdate( T newComp);
        
        public abstract void OnComponentBind(T component, World world);

        private void HandleUpdate(T newComp)
        {
            if (component != null && newComp.entity == component.entity)
            {
                OnComponentUpdate(newComp);
            }
        }

        private void HandleDestroy(T destroyedComp)
        {
            if (component != null && destroyedComp.entity == component.entity)
            {
                OnComponentDestroy();
            }
        }
        
    }
}