using SC2APIProtocol;
using Sharky.Chat;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class EnemyRaceManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        EnemyData EnemyData;

        SharkyOptions SharkyOptions;

        ChatService ChatService;

        public EnemyRaceManager(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, EnemyData enemyData, SharkyOptions sharkyOptions, ChatService chatService)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            EnemyData = enemyData;
            SharkyOptions = sharkyOptions;
            ChatService = chatService;
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
                if (ActiveUnitData.EnemyUnits.Any(e => SharkyUnitData.ProtossTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyData.EnemyRace = Race.Protoss;
                    TagRace();
                }
                else if (ActiveUnitData.EnemyUnits.Any(e => SharkyUnitData.TerranTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyData.EnemyRace = Race.Terran;
                    TagRace();
                }
                else if (ActiveUnitData.EnemyUnits.Any(e => SharkyUnitData.ZergTypes.Contains((UnitTypes)e.Value.Unit.UnitType)))
                {
                    EnemyData.EnemyRace = Race.Zerg;
                    TagRace();
                }
            }

            return new List<Action>();
        }

        void TagRace()
        {
            if (SharkyOptions.TagsEnabled)
            {
                ChatService.SendAllyChatMessage($"Tag:EnemyRandomRace-{EnemyData.EnemyRace}");
            }
        }
    }
}
