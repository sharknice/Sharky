namespace Sharky.MicroControllers.Protoss
{
    public class MothershipMicroController : IndividualMicroController
    {
        int CloakRange = 5;
        int TimeWarpRange = 9;
        float TImeWarpRadius = 3.5f;

        public MothershipMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var unitToSupport = GetSupportTarget(commander, supportTargets, target, defensivePoint);

            if (unitToSupport == null)
            {
                unitToSupport = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitRole == UnitRole.Leader);
                if (unitToSupport == null)
                {
                    return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
                }
            }

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame) == 0 || !unitToSupport.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.CLOAKFIELDEFFECT))
            {
                return commander.Order(frame, Abilities.MOVE, new Point2D { X = unitToSupport.UnitCalculation.Position.X, Y = unitToSupport.UnitCalculation.Position.Y });
            }

            return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (Cloak(commander, frame, out action))
            {
                TagService.TagAbility("mothershipcloak");
                return true;
            }

            if (TimeWarp(commander, frame, out action))
            {
                TagService.TagAbility("timewarp");
                return true;
            }

            if (SupportArmy(commander, target, defensivePoint, groupCenter, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected bool Cloak(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AbilityOffCooldown(Abilities.MOTHERSHIPCLOAK, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            if (MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos))
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyAllies.Any(a => a.EnemiesInRangeOf.Any() && Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position) < 36))
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.MOTHERSHIPCLOAK);
                return true;
            }

            return false;
        }

        protected bool TimeWarp(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_TIMEWARP))
            {
                return true;
            }

            if (commander.LastAbility == Abilities.EFFECT_TIMEWARP && frame - 5 > commander.LastOrderFrame)
            {
                return true;
            }

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_TIMEWARP, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            var point = GetTimeWarpLocation(commander);

            if (point == null)
            {
                return false;
            }

            CameraManager.SetCamera(point);
            action = commander.Order(frame, Abilities.EFFECT_TIMEWARP, point);
            return true;
        }

        Point2D GetTimeWarpLocation(UnitCommander commander)
        {
            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.Damage > 0 && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < TimeWarpRange * TimeWarpRange);

            var damageCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in enemiesInRange)
            {
                float damageReduction = 0;
                foreach (var hitEnemy in enemiesInRange)
                {
                    if (!hitEnemy.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(hitEnemy.Position, enemyAttack.Position) <= (hitEnemy.Unit.Radius + TImeWarpRadius) * (hitEnemy.Unit.Radius + TImeWarpRadius))
                    {
                        damageReduction += hitEnemy.Dps;
                    }
                }
                damageCounts[enemyAttack.Unit.Pos] = damageReduction;
            }

            return GetBestTimeWarpLocation(damageCounts.OrderByDescending(x => x.Value));
        }

        Point2D GetBestTimeWarpLocation(IOrderedEnumerable<KeyValuePair<Point, float>> locations)
        {
            foreach (var location in locations)
            {
                if (location.Value < 50)
                {
                    continue;
                }

                var placement = new Point2D { X = location.Key.X, Y = location.Key.Y };
                bool good = true;
                if (!MapDataService.SelfVisible(placement))
                {
                    continue;
                }

                if (good)
                {
                    return placement;
                }
            }
            return null;
        }

        bool SupportArmy(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, out List<SC2APIProtocol.Action> action)
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


            // follow behind at the range of cloak field

            var unitToSupport = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitRole == UnitRole.Leader && c.UnitCalculation.EnemiesThreateningDamage.Any())?.UnitCalculation;

            if (unitToSupport == null)
            {
                unitToSupport = commander.UnitCalculation.NearbyAllies.Where(u => u.UnitClassifications.Contains(UnitClassification.ArmyUnit) && !u.Unit.IsHallucination && u.EnemiesInRangeOf.Any()).OrderByDescending(u => u.EnemiesInRangeOf.Count()).FirstOrDefault();

                if (unitToSupport == null)
                {
                    unitToSupport = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitRole == UnitRole.Leader)?.UnitCalculation;
                    if (unitToSupport == null)
                    {
                        return false;
                    }
                }
            }

            var moveTo = GetSupportSpot(commander, unitToSupport, target, defensivePoint);

            if (AvoidDeceleration(commander, moveTo, false, frame, out action)) { return true; }

            action = commander.Order(frame, Abilities.MOVE, moveTo);
            return true;
        }

        protected override Point2D GetSupportSpot(UnitCommander commander, UnitCalculation unitToSupport, Point2D target, Point2D defensivePoint)
        {
            var angle = Math.Atan2(unitToSupport.Position.Y - defensivePoint.Y, defensivePoint.X - unitToSupport.Position.X);
            var x = CloakRange * Math.Cos(angle);
            var y = CloakRange * Math.Sin(angle);
            return new Point2D { X = unitToSupport.Position.X + (float)x, Y = unitToSupport.Position.Y - (float)y };
        }

        protected override bool DoFreeDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }
    }
}
