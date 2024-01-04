using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleECS
{
    public abstract class ViewManager<T, K> : MonoBehaviour where T : Component where K : EntityView<T>
    {
        public List<K> views { get; private set; } = new List<K>();
        
        [SerializeField] protected World world;

        private Dictionary<int, K> entityToViewMap = new Dictionary<int, K>();
        
        private Action<Component> destroyCallback;
        private Action<Component> createCallback;

        public K GetView(T component)
        {
            return component != null && entityToViewMap.ContainsKey(component.entity) ? entityToViewMap[component.entity] : null;
        }
        
        public virtual void OnEnable()
        {
            createCallback = world.SubscribeCreate<T>(OnComponentCreated);
            destroyCallback = world.SubscribeDestroy<T>(OnComponentDestroy);

            // destroy all current views.
            foreach (var pair in entityToViewMap)
            {
                DestroyView(pair.Value);
            }

            entityToViewMap.Clear();
            
            world.ForEach<T>(comp =>
            {
                OnComponentCreated(comp);
            });
        }

        public virtual void OnDisable()
        {
            world.UnsubscribeDestroy<T>(destroyCallback);
            world.UnsubscribeCreate<T>(createCallback);
        }

        protected abstract K CreateView(T component);
        protected abstract void DestroyView(K view);
        
        protected void OnComponentCreated(T comp)
        {
            if (entityToViewMap.ContainsKey(comp.entity))
            {
                Debug.LogError("Trying to add component to view manager, but it already exists.");
                return;
            }

            StartCoroutine(BindAfterOneFrame(comp));
        }

        private IEnumerator BindAfterOneFrame(T comp)
        {
            yield return new WaitForEndOfFrame();
            entityToViewMap[comp.entity] = CreateView(comp);
            views.Add(entityToViewMap[comp.entity]);
            
            if (entityToViewMap[comp.entity] != null)
            {
                entityToViewMap[comp.entity].BindComponent(comp, world);
            }
        }

        protected void OnComponentDestroy(T newComp)
        {
            if (entityToViewMap.ContainsKey(newComp.entity))
            {
                var view = entityToViewMap[newComp.entity];
                views.Remove(view);
                DestroyView(entityToViewMap[newComp.entity]);
                entityToViewMap.Remove(newComp.entity);
                
            }
        }
    }
}