using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

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

        public GameObject root { get; private set; }
        
        [SerializeField] [CanBeNull] private List<ScriptableObject> gameData;
        
        private List<IGameSystem> systems;
        private Dictionary<string, Dictionary<int, Component>> components;

        private Dictionary<int, Entity> entities;

        // This is used to cache subcomponents and speed up subsequent lookups when we are finding a child component with a parent class type.
        private Dictionary<Type, Type[]> subcomponentMap = new Dictionary<Type, Type[]>();

        private Dictionary<Type, List<Action<Component>>> subscribedUpdateCallbacks;
        private Dictionary<Type, List<Action<Component>>> subscribedCreateCallbacks;
        private Dictionary<Type, List<Action<Component>>> subscribedDestroyCallbacks;

        private List<Component> updateQueue;

        int entityID = 0;
        public bool isInitialized { get; set; }

        public T GetData<T>() where T : ScriptableObject
        {
            var data = gameData.Find(d => d is T);
            if (data == null)
            {
                Debug.LogError("Trying to get data of type " + typeof(T).Name + " but it does not exist in this world!");
                return null;
            }

            return data as T;
        }

        public List<T> GetAllData<T>() where T : ScriptableObject
        {
            var allData = gameData.FindAll(d => d is T);
            if (allData.Count == 0)
            {
                Debug.LogError("Trying to get data of type " + typeof(T).Name + " but it does not exist in this world!");
                return null;
            }

            return allData.ConvertAll(itr => itr as T);
        }
        
        #region Event Functions

        public void Create(GameObject rootObject)
        {
            root = rootObject;
            
            isInitialized = false;

            entityID = 0;

            systems = new List<IGameSystem>();
            components = new Dictionary<string, Dictionary<int, Component>>();
            entities = new Dictionary<int, Entity>();

            subscribedUpdateCallbacks = new Dictionary<Type, List<Action<Component>>>();
            subscribedDestroyCallbacks = new Dictionary<Type, List<Action<Component>>>();
            subscribedCreateCallbacks = new Dictionary<Type, List<Action<Component>>>();

            updateQueue = new List<Component>();
        }
        
        public async Task<bool> InitSystems()
        {
            isInitialized = false;
            // init the async systems
            for (int i = 0; i < systems.Count; ++i)
            {
                var system = systems[i];

                if (system is IAsyncInitSystem)
                {
                    var success = await (system as IAsyncInitSystem).InitAsync();
                    if (!success) return false;
                }
            }
            
            // call post init on all the systems.
            for (int i = 0; i < systems.Count; ++i)
            {
                var system = systems[i];
                if (system is IPostInitSystem)
                {
                    (system as IPostInitSystem).PostInit();
                }
            }

            // We are done initialization
            isInitialized = true;

            return true;
        }

        // This must be called by a MonoBehaviour in an update loop to properly update systems
        // todo: Do we want to hook this up to render from photon instead?
        public void Update()
        {
            // only run this once intiialized
            if (isInitialized)
            {
                // Update all the systems.
                for (int i = 0; i < systems.Count; ++i)
                {
                    var system = systems[i];

                    if (system is IUpdateSystem)
                    {
                        (system as IUpdateSystem).Update();
                    }
                }
            }

            for (int i = 0; i < updateQueue.Count; ++i)
            {
                SendComponentUpdate(updateQueue[i]);
            }

            updateQueue.Clear();
        }

        // This must be called by a MonoBehaviour in an update loop to properly update systems
        public void FixedUpdate()
        {
            // only run this once intiialized
            if (isInitialized)
            {
                // Update all the systems.
                for (int i = 0; i < systems.Count; ++i)
                {
                    var system = systems[i];

                    if (system is IFixedUpdateSystem)
                    {
                        (system as IFixedUpdateSystem).FixedUpdate();
                    }
                }
            }
        }

        // This must be called by a MonoBehaviour in OnDestroy to properly destroy systems.
        public void Teardown()
        {
            for (int i = 0; i < systems.Count; ++i)
            {
                if (systems[i] is IDestroySystem)
                {
                    (systems[i] as IDestroySystem).Destroy();
                }
            }

            var keys = entities.Keys.ToList();
            for (int i = entities.Count - 1; i >= 0; --i)
            {
                RemoveEntity(entities[keys[i]]);
            }

            components = new Dictionary<string, Dictionary<int, Component>>();
            entities = new Dictionary<int, Entity>();
        }

        #endregion

        #region Callbacks

        public Action<Component> SubscribeCreate<T>(Action<T> callback) where T : Component
        {
            if (subscribedCreateCallbacks == null) return null;

            if (!subscribedCreateCallbacks.ContainsKey(typeof(T)))
            {
                subscribedCreateCallbacks.Add(typeof(T), new List<Action<Component>>());
            }

            Action<Component> boundCallback = (comp) => { callback((T)comp); };

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
            {
                subscribedUpdateCallbacks.Add(typeof(T), new List<Action<Component>>());
            }

            Action<Component> boundCallback = (comp) => { callback((T)comp); };

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
            {
                subscribedDestroyCallbacks.Add(typeof(T), new List<Action<Component>>());
            }

            Action<Component> boundCallback = (comp) => { callback((T)comp); };

            subscribedDestroyCallbacks[typeof(T)].Add(boundCallback);

            return boundCallback;
        }

        public void UnsubscribeDestroy<T>(Action<Component> callback) where T : Component
        {
            if (subscribedDestroyCallbacks == null  || !subscribedDestroyCallbacks.ContainsKey(typeof(T)) ||
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
            var comps = GetComponents<T>();
            return comps.Find(predicate);
        }

        public List<T> FindAll<T>(Predicate<T> predicate) where T : Component
        {
            var comps = GetComponents<T>();
            return comps.FindAll(predicate);
        }

        /* Iterators */
        public void ForEach<T>(Action<T> callback) where T : Component
        {
            var id = GetComponentID<T>();

            if (!components.ContainsKey(id))
            {
                Debug.LogWarning(string.Format(
                    "Trying to iterate over components of type {0} but there are none in this world!", typeof(T).Name));
                return;
            }

            foreach (var c in components[id].Values)
            {
                callback(c as T);
            }
        }

        public void ForEach<T, K>(Action<T, K> callback) where T : Component where K : Component
        {
            var ids = new string[] { GetComponentID<T>(), GetComponentID<K>() };

            foreach (var e in entities.Values)
            {
                if (e.Components.Contains(ids[0]) && e.Components.Contains(ids[1]))
                {
                    var c1 = GetComponent<T>(e);
                    var c2 = GetComponent<K>(e);
                    callback(c1, c2);
                }
            }
        }

        public void ForEach<T, K, R>(Action<T, K, R> callback)
            where T : Component where K : Component where R : Component
        {
            var ids = new string[] { GetComponentID<T>(), GetComponentID<K>(), GetComponentID<R>() };

            foreach (var e in entities.Values)
            {
                if (e.Components.Contains(ids[0]) && e.Components.Contains(ids[1]) && e.Components.Contains(ids[2]))
                {
                    var c1 = GetComponent<T>(e);
                    var c2 = GetComponent<K>(e);
                    var c3 = GetComponent<R>(e);
                    callback(c1, c2, c3);
                }
            }
        }
        #endregion

        #region Private Fields

        private void OnPropertyChanged(Type type, int entity)
        {
            var id = GetComponentID(type);

            // Get the component
            var component = components[id][entity];

            if (!updateQueue.Contains(component))
            {
                updateQueue.Add(component);
            }
        }

        private void SendComponentUpdate(Component component)
        {
            var type = component.GetType();

            if (subscribedUpdateCallbacks.ContainsKey(type))
            {
                for (int i = subscribedUpdateCallbacks[type].Count - 1; i >= 0; --i)
                {
                    var callback = subscribedUpdateCallbacks[type][i];

                    callback?.Invoke(component);
                }
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
            if (system is IInitSystem)
            {
                (system as IInitSystem).Init();
            }

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
        public Entity CreateEntity(int id)
        {
            var entity = new Entity(id);
            entities.Add(entity.ID, entity);
            return entity;
        }

        public Entity CreateEntity()
        {
            // Take care of occupied entity slots from networking
            while (GetEntity(entityID) != null) entityID++;
            var entity = CreateEntity(entityID);
            entityID++;
            return entity;
        }

        public Entity CreateEntity<T>() where T : Component, new()
        {
            var entity = CreateEntity();
            AddComponent<T>(entity);
            return entity;
        }

        public Entity CreateEntity<T, K>() where T : Component, new() where K : Component, new()
        {
            var entity = CreateEntity();
            AddComponent<T>(entity);
            AddComponent<K>(entity);
            return entity;
        }

        public Entity CreateEntity<T>(T component) where T : Component, new()
        {
            var entity = CreateEntity();
            AddComponent<T>(entity, component);
            return entity;
        }

        public Entity CreateEntity<T, K>(T tComp, K kComp) where T : Component, new() where K : Component, new()
        {
            var entity = CreateEntity();
            AddComponent<T>(entity, tComp);
            AddComponent<K>(entity, kComp);
            return entity;
        }

        public bool HasEntity(int id)
        {
            return GetEntity(id) != null;
        }


        public Entity GetEntity(int id)
        {
            return entities != null && entities.ContainsKey(id) ? entities[id] : null;
        }

        public void RemoveEntity(int id)
        {
            var entity = GetEntity(id);
            RemoveEntity(entity);
        }

        public void RemoveEntity(Entity e)
        {
            // Remove all components
            for (int i = e.Components.Count - 1; i >= 0; --i)
            {
                var component = e.Components.Count > i ? e.Components[i] : null;

                if (component == null) continue;

                var type = components[component][e.ID].GetType();

                if (subscribedDestroyCallbacks.ContainsKey(type))
                {
                    for (int k = subscribedDestroyCallbacks[type].Count - 1; k >= 0; --k)
                    {
                        if (subscribedDestroyCallbacks[type].Count > k)
                        {
                            subscribedDestroyCallbacks[type][k].Invoke(components[component][e.ID]);
                        }
                    }
                }

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

            return AddComponent<T>(e, component);
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

            if (!components.ContainsKey(id))
            {
                components.Add(id, new Dictionary<int, Component>());
            }

            components[id].Add(e.ID, component);

            // Subscribe to property change
            component.SubscribePropertyChanged(OnPropertyChanged);

            if (subscribedCreateCallbacks.ContainsKey(type))
            {
                foreach (var callback in subscribedCreateCallbacks[type])
                {
                    callback?.Invoke(component);
                }
            }

            return component;
        }

        // Used for networking component removals.
        public bool RemoveComponent(Entity e, Type type)
        {
            if (!type.IsSubclassOf(typeof(Component)))
            {
                Debug.LogError("Trying to remove a component, but it is not a subclass of component!");
                return false;
            }

            var id = GetComponentID(type);

            var component = components[id][e.ID];

            if (subscribedDestroyCallbacks.ContainsKey(type))
            {
                foreach (var callback in subscribedDestroyCallbacks[type])
                {
                    callback?.Invoke(component);
                }
            }

            var success = components[id].Remove(e.ID);
            success &= e.Components.Remove(id);

            return success;
        }

        public bool RemoveComponent<T>(Entity e) where T : Component
        {
            return RemoveComponent(e, typeof(T));
        }

        public void RemoveAll<T>() where T : Component
        {
            var components = GetComponents<T>();

            for (int i = components.Count - 1; i >= 0; --i)
            {
                RemoveComponent<T>(GetEntity(components[i].entity));
            }
        }

        public T GetComponent<T>() where T : Component
        {
            var id = GetComponentID<T>();

            if (components == null || !components.ContainsKey(id) || components[id].Count == 0) return null;

            return components[id].First().Value as T;
        }

        public T GetComponent<T>(int entityId) where T : Component
        {
            var entity = GetEntity(entityId);
            return GetComponent<T>(entity);
        }

        public T GetComponent<T>(Entity e) where T : Component
        {
            if (e == null) return null;
            
            var id = GetComponentID<T>();

            var comp = GetComponentByID<T>(e, id);

            if (comp != null)
            {
                return comp;
            }

            // If we don't find the component type by direct class, check children.
            var parentType = typeof(T);

            if (!subcomponentMap.ContainsKey(parentType))
            {
                var assembly = Assembly.GetExecutingAssembly();

                var types = assembly.GetTypes();
                subcomponentMap.Add(parentType, types.Where(t => t.IsSubclassOf(parentType)).ToArray());
            }

            var subclasses = subcomponentMap[parentType];

            foreach (var type in subclasses)
            {
                var subId = GetComponentID(type);
                var subComp = GetComponentByID<T>(e, subId);
                if (subComp != null)
                {
                    return subComp;
                }
            }

            return null;
        }

        private T GetComponentByID<T>(Entity e, string id) where T : Component
        {
            return e != null && e.Components.Contains(id) ? components[id][e.ID] as T : null;
        }

        public List<T> GetComponents<T>() where T : Component
        {
            var id = GetComponentID<T>();

            if (components == null || !components.ContainsKey(id)) return new List<T>();

            // This could be optimized...
            return components[id].Values.ToList().ConvertAll(comp => comp as T);
        }

        private string GetComponentID<T>() where T : Component
        {
            return GetComponentID(typeof(T));
        }

        private string GetComponentID(Type type)
        {
            return type.Name;
        }

        #endregion

    }
}