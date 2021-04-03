using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class ReaperMicroController : IndividualMicroController
    {
        float Kd8Charge;

        public ReaperMicroController(MapDataService mapDataService, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, sharkyUnitData, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
            Kd8Charge = 5;
        }

        // TODO: use offensiveability when retreating

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Health < 10 || (commander.UnitCalculation.Unit.Health < 30 && commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageGround && e.Damage > commander.UnitCalculation.Unit.Health)))
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            if (commander.UnitCalculation.Unit.Health < 10 && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)))
            {
                action = Retreat(commander, defensivePoint, groupCenter, frame);
                return true;
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

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_KD8CHARGE)) { return true; }

            if (commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_KD8CHARGE)) { return false;  } // don't spam them all at once

            if (bestTarget != null && bestTarget.Unit.Tag != commander.UnitCalculation.Unit.Tag && bestTarget.FrameLastSeen == frame && !bestTarget.Attributes.Contains(Attribute.Structure) && commander.AbilityOffCooldown(Abilities.EFFECT_KD8CHARGE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                var distanceSqaured = Vector2.DistanceSquared(commander.UnitCalculation.Position, bestTarget.Position); // TODO: use unit velocity to predict where to place the charge and check if in range of that

                if (distanceSqaured <= 100)
                {
                    var enemyPosition = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
                    if (bestTarget.Velocity > 0)
                    {
                        var futurePosition = bestTarget.Position + (bestTarget.AverageVector * (bestTarget.AverageVelocity * SharkyOptions.FramesPerSecond));
                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, futurePosition) < Kd8Charge * Kd8Charge)
                        {
                            var interceptionPoint = new Point2D { X = futurePosition.X, Y = futurePosition.Y };
                            action = commander.Order(frame, Abilities.EFFECT_KD8CHARGE, interceptionPoint);
                            return true;
                        }
                    }
                    else if (distanceSqaured < Kd8Charge * Kd8Charge)
                    {
                        var point = new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
                        action = commander.Order(frame, Abilities.EFFECT_KD8CHARGE, point);
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool GetHighGroundVision(UnitCommander commander, Point2D target, Point2D defensivePoint, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }
    }
}
