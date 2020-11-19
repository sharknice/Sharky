using Sharky.Managers;
using System;
using System.Linq;
using System.Numerics;

namespace Sharky.EnemyStrategies
{
    public class Proxy : EnemyStrategy
    {
        ITargetingManager TargetingManager;

        public Proxy(EnemyStrategyHistory enemyStrategyHistory, IChatManager chatManager, IUnitManager unitManager, SharkyOptions sharkyOptions, ITargetingManager targetingManager)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatManager = chatManager;
            UnitManager = unitManager;
            SharkyOptions = sharkyOptions;
            TargetingManager = targetingManager;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (UnitManager.EnemyUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(new Vector2(TargetingManager.EnemyMainBasePoint.X, TargetingManager.EnemyMainBasePoint.Y), new Vector2(u.Unit.Pos.X, u.Unit.Pos.Y)) > (100 * 100)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
