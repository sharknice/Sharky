using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Protoss
{
    public class AdeptRush : EnemyStrategy
    {
        public AdeptRush(DefaultSharkyBot defaultSharkyBot)
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
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_ADEPT))
            {
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_ADEPT) >= 4 && frame < SharkyOptions.FramesPerSecond * 5 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
