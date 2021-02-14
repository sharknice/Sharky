using Sharky.Chat;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies.Protoss
{
    public class ProxyRobo : EnemyStrategy
    {
        TargetingData TargetingData;

        public ProxyRobo(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService, TargetingData targetingData)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
            UnitCountService = unitCountService;

            TargetingData = targetingData;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && u.Unit.UnitType == (uint)UnitTypes.PROTOSS_ROBOTICSFACILITY && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y)) > (75 * 75)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
