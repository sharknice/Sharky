using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class EnemyRaceManager : SharkyManager
    {
        public Race EnemyRace { get; private set; }

        IUnitManager UnitManager;
        UnitDataManager UnitDataManager;

        public EnemyRaceManager(IUnitManager unitManager, UnitDataManager unitDataManager)
        {
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId != playerId)
                {
                    EnemyRace = playerInfo.RaceRequested;
                }
            }
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            if (EnemyRace == Race.Random)
            {
                if (UnitManager.EnemyUnits.Any(e => UnitDataManager.ProtossTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyRace = Race.Protoss;
                }
                else if (UnitManager.EnemyUnits.Any(e => UnitDataManager.TerranTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyRace = Race.Terran;
                }
                else if (UnitManager.EnemyUnits.Any(e => UnitDataManager.ZergTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyRace = Race.Zerg;
                }
            }

            return new List<Action>();
        }
    }
}
