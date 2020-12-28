using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class EnemyRaceManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        UnitDataManager UnitDataManager;
        EnemyData EnemyData;

        public EnemyRaceManager(ActiveUnitData activeUnitData, UnitDataManager unitDataManager, EnemyData enemyData)
        {
            ActiveUnitData = activeUnitData;
            UnitDataManager = unitDataManager;
            EnemyData = enemyData;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId != playerId)
                {
                    EnemyData.EnemyRace = playerInfo.RaceRequested;
                }
            }
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            if (EnemyData.EnemyRace == Race.Random)
            {
                if (ActiveUnitData.EnemyUnits.Any(e => UnitDataManager.ProtossTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyData.EnemyRace = Race.Protoss;
                }
                else if (ActiveUnitData.EnemyUnits.Any(e => UnitDataManager.TerranTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyData.EnemyRace = Race.Terran;
                }
                else if (ActiveUnitData.EnemyUnits.Any(e => UnitDataManager.ZergTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyData.EnemyRace = Race.Zerg;
                }
            }

            return new List<Action>();
        }
    }
}
