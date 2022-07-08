using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Terran
{
    public class MarineRush : EnemyStrategy
    {
        public MarineRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame > SharkyOptions.FramesPerSecond * 4 * 60 || UnitCountService.EnemyCount(UnitTypes.TERRAN_REFINERY) > 0 || UnitCountService.EnemyCount(UnitTypes.TERRAN_FACTORY) > 0)
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_BARRACKS) >= 3 && frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                return true;
            }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.TERRAN_MARINE) || UnitCountService.EnemyCount(UnitTypes.TERRAN_MARINE) < 2)
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
