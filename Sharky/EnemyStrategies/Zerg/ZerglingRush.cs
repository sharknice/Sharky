using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingRush : EnemyStrategy
    {
        EnemyData EnemyData;

        public ZerglingRush(DefaultSharkyBot defaultSharkyBot)
        {
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            ChatService = defaultSharkyBot.ChatService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            DebugService = defaultSharkyBot.DebugService;
            UnitCountService = defaultSharkyBot.UnitCountService;

            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;

            EnemyData = defaultSharkyBot.EnemyData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.ZERG_ZERGLING))
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

            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 4 && frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                return true;
            }

            if (frame < SharkyOptions.FramesPerSecond * 4 * 60 && UnitCountService.EnemyCount(UnitTypes.ZERG_ZERGLING) >= 6 && UnitCountService.EnemyCount(UnitTypes.ZERG_EXTRACTOR) <= 1 && UnitCountService.EnemyCount(UnitTypes.ZERG_ROACHWARREN) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
