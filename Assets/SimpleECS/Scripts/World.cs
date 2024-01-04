#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace SimpleECS
{
    [CreateAssetMenu]
    public class World : ScriptableObject
    {
        public enum ChangeType
        {
            Added,
            Removed,
            Updated
        }

        [SerializeField] [CanBeNull] private List<ScriptableObject> gameData;

        // This is used to cache subcomponents and speed up subsequent lookups when we are finding a child component with a parent class type.
        private readonly Dictionary<Type, Type[]> subcomponentMap = new();

        private Dictionary<string, Dictionary<string, Component>> components;

        private Dictionary<string, Entity> entities;
        private Dictionary<Type, List<Action<Component>>> subscribedCreateCallbacks;
        private Dictionary<Type, List<Action<Component>>> subscribedDestroyCallbacks;

        private Dictionary<Type, List<Action<Component>>> subscribedUpdateCallbacks;

        private Dictionary<Type, string> typeNameMap;

        private List<Component> updateQueue;

        public GameObject root { get; private set; }
        public List<IGameSystem> systems { get; private set; }

        public bool isInitialized { get; set; }

        public T GetData<T>() where T : ScriptableObject
        {
            var data = gameData.Find(d => d is T);
            if (data == null)
            {
                Debug.LogError("Trying to get data of type " + typeof(T).Name +
                               " but it does not exist in this world!");
                return null;
            }

            return data as T;
        }

        public List<T> GetAllData<T>() where T : ScriptableObject
        {
            var allData = gameData.FindAll(d => d is T);
            if (allData.Count == 0)
            {
                Debug.LogError("Trying to get data of type " + typeof(T).Name +
                               " but it does not exist in this world!");
                return null;
            }

            return allData.ConvertAll(itr => itr as T);
        }

        public T GetData<T>(string name) where T : ScriptableObject
        {
            var data = gameData.Find(d => d is T && d.name == name);
            if (data == null)
            {
                Debug.LogError("Trying to get data of type " + typeof(T).Name + " with key " + name +
                               " but it does not exist in this world!");
                return null;
            }

            return data as T;
        }

        #region Event Functions

        // This must be called by a MonoBehaviour in OnDestroy to properly destroy systems.
        public void Teardown()
        {
            for (var i = 0; i < systems.Count; ++i)
                if (systems[i] is IDestroySystem)
                    (systems[i] as IDestroySystem).Destroy();

            Reset();
        }

        public void Create(GameObject rootObject)
        {
            root = rootObject;

            Reset();
        }

        public void Reset()
        {
            isInitialized = false;

            typeNameMap = new Dictionary<Type, string>();
            systems = new List<IGameSystem>();
            components = new Dictionary<string, Dictionary<string, Component>>();
            entities = new Dictionary<string, Entity>();

            subscribedUpdateCallbacks = new Dictionary<Type, List<Action<Component>>>();
            subscribedDestroyCallbacks = new Dictionary<Type, List<Action<Component>>>();
            subscribedCreateCallbacks = new Dictionary<Type, List<Action<Component>>>();

            updateQueue = new List<Component>();
        }

        public void PostInitSystems()
        {
            // call post init on all the systems.
            for (var i = 0; i < systems.Count; ++i)
            {
                var system = systems[i];
                if (system is IPostInitSystem) (system as IPostInitSystem).PostInit();
            }

            // We are done initialization
            isInitialized = true;
        }

        // This must be called by a MonoBehaviour in an update loop to properly update systems
        // todo: Do we want to hook this up to render from photon instead?
        public void Update()
        {
            // only run this once intiialized
            if (isInitialized)
                // Update all the systems.
                for (var i = 0; i < systems.Count; ++i)
                {
                    var system = systems[i];

                    if (system is IUpdateSystem) (system as IUpdateSystem).UpdateSystem();
                }

            for (var i = 0; i < updateQueue.Count; ++i) SendComponentUpdate(updateQueue[i]);

            updateQueue.Clear();
        }

        // This must be called by a MonoBehaviour in an update loop to properly update systems
        public void FixedUpdate()
        {
            // only run this once intiialized
            if (isInitialized)
                // Update all the systems.
                for (var i = 0; i < systems.Count; ++i)
                {
                    var system = systems[i];

                    if (system is IFixedUpdateSystem) (system as IFixedUpdateSystem).FixedUpdate();
                }
        }

        #endregion

        #region Callbacks

        public Action<Component> SubscribeCreate<T>(Action<T> callback) where T : Component
        {
            if (subscribedCreateCallbacks == null) return null;

            if (!subscribedCreateCallbacks.ContainsKey(typeof(T)))
                subscribedCreateCallbacks.Add(typeof(T), new List<Action<Component>>());

            Action<Component> boundCallback = comp => { callback((T)comp); };

            subscribedCreateCallbacks[typeof(T)].Add(boundCallback);

            return boundCallback;
        }

        public void UnsubscribeCreate<T>(Action<Component> callback) where T : Component
        {
            if (subscribedCreateCallbacks == null || !subscribedCreateCallbacks.ContainsKey(typeof(T)) ||
                !subscribedCreateCallbacks[typeof(T)].Contains(callback))
            {
                Debug.LogWarning(
                    "Trying to unsubscribe from creating entities, but the callback passed was not subscribed.");
                return;
            }

            subscribedCreateCallbacks[typeof(T)].Remove(callback);
        }

        public Action<Component> SubscribeUpdates<T>(Action<T> callback) where T : Component
        {
            if (!subscribedUpdateCallbacks.ContainsKey(typeof(T)))
                subscribedUpdateCallbacks.Add(typeof(T), new List<Action<Component>>());

            Action<Component> boundCallback = comp => { callback((T)comp); };

            subscribedUpdateCallbacks[typeof(T)].Add(boundCallback);

            return boundCallback;
        }

        public void UnsubscribeUpdates<T>(Action<Component> callback) where T : Component
        {
            if (subscribedUpdateCallbacks == null || !subscribedUpdateCallbacks.ContainsKey(typeof(T)) ||
                !subscribedUpdateCallbacks[typeof(T)].Contains(callback))
            {
                Debug.LogWarning("Trying to unsubscribe from updates, but the callback passed was not subscribed.");
                return;
            }

            subscribedUpdateCallbacks[typeof(T)].Remove(callback);
        }

        public Action<Component> SubscribeDestroy<T>(Action<T> callback) where T : Component
        {
            if (!subscribedDestroyCallbacks.ContainsKey(typeof(T)))
                subscribedDestroyCallbacks.Add(typeof(T), new List<Action<Component>>());

            Action<Component> boundCallback = comp => { callback((T)comp); };

            subscribedDestroyCallbacks[typeof(T)].Add(boundCallback);

            return boundCallback;
        }

        public void UnsubscribeDestroy<T>(Action<Component> callback) where T : Component
        {
            if (subscribedDestroyCallbacks == null || !subscribedDestroyCallbacks.ContainsKey(typeof(T)) ||
                !subscribedDestroyCallbacks[typeof(T)].Contains(callback))
            {
                Debug.LogWarning("Trying to unsubscribe from updates, but the callback passed was not subscribed.");
                return;
            }

            subscribedDestroyCallbacks[typeof(T)].Remove(callback);
        }

        #endregion

        #region Iterators

        public T Find<T>(Predicate<T> predicate) where T : Component
        {
            var id = GetComponentID<T>();

            // Fast-exit if we have no components of this type
            if (components == null || !components.TryGetValue(id, out var bucket))
                return null;

            // Iterate the existing value collection – no List<> allocation
            foreach (var comp in bucket.Values)
            {
                // We know every value in this bucket is (at least) a Component,
                // but we still need the exact T when invoking the predicate.
                var typed = comp as T;
                if (typed != null && predicate(typed))
                    return typed; // early exit on first match
            }

            return null; // nothing matched
        }

        public List<T> FindAll<T>(Predicate<T> predicate) where T : Component
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var id = GetComponentID<T>();

            // Fast-exit if we have no components of this type
            if (components == null || !components.TryGetValue(id, out var bucket))
                return new List<T>(); // empty list, zero extra allocs

            // Allocate the result list once, with a sensible initial capacity
            var results = new List<T>(bucket.Count);

            foreach (var comp in bucket.Values)
            {
                // The dictionary stores Component, so cast to the exact T
                var typed = comp as T;
                if (typed != null && predicate(typed))
                    results.Add(typed);
            }

            return results;
        }


        /* Iterators */
        public void ForEach<T>(Action<T> callback) where T : Component
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (components == null ||
                !components.TryGetValue(GetComponentID<T>(), out var bucket))
                return; // nothing to iterate – silent no-op

            foreach (var comp in bucket.Values)
                callback((T)comp); // direct cast, no boxing
        }

        #endregion

        #region Private Fields

        private void OnPropertyChanged(Type type, string entity)
        {
            var id = GetComponentID(type);

            // Get the component
            var component = components[id][entity];

            if (!updateQueue.Contains(component)) updateQueue.Add(component);
        }

        private void SendComponentUpdate(Component component)
        {
            var type = component.GetType();

            if (subscribedUpdateCallbacks.ContainsKey(type))
                for (var i = subscribedUpdateCallbacks[type].Count - 1; i >= 0; --i)
                {
                    var callback = subscribedUpdateCallbacks[type][i];

                    callback?.Invoke(component);
                }
        }

        /* Systems */
        public T AddSystem<T>() where T : IGameSystem, new()
        {
            IGameSystem system;

            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                // get it from root object.
                system = (IGameSystem)root.AddComponent(typeof(T));

                system.world = this;
            }
            else
            {
                system = new T { world = this };
            }

            systems.Add(system);
            if (system is IInitSystem) (system as IInitSystem).Init();

            return (T)system;
        }

        public bool RemoveSystem<T>() where T : IGameSystem
        {
            var system = GetSystem<T>();
            return systems.Remove(system);
        }

        public T GetSystem<T>() where T : IGameSystem
        {
            return (T)systems.Find(sys => sys is T);
        }

        public T[] GetSystems<T>() where T : IGameSystem
        {
            return systems.Where(sys => sys is T).Cast<T>().ToArray();
        }

        /* Entities */
        public Entity CreateEntity(string id)
        {
            var entity = GetEntity(id);
            if (entity != null)
            {
                Debug.LogWarning($"Error: Trying to create entity with same ID {id}");
                return entity;
            }

            entity = new Entity(id);
            entities.Add(entity.ID, entity);
            return entity;
        }

        public Entity CreateEntity()
        {
            var id = Guid.NewGuid().ToString();
            var entity = CreateEntity(id);
            return entity;
        }

        public Entity CreateEntity<T>() where T : Component, new()
        {
            var entity = CreateEntity();
            AddComponent<T>(entity);
            return entity;
        }

        public Entity CreateEntity<T>(T component) where T : Component, new()
        {
            var entity = CreateEntity();
            AddComponent(entity, component);
            return entity;
        }

        public Entity CreateEntity<T>(string id, T component) where T : Component, new()
        {
            var entity = CreateEntity(id);
            AddComponent(entity, component);
            return entity;
        }

        public bool HasEntity(string id)
        {
            return GetEntity(id) != null;
        }


        public Entity GetEntity(string id)
        {
            if (entities == null) return null;

            if (id == null) return null;

            if (!entities.ContainsKey(id)) return null;

            return entities[id];
        }

        public void RemoveEntity(string id)
        {
            var entity = GetEntity(id);
            RemoveEntity(entity);
        }

        public void RemoveEntity(Entity e)
        {
            // Remove all components
            for (var i = e.Components.Count - 1; i >= 0; --i)
            {
                var component = e.Components.Count > i ? e.Components[i] : null;

                if (component == null) continue;

                var type = components[component][e.ID].GetType();

                if (subscribedDestroyCallbacks.ContainsKey(type))
                    for (var k = subscribedDestroyCallbacks[type].Count - 1; k >= 0; --k)
                        if (subscribedDestroyCallbacks[type].Count > k)
                            subscribedDestroyCallbacks[type][k].Invoke(components[component][e.ID]);

                components[component].Remove(e.ID);
            }

            // Remove the entity
            entities.Remove(e.ID);
        }

        public int GetEntityCount()
        {
            return entities.Count;
        }

        public T AddComponent<T>(Entity e) where T : Component, new()
        {
            var component = new T();

            return AddComponent(e, component);
        }

        public T AddComponent<T>(Entity e, T component) where T : Component, new()
        {
            return (T)AddComponent(e, component, typeof(T));
        }

        public Component AddComponent(Entity e, Component component, Type type)
        {
            var id = GetComponentID(type);
            component.entity = e.ID;

            e.Components.Add(id);

            if (!components.ContainsKey(id)) components.Add(id, new Dictionary<string, Component>());

            components[id].Add(e.ID, component);

            // Subscribe to property change
            component.SubscribePropertyChanged(OnPropertyChanged);

            if (subscribedCreateCallbacks.ContainsKey(type))
                foreach (var callback in subscribedCreateCallbacks[type])
                    callback?.Invoke(component);

            return component;
        }


        // Used for networking component removals.
        public bool RemoveComponent(Entity e, Type type)
        {
            if (e == null)
            {
                Debug.LogError("Trying to remove a component, but the entity is null!");
                return false;
            }

            if (!type.IsSubclassOf(typeof(Component)))
            {
                Debug.LogError("Trying to remove a component, but it is not a subclass of component!");
                return false;
            }

            var id = GetComponentID(type);

            if (!components.ContainsKey(id))
            {
                Debug.LogError("Trying to remove a component, but there are no components of that type!");
                return false;
            }

            if (!components[id].ContainsKey(e.ID))
            {
                Debug.LogError($"Trying to remove a component, but there is no component with entity ID {e.ID}!");
                return false;
            }

            var component = components[id][e.ID];

            if (subscribedDestroyCallbacks.ContainsKey(type))
                foreach (var callback in subscribedDestroyCallbacks[type])
                    callback?.Invoke(component);

            var success = components[id].Remove(e.ID);
            success &= e.Components.Remove(id);

            if (e.Components.Count == 0) RemoveEntity(e);

            return success;
        }

        public bool RemoveComponent<T>(string e) where T : Component
        {
            var entity = GetEntity(e);
            return RemoveComponent(entity, typeof(T));
        }

        public bool RemoveComponent<T>(Entity e) where T : Component
        {
            return RemoveComponent(e, typeof(T));
        }

        public void RemoveAll<T>() where T : Component
        {
            var components = GetComponents<T>();

            for (var i = components.Count - 1; i >= 0; --i) RemoveComponent<T>(GetEntity(components[i].entity));
        }

        public T GetComponent<T>() where T : Component
        {
            var id = GetComponentID<T>();

            if (components == null || !components.ContainsKey(id) || components[id].Count == 0) return null;

            using var enumerator = components[id].GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current.Value as T;
        }

        public T GetComponent<T>(string entityId) where T : Component
        {
            var entity = GetEntity(entityId);
            return GetComponent<T>(entity);
        }

        /// <summary>
        ///     Zero-allocation, GC-free component lookup that also checks subclasses.
        /// </summary>
        public T GetComponent<T>(Entity e) where T : Component
        {
            if (e == null) return null;

            // 1. Fast path – exact type
            if (TryGetComponentByID(e, GetComponentID<T>(), out T comp))
                return comp;

            // 2. Second pass – any concrete subclass of T
            foreach (var sub in GetOrCacheSubTypes(typeof(T)))
                if (TryGetComponentByID(e, GetComponentID(sub), out comp))
                    return comp;

            return null;
        }

        /// <summary> Maps a base component type to all of its concrete subclasses. </summary>
        private readonly Dictionary<Type, Type[]> _subTypeCache = new();

        /// <summary> One reflection scan per *base* type. </summary>
        private Type[] GetOrCacheSubTypes(Type baseType)
        {
            if (_subTypeCache.TryGetValue(baseType, out var cached))
                return cached;

            // Reflection scan – this runs only once for each base type
            var list = new List<Type>(32);
            foreach (var t in baseType.Assembly.GetTypes())
                if (t.IsSubclassOf(baseType) && !t.IsAbstract)
                    list.Add(t);

            var arr = list.ToArray();
            _subTypeCache[baseType] = arr;
            return arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetComponentByID<T>(Entity e, string id, out T comp) where T : Component
        {
            comp = null;

            if (components.TryGetValue(id, out var perEntity) &&
                perEntity.TryGetValue(e.ID, out var raw))
            {
                comp = (T)raw; // single cast, no boxing
                return true;
            }

            return false;
        }

        public List<T> GetComponents<T>() where T : Component
        {
            var id = GetComponentID<T>();

            if (components == null || !components.ContainsKey(id)) return new List<T>();

            // This could be optimized...
            return components[id].Values.OfType<T>().ToList();
        }

        // more performant but just returns raw components.
        public IEnumerable<Component> GetComponentsRaw<T>() where T : Component
        {
            var id = GetComponentID<T>();

            if (components == null || !components.ContainsKey(id)) return null;

            return components[id].Values;
        }

        private string GetComponentID<T>() where T : Component
        {
            return GetComponentID(typeof(T));
        }

        private string GetComponentID(Type type)
        {
            if (typeNameMap == null)
                return type.Name;

            if (!typeNameMap.ContainsKey(type))
                typeNameMap.Add(type, type.Name);

            return typeNameMap[type];
        }

        #endregion
    }
}