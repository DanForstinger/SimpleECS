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
        void UpdateSystem();
    }


    public interface IFixedUpdateSystem
    {
        void FixedUpdate();
    }

    public interface IDestroySystem
    {
        void Destroy();
    }
}