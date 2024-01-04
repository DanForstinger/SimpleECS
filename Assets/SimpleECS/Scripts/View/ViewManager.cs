#region

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace SimpleECS
{
    public abstract class ViewManager<T, K> : MonoBehaviour where T : Component where K : EntityView<T>
    {
        [SerializeField] protected World world;

        private readonly Dictionary<string, K> entityToViewMap = new();
        private Action<Component> createCallback;

        private Action<Component> destroyCallback;
        public List<K> views { get; } = new();

        public virtual void OnEnable()
        {
            createCallback = world.SubscribeCreate<T>(OnComponentCreated);
            destroyCallback = world.SubscribeDestroy<T>(OnComponentDestroy);

            // destroy all current views.
            foreach (var pair in entityToViewMap) DestroyView(pair.Value);

            entityToViewMap.Clear();

            world.ForEach<T>(comp => { OnComponentCreated(comp); });
        }

        public virtual void OnDisable()
        {
            if (world == null) return;

            if (destroyCallback != null) world.UnsubscribeDestroy<T>(destroyCallback);

            if (createCallback != null) world.UnsubscribeCreate<T>(createCallback);
        }

        public K GetView(T component)
        {
            return component != null && entityToViewMap.ContainsKey(component.entity)
                ? entityToViewMap[component.entity]
                : null;
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

            if (entityToViewMap[comp.entity] != null)
            {
                views.Add(entityToViewMap[comp.entity]);

                entityToViewMap[comp.entity].BindComponent(comp, world);
            }
        }

        protected void OnComponentDestroy(T newComp)
        {
            if (entityToViewMap.ContainsKey(newComp.entity))
            {
                var view = entityToViewMap[newComp.entity];

                if (view != null)
                {
                    views.Remove(view);
                    DestroyView(entityToViewMap[newComp.entity]);
                }

                entityToViewMap.Remove(newComp.entity);
            }
        }
    }
}