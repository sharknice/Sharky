using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class CycloneMicroController : IndividualMicroController
    {
        public CycloneMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.CommanderState == CommanderState.MaintainLockon)
            {
                var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.Tag == commander.LastLockOn.Tag);
                if (enemy != null)
                {
                    var range = 10f; // stay in sight range
                    if (enemy.EnemiesInRangeOf.Count() > 1)
                    {
                        range = 14f; // other friendlies spotting
                    }
                    var avoidPoint = GetPositionFromRange(commander, enemy.Unit.Pos, commander.UnitCalculation.Unit.Pos, range);
                    // TODO: make sure the avoidpoint is same height or higher than enemy position so vision isn't lost
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.LastLockOn == null || (frame - commander.LastLockOn.EndFrame) > (4.3 * SharkyOptions.FramesPerSecond))
            {
                if (bestTarget != null && Vector2.DistanceSquared(bestTarget.Position, commander.UnitCalculation.Position) <= 49)
                {
                    action = commander.Order(frame, Abilities.EFFECT_LOCKON, targetTag: bestTarget.Unit.Tag);
                    commander.LastLockOn = new LockOnData { StartFrame = frame, Tag = bestTarget.Unit.Tag, EndFrame = frame + (int)(14.3 * SharkyOptions.FramesPerSecond) };
                    return true;
                }
            }
            
            return false;
        }

        protected override void UpdateState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {
            if (commander.LastLockOn != null && commander.LastLockOn.EndFrame > frame)
            {
                var enemy = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.Tag == commander.LastLockOn.Tag);
                if (enemy == null)
                {
                    commander.LastLockOn.EndFrame = frame;
                }
                else
                {
                    if (!enemy.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
                    {
                        commander.LastLockOn.EndFrame = frame;
                    }
                }

                if (commander.CommanderState == CommanderState.MaintainLockon && commander.LastLockOn.EndFrame <= frame)
                {
                    commander.CommanderState = CommanderState.None;
                }
                else if (commander.CommanderState != CommanderState.MaintainLockon && commander.LastLockOn.EndFrame > frame)
                {
                    commander.CommanderState = CommanderState.MaintainLockon;
                }
            }
        }
    }
}
