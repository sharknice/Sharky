namespace Sharky.EnemyStrategies.Protoss
{
    public class SuspectedProtossProxy : EnemyStrategy
    {
        MapDataService MapDataService { get; set; }
        BaseData BaseData { get; set; }
        public SuspectedProtossProxy(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            MapDataService = defaultSharkyBot.MapDataService;
            BaseData = defaultSharkyBot.BaseData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss || !BaseData.EnemyBaseLocations.Any()) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 1.5 * 60)
            {
                if (MapDataService.SelfVisible(BaseData.EnemyBaseLocations.FirstOrDefault().Location))
                {
                    if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_PYLON) == 0 && UnitCountService.EnemyCount(UnitTypes.PROTOSS_GATEWAY) == 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            if (frame > SharkyOptions.FramesPerSecond * 5 * 60)
            {
                return false;
            }

            if (MapDataService.SelfVisible(BaseData.EnemyBaseLocations.FirstOrDefault().Location)) 
            { 
                return false; 
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_NEXUS) == 1 && UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_GATEWAY) + UnitCountService.EnemyCount(UnitTypes.PROTOSS_CYBERNETICSCORE) + UnitCountService.EnemyCount(UnitTypes.PROTOSS_FORGE) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
