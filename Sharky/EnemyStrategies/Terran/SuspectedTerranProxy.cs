namespace Sharky.EnemyStrategies.Terran
{
    public class SuspectedTerranProxy : EnemyStrategy
    {
        MapDataService MapDataService;
        TargetingData TargetingData;

        public SuspectedTerranProxy(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        {
            MapDataService = defaultSharkyBot.MapDataService;
            TargetingData= defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 90)
            {
                return false;
            }
            if (frame > SharkyOptions.FramesPerSecond * 3 * 60)
            {
                return false;
            }

            if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_COMMANDCENTER) < 2 && MapDataService.SelfVisible(TargetingData.EnemyMainBasePoint))
            {
                if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_BARRACKS) < 1)
                {
                    if (UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_SUPPLYDEPOT) + UnitCountService.EnemyCount(UnitTypes.TERRAN_REFINERY) > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
