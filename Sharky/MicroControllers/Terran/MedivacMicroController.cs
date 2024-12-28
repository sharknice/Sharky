namespace Sharky.MicroControllers.Terran
{
    public class MedivacMicroController : IndividualMicroController
    {
        public MedivacMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (IgniteAfterburners(commander, frame, bestTarget, out action))
            {
                return true;
            }

            return false;
        }

        bool IgniteAfterburners(UnitCommander commander, int frame, UnitCalculation bestTarget, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.AbilityOffCooldown(Abilities.EFFECT_MEDIVACIGNITEAFTERBURNERS, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                TagService.TagAbility("afterburner");
                action = commander.Order(frame, Abilities.EFFECT_MEDIVACIGNITEAFTERBURNERS);
                return true;
            }

            return false;
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }


        public override bool WeaponReady(UnitCommander commander, int frame)
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

            if (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 2)
            {
                if (AvoidTargetedDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }

                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
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
                action = commander.Order(frame, Abilities.ATTACK, moveTo);
                return true;
            }

            return false;
        }

        protected bool NavigateToSupportUnit(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AvoidDeceleration(commander, target, false, frame, out action)) { return true; }
            action = commander.Order(frame, Abilities.ATTACK, target);
            return true;
        }

        protected UnitCalculation GetSupportTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, IEnumerable<UnitCalculation> supportableUnits = null)
        {
            if (supportableUnits == null)
            {
                supportableUnits = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.Health < a.Unit.HealthMax && a.Attributes.Contains(SC2Attribute.Biological));
                if (!supportableUnits.Any())
                {
                    supportableUnits = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType != commander.UnitCalculation.Unit.UnitType && u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !u.Unit.IsHallucination);
                }
            }

            var friendlies = supportableUnits.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && u.Attributes.Contains(SC2Attribute.Biological) && u.Unit.Health < u.Unit.HealthMax
                    && Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position) < 225
                    && u.NearbyEnemies.Any(e => DistanceSquared(u, e) < 225)
                ).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).First(), u));

            if (friendlies.Any())
            {
                return friendlies.First();
            }


            // if still none
            //get ally closest to target
            friendlies = supportableUnits.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)).OrderBy(u => DistanceSquared(u.NearbyEnemies.OrderBy(e => DistanceSquared(e, u)).FirstOrDefault(), u));

            if (friendlies.Any())
            {
                return friendlies.First();
            }


            if (friendlies.Any())
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
