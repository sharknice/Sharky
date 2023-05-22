using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers
{
    public class FlyingDetectorMicroController : IndividualMicroController
    {
        public FlyingDetectorMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            LooseFormationDistance = 10;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax)
            {
                var cloakedPosition = CloakedInvader(commander);
                if (cloakedPosition != null)
                {
                    action = commander.Order(frame, Abilities.MOVE, cloakedPosition);
                    return true;
                }
            }

            if (AttackData.Attacking && SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected Point2D CloakedInvader(UnitCommander commander)
        {
            var pos = commander.UnitCalculation.Position;

            if (commander.UnitRole != UnitRole.Defend)
            {
                var hiddenUnits = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.DisplayType == DisplayType.Hidden).OrderBy(e => Vector2.DistanceSquared(pos, e.Position));
                var hiddenByAllies = hiddenUnits.FirstOrDefault(e => e.NearbyEnemies.Any(a => !a.Unit.IsHallucination && DamageService.CanDamage(a, e)));
                if (hiddenByAllies != null)
                {
                    return new Point2D { X = hiddenByAllies.Unit.Pos.X, Y = hiddenByAllies.Unit.Pos.Y };
                }
            }

            var unit = ActiveUnitData.SelfUnits.Values.Where(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS).SelectMany(a => a.NearbyEnemies).Where(e => SharkyUnitData.CloakableAttackers.Contains((UnitTypes)e.Unit.UnitType) && !e.Unit.BuffIds.Contains((uint)Buffs.ORACLEREVELATION)).OrderBy(e => Vector2.DistanceSquared(pos, e.Position)).FirstOrDefault();
            if (unit != null)
            {
                return new Point2D { X = unit.Unit.Pos.X, Y = unit.Unit.Pos.Y };
            }

            return null;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return false;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (PreOffenseOrder(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out action)) { return action; }

            if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }

            return Idle(commander, defensivePoint, frame);
        }

        protected bool StayAwayFromSameUnitTypes(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var otherDetector = commander.UnitCalculation.NearbyAllies.Take(25).Where(a => a.Unit.UnitType == commander.UnitCalculation.Unit.UnitType).OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
            if (otherDetector != null)
            {
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, otherDetector.Position) < LooseFormationDistance * LooseFormationDistance)
                {
                    var avoidPoint = GetPositionFromRange(commander, otherDetector.Unit.Pos, commander.UnitCalculation.Unit.Pos, LooseFormationDistance + 2);
                    action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                    return true;
                }
            }
            return false;
        }

        public bool SupportArmy(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, out List<SC2APIProtocol.Action> action, IEnumerable<UnitCalculation> supportableUnits = null)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax / 2)
            {
                if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (commander.UnitCalculation.Unit.Shield < 1)
                {
                    if (Retreat(commander, target, defensivePoint, frame, out action))
                    {
                        return true;
                    }
                }
            }

            if (StayAwayFromSameUnitTypes(commander, frame, out action))
            {
                return true;
            }

            var unitToSupport = GetSupportTarget(commander, target, defensivePoint, supportableUnits);
            if (unitToSupport == null)
            {
                return false;
            }

            if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.Tag == unitToSupport.Unit.Tag))
            {
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(target.X, target.Y)) > Vector2.DistanceSquared(unitToSupport.Position, new Vector2(target.X, target.Y)))
                {
                    if (NavigateToSupportUnit(commander, target, frame, out action))
                    {
                        return true;
                    }

                }
            }

            var moveTo = new Point2D { X = unitToSupport.Unit.Pos.X, Y = unitToSupport.Unit.Pos.Y };

            if (!InRange(new Vector2(moveTo.X, moveTo.Y), commander.UnitCalculation.Position, 2))
            {
                action = commander.Order(frame, Abilities.MOVE, moveTo);
                return true;
            }

            return false;
        }

        protected bool NavigateToSupportUnit(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AvoidDeceleration(commander, target, false, frame, out action)) { return true; }
            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        protected UnitCalculation GetSupportTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, IEnumerable<UnitCalculation> supportableUnits = null)
        {
            if (supportableUnits == null)
            {
                supportableUnits = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType != commander.UnitCalculation.Unit.UnitType && u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Unit.IsHallucination);
            }

            // no allies that already have a friendly observer within 4 range
            var otherDetectors = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.Tag != commander.UnitCalculation.Unit.Tag && (u.Unit.UnitType == commander.UnitCalculation.Unit.UnitType || u.UnitClassifications.Contains(UnitClassification.DetectionCaster) || u.UnitClassifications.Contains(UnitClassification.Detector)));

            var friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit)
                && !otherDetectors.Any(o => DistanceSquared(o, u) < 16)
                    && Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position) < 225
                    && u.NearbyEnemies.Any(e => DistanceSquared(u, e) < 225)
                ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).First(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if none
            // get any allies
            // select the friendies with enemies in 15 range
            // order by closest to the enemy
            friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit)
                            && !otherDetectors.Any(o => DistanceSquared(o, u) < 64)
                                && u.NearbyEnemies.Any(e => DistanceSquared(u, e) < 225)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target
            friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit)
                            && !otherDetectors.Any(o => DistanceSquared(o, u) < 64)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target even if there is another detector nearby
            friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            return null;
        }

        protected float DistanceSquared(UnitCalculation unit1, UnitCalculation unit2)
        {
            if (unit1 == null || unit2 == null)
            {
                return 0;
            }
            return Vector2.DistanceSquared(unit1.Position, unit2.Position);
        }
    }
}
