namespace Sharky.MicroControllers.Protoss
{
    public class PhoenixMicroController : IndividualMicroController
    {
        public PhoenixMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            FollowPathProximitySquared = 10;
        }

        public override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                action = commander.Order(frame, Abilities.MOVE, defensivePoint); // no damaging targets in range, retreat towards the defense point
                return true;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag && bestTarget.FrameLastSeen == frame) && MicroPriority != MicroPriority.NavigateToLocation)
            {
                action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                //action = commander.Order(frame, Abilities.MOVE, GetPositionFromRange(commander, bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range));
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation)
            {
                action = commander.Order(frame, Abilities.MOVE, GetPositionFromRange(commander, bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range));
                return true;
            }

            var vectors = commander.UnitCalculation.NearbyAllies.Where(a => (!a.Unit.IsFlying && !commander.UnitCalculation.Unit.IsFlying) || (a.Unit.IsFlying && commander.UnitCalculation.Unit.IsFlying)).Select(u => u.Position);
            if (vectors.Any())
            {
                var center = new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };
                if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(center.X, center.Y)) + commander.UnitCalculation.Unit.Radius > 1)
                {
                    action = commander.Order(frame, Abilities.MOVE, center);
                    return true;
                }
            }

            action = commander.Order(frame, Abilities.ATTACK, target);
            return true;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_GRAVITONBEAM))
            {
                if (commander.UnitCalculation.NearbyAllies.Count() == 0)
                {
                    action = commander.Order(frame, Abilities.CANCEL_GRAVITONBEAM);
                    return true;
                }
                var tag = commander.UnitCalculation.Unit.Orders.FirstOrDefault(o => o.AbilityId == (uint)Abilities.EFFECT_GRAVITONBEAM).TargetUnitTag;
                var liftedUnit = commander.UnitCalculation.NearbyEnemies.FirstOrDefault(e => e.Unit.Tag == tag);
                if (liftedUnit != null)
                {
                    if (liftedUnit.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTORPHASED && e.Unit.BuffDurationRemain < 5 && Vector2.Distance(e.Position, liftedUnit.Position) < 1.375f))
                    {
                        action = commander.Order(frame, Abilities.CANCEL_GRAVITONBEAM);
                        return true;
                    }
                }

                return true;
            }

            if (commander.UnitCalculation.Unit.Energy < 50 || commander.UnitCalculation.NearbyAllies.Count() == 0)
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM))) // only have one unit lifted at a time
            {
                return false;
            }

            var bestGravitonTarget = GetBestGravitonBeamTarget(commander, target, defensivePoint);
            if (bestGravitonTarget != null && bestGravitonTarget.FrameLastSeen + 1 >= frame)
            {
                TagService.TagAbility("graviton");
                CameraManager.SetCamera(bestGravitonTarget.Position);
                action = commander.Order(frame, Abilities.EFFECT_GRAVITONBEAM, null, bestGravitonTarget.Unit.Tag);
                return true;
            }

            return false;
        }

        protected virtual UnitCalculation GetBestGravitonBeamTarget(UnitCommander commander, Point2D target, Point2D defensivePoint)
        {
            var existingOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.EFFECT_GRAVITONBEAM).FirstOrDefault();

            var range = 4;

            var attacks = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType != DisplayType.Hidden && !u.Unit.IsFlying && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)
                && (u.EnemiesInRange.Count() > 1 || (u.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANK || u.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE || u.Unit.UnitType == (uint)UnitTypes.PROTOSS_IMMORTAL || u.Unit.UnitType == (uint)UnitTypes.PROTOSS_DISRUPTOR) && u.EnemiesInRange.Any()));

            if (attacks.Any())
            {
                var bestAttack = GetBestTargetFromList(commander, attacks, existingOrder);
                if (bestAttack != null)
                {
                    return bestAttack;
                }
            }

            attacks =commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && !u.Unit.IsFlying && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM) && InRange(u.Position, commander.UnitCalculation.Position, range)
                && u.EnemiesInRange.Count() > 1); // units that are in range right now

            if (attacks.Any())
            {
                var bestAttack = GetBestTargetFromList(commander, attacks, existingOrder);
                if (bestAttack != null)
                {
                    return bestAttack;
                }
            }

            attacks = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType != DisplayType.Hidden && !u.Unit.IsFlying && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM) && !InRange(u.Position, commander.UnitCalculation.Position, range)
                && u.EnemiesInRange.Count() > 1); // units that are not in range right now

            if (attacks.Any())
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingOrder);
                if (bestOutOfRangeAttack != null)
                {
                    return bestOutOfRangeAttack;
                }
            }

            if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir) && commander.UnitCalculation.NearbyAllies.Any(a => a.DamageAir))
            {
                attacks = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType != DisplayType.Hidden && !u.Unit.IsFlying && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM));
                if (attacks.Any())
                {
                    var bestSafeLift = GetBestTargetFromList(commander, attacks, existingOrder);
                    if (bestSafeLift != null)
                    {
                        return bestSafeLift;
                    }
                }
            }

            return null;
        }
    }
}
