namespace Sharky.EnemyStrategies.Protoss
{
    public class SuspectedFourGate : EnemyStrategy
    {
        MapDataService MapDataService;
        BaseData BaseData;

        public SuspectedFourGate(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        {
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss || !BaseData.EnemyBaseLocations.Any()) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 4 * 60) { return false; }

            if (UnitCountService.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) + UnitCountService.Count(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) + UnitCountService.Count(UnitTypes.PROTOSS_DARKSHRINE) > 0) { return false; }

            if (MapDataService.SelfVisible(BaseData.EnemyBaseLocations.FirstOrDefault().Location))
            {
                if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_NEXUS) > 1) { return false; }

                if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) >= 3 && UnitCountService.EnemyCount(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
                {
                    return true;
                }
            }    

            return false;
        }
    }
}
