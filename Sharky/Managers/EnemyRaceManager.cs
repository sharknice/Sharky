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
                    EnemyData.EnemyRaceRequested = playerInfo.RaceRequested;
                }
                else
                {
                    EnemyData.SelfRace = playerInfo.RaceActual;
                    EnemyData.SelfRaceRequested = playerInfo.RaceRequested;
                }
            }
        }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
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

            return new List<SC2Action>();
        }

        void TagRace()
        {
            ChatService.Tag($"EnemyRandomRace_{EnemyData.EnemyRace}");
        }
    }
}
