using Sharky.Chat;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Terran
{
    public class BunkerContain : EnemyStrategy
    {
        TargetingData TargetingData;

        public BunkerContain(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, TargetingData targetingData, DebugService debugService, UnitCountService unitCountService, FrameToTimeConverter frameToTimeConverter)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            TargetingData = targetingData;
            DebugService = debugService;
            UnitCountService = unitCountService;
            FrameToTimeConverter = frameToTimeConverter;
        }

        protected override bool Detect(int frame)
        {
            if (frame >= SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), u.Position) < 900))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
