using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleECS;

public class GameRunner : MonoBehaviour
{
    [SerializeField] private World world;
    [SerializeField] private TestView testView;
    
    async void Awake()
    {
        world.Create(gameObject);
        
        // add systems to the world!
        world.AddSystem<TestSystem>();

        // initialize the world.
        var success = await world.InitSystems();

        if (success)
        {
            Debug.Log("World initialized successfully!");
        }
        else
        {
            Debug.LogError("Error: World not initialized successfully!");
        }
    }

    void Start()
    {
        // setup our test component.
        var entity = world.CreateEntity();
        var component = world.AddComponent(entity, new TestComponent { testValue = 99 });
        
        testView.BindComponent(component, world);
    }

    private void Update()
    {
        if (!world.isInitialized)
        {
            return;
        }
        
        // update the world!
        world.Update();
    }

    private void OnDestroy()
    {
        if (!world.isInitialized)
        {
            return;
        }

        world.Teardown();
    }
}
