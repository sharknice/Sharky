using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class ObserverMicroController : IndividualMicroController
    {
        public ObserverMicroController(MapDataService mapDataService, SharkyUnitData unitDataManager, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
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

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }

        Point2D CloakedInvader(UnitCommander commander)
        {
            var pos = commander.UnitCalculation.Position;

            var hiddenUnits = ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.DisplayType == DisplayType.Hidden).OrderBy(e => Vector2.DistanceSquared(pos, e.Value.Position));
            if (hiddenUnits.Count() > 0)
            {
                return new Point2D { X = hiddenUnits.FirstOrDefault().Value.Unit.Pos.X, Y = hiddenUnits.FirstOrDefault().Value.Unit.Pos.Y };
            }

            return null;
        }

        protected override bool WeaponReady(UnitCommander commander)
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

        bool NavigateToSupportUnit(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = commander.Order(frame, Abilities.MOVE, target);
            return true;
        }

        UnitCalculation GetSupportTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, IEnumerable<UnitCalculation> supportableUnits = null)
        {
            if (supportableUnits == null)
            {
                supportableUnits = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType != (uint)UnitTypes.PROTOSS_OBSERVER && u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Unit.IsHallucination);
            }

            // no allies that already have a friendly observer within 4 range
            var otherObservers = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.Tag != commander.UnitCalculation.Unit.Tag && u.Unit.UnitType == (uint)UnitTypes.PROTOSS_OBSERVER);

            var friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit)
                && !otherObservers.Any(o => DistanceSquared(o, u) < 16)
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
                            && !otherObservers.Any(o => DistanceSquared(o, u) < 64)
                                && u.NearbyEnemies.Any(e => DistanceSquared(u, e) < 225)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target
            friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit) 
                            && !otherObservers.Any(o => DistanceSquared(o, u) < 64)
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            // if still none
            //get ally closest to target even if there is another observer nearby
            friendlies = supportableUnits.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit) 
                            ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Count() > 0)
            {
                return friendlies.First();
            }

            return null;
        }

        float DistanceSquared(UnitCalculation unit1, UnitCalculation unit2)
        {
            if (unit1 == null || unit2 == null)
            {
                return 0;
            }
            return Vector2.DistanceSquared(unit1.Position, unit2.Position);
        }
    }
}
