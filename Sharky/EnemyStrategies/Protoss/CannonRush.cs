using Sharky.Chat;
using Sharky.Managers;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Protoss
{
    public class CannonRush : EnemyStrategy
    {
        TargetingData TargetingData;

        public CannonRush(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, TargetingData targetingData, DebugManager debugManager, UnitCountService unitCountService)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            TargetingData = targetingData;
            DebugManager = debugManager;
            UnitCountService = unitCountService;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON && Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y)) < 900))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
