using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class ReaperMicroController : IndividualMicroController
    {
        float Kd8Charge;

        public ReaperMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
            Kd8Charge = 5;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Health < 10)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            if (commander.UnitCalculation.Unit.Health == commander.UnitCalculation.Unit.HealthMax)
            {
                commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
            }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (bestTarget != null && commander.AbilityOffCooldown(Abilities.EFFECT_KD8CHARGE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                var distanceSqaured = Vector2.DistanceSquared(new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y), new Vector2(bestTarget.Unit.Pos.X, bestTarget.Unit.Pos.Y));

                if (distanceSqaured <= Kd8Charge * Kd8Charge)
                {
                    var x = bestTarget.Unit.Radius * Math.Cos(bestTarget.Unit.Facing);
                    var y = bestTarget.Unit.Radius * Math.Sin(bestTarget.Unit.Facing);
                    var blinkPoint = new Point2D { X = bestTarget.Unit.Pos.X + (float)x, Y = bestTarget.Unit.Pos.Y - (float)y };

                    action = commander.Order(frame, Abilities.EFFECT_KD8CHARGE, blinkPoint);
                    return true;
                }
            }

            return false;
        }
    }
}
