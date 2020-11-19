using System.Collections.Generic;

namespace Sharky.EnemyPlayer
{
    public interface IEnemyNameService
    {
        string GetNameFromGame(Game game, List<EnemyPlayer> enemies);
        string GetEnemyNameFromId(string id, List<EnemyPlayer> enemies);
        string GetNameFromChat(string chat, List<EnemyPlayer> enemies);
    }
}
