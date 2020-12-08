using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class DarkTemplarMicroController : IndividualMicroController
    {
        float ShadowStrikeRange = 8;

        public DarkTemplarMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, IUnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, IBaseManager baseManager, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, baseManager, sharkyOptions, microPriority, groupUpEnabled)
        {
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (bestTarget != null && UnitDataManager.ResearchedUpgrades.Contains((uint)Upgrades.DARKTEMPLARBLINKUPGRADE) && commander.AbilityOffCooldown(Abilities.EFFECT_SHADOWSTRIDE, frame, SharkyOptions.FramesPerSecond, UnitDataManager))
            {
                var distanceSqaured = Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(bestTarget.Unit.Pos.X, bestTarget.Unit.Pos.Y));

                if (distanceSqaured <= ShadowStrikeRange * ShadowStrikeRange && distanceSqaured > 9)
                {
                    var x = bestTarget.Unit.Radius * Math.Cos(bestTarget.Unit.Facing);
                    var y = bestTarget.Unit.Radius * Math.Sin(bestTarget.Unit.Facing);
                    var blinkPoint = new Point2D { X = bestTarget.Unit.Pos.X + (float)x, Y = bestTarget.Unit.Pos.Y - (float)y };

                    action = commander.Order(frame, Abilities.EFFECT_SHADOWSTRIDE, blinkPoint);
                    return true;
                }
            }

            return false;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (Detected(commander))
            {
                return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
            }
            return false;
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out SC2APIProtocol.Action action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            if (Detected(commander))
            {
                return base.AvoidDamage(commander, target, defensivePoint, frame, out action);
            }

            return false;
        }
    }
}
