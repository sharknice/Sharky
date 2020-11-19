namespace Sharky.EnemyStrategies
{
    public interface IEnemyStrategy
    {
        string Name();
        void OnFrame(int frame);
        bool Active { get; }
        bool Detected { get; }
    }
}
