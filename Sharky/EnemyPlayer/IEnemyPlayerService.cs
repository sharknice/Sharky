namespace Sharky.EnemyPlayer
{
    public interface IEnemyPlayerService
    {
        List<EnemyPlayer> Enemies { get; }
        public Tournament Tournament { get; }
        void SaveGame(Game game);
    }
}
