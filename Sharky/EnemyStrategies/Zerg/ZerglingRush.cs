using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingRush : EnemyStrategy
    {
        public ZerglingRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            var lingCount = UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING);

            if (lingCount == 0)
            {
                return false;
            }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.Unit.UnitType == (uint)UnitTypes.ZERG_SPAWNINGPOOL && e.Unit.BuildProgress > .5) && frame < SharkyOptions.FramesPerSecond * 1 * 60)
            {
                return true;
            }

            if (UnitCountService.EnemyCompleted(UnitTypes.ZERG_SPAWNINGPOOL) > 0 && frame < SharkyOptions.FramesPerSecond * 1.25 * 60)
            {
                return true;
            }

            if (lingCount > 6 && frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 4 * 60 && lingCount >= 6 && UnitCountService.EnemyCount(UnitTypes.ZERG_EXTRACTOR) <= 1 && UnitCountService.EnemyCount(UnitTypes.ZERG_ROACHWARREN) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
