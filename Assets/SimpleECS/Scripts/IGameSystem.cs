using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Threading.Tasks;

namespace SimpleECS
{
    public interface IGameSystem
    {
        public World world { get; set; }
    }
    
    public interface IInitSystem
    {
        void Init();
    }

    public interface IPostInitSystem
    {
        void PostInit();
    }
    
    public interface IUpdateSystem
    {
        void Update();
    }

    public interface IFixedUpdateSystem
    {
        void FixedUpdate();
    }
    
    public interface IDestroySystem
    {
        void Destroy();
    }

    public interface IAsyncInitSystem
    {
        Task<bool> InitAsync();
    }
}