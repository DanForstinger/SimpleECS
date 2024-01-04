using SimpleECS;


public class TestSystem : IGameSystem, IInitSystem, IUpdateSystem
{
    public void Init()
    {
    }

    public void Update()
    {
    }

    public World world { get; set; }
}