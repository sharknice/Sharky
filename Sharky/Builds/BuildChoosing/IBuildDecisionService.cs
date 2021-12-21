using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.Builds.BuildChoosing
{
    public interface IBuildDecisionService
    {
        List<string> GetBestBuild(EnemyPlayer.EnemyPlayer enemyBot, List<List<string>> buildSequences, string map, List<EnemyPlayer.EnemyPlayer> enemyBots, Race enemyRace, Race myRace);
    }
}
