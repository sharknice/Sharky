using Sharky.Chat;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Terran
{
    public class BunkerRush : EnemyStrategy
    {
        TargetingData TargetingData;

        public BunkerRush(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, TargetingData targetingData, DebugService debugService, UnitCountService unitCountService)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            TargetingData = targetingData;
            DebugService = debugService;
            UnitCountService = unitCountService;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 3)
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
