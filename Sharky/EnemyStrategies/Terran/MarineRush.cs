namespace Sharky.EnemyStrategies.Terran
{
    public class MarineRush : EnemyStrategy
    {
        BaseData BaseData;
        MapDataService MapDataService;

        public MarineRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        {
            BaseData = defaultSharkyBot.BaseData;
            MapDataService = defaultSharkyBot.MapDataService;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != Race.Terran) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 4 * 60 || UnitCountService.EnemyCount(UnitTypes.TERRAN_REFINERY) > 0 || UnitCountService.EnemyCount(UnitTypes.TERRAN_FACTORY) > 0)
            {
                return false;
            }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ResourceCenter) && e.Unit.Pos.X == BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().Location.X && e.Unit.Pos.Y == BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().Location.Y))
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKS) >= 3 && frame < SharkyOptions.FramesPerSecond * 3 * 60 && MapDataService.SelfVisible(BaseData.BaseLocations.FirstOrDefault().Location.ToVector2(), 5))
            {
                return true;
            }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.TERRAN_MARINE) || UnitCountService.EnemyCount(UnitTypes.TERRAN_MARINE) < 2)
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_MARINE) >= 8 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }
            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKS) >= 3 && frame < SharkyOptions.FramesPerSecond * 4 * 60)
            {
                return true;
            }
            if (frame < SharkyOptions.FramesPerSecond * 5 * 60 && UnitCountService.EnemyCount(UnitTypes.TERRAN_MARINE) >= 5 && UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKS) >= 2 && UnitCountService.EnemyCount(UnitTypes.TERRAN_MARAUDER) == 0 && UnitCountService.EnemyCount(UnitTypes.TERRAN_REAPER) == 0 && UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKSREACTOR) == 0 && UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKSTECHLAB) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
