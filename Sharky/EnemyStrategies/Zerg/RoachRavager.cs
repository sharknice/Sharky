using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Zerg
{
    public class RoachRavager : EnemyStrategy
    {
        EnemyData EnemyData;

        public RoachRavager(DefaultSharkyBot defaultSharkyBot)
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

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.ZERG_ROACH && e.Unit.UnitType != (uint)UnitTypes.ZERG_RAVAGER && e.Unit.UnitType != (uint)UnitTypes.ZERG_RAVAGERCOCOON))
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.ZERG_ROACH) >= 2 && UnitCountService.EnemyCount(UnitTypes.ZERG_RAVAGER) + UnitCountService.EnemyCount(UnitTypes.ZERG_RAVAGERCOCOON) > 0)
            {
                return true;
            }

            return false;
        }
    }
}
