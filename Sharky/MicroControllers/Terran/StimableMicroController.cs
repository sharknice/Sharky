namespace Sharky.MicroControllers.Terran
{
    public class StimableMicroController : IndividualMicroController
    {
        public StimableMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (GetOutOfBunker(commander, target, frame, out List<SC2Action> action)) { return action; }
            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, null, frame, out action)) { return action; }
            if (commander.UnitCalculation.TargetPriorityCalculation.OverallWinnability < 1 && GetInBunker(commander, target, frame, out action)) { return action; }
            return base.Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        public override List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (GetOutOfBunker(commander, target, frame, out List<SC2Action> action)) { return action; }
            if (GetInBunker(commander, target, frame, out action)) { return action; }
            return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
        }

        protected bool GetOutOfBunker(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Loaded)
            {
                var bunker = ActiveUnitData.Commanders.Values.FirstOrDefault(a => a.UnitCalculation.Unit.Passengers.Any(p => p.Tag == commander.UnitCalculation.Unit.Tag));
                if (bunker.UnitCalculation.EnemiesThreateningDamage.Any() || bunker.UnitCalculation.NearbyEnemies.Count(e => e.DamageGround && e.FrameLastSeen == frame && e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) > 3)
                {
                    return false;
                }
                if (Vector2.DistanceSquared(bunker.UnitCalculation.Position, target.ToVector2()) > 100)
                {
                    action = bunker.UnloadSpecificUnit(frame, Abilities.UNLOADALLAT, commander.UnitCalculation.Unit.Tag);
                    return true;
                }
            }

            return false;
        }

        protected bool Stiming(UnitCommander commander)
        {
            return commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER);
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.STIMPACK))
            {
                if (Stiming(commander)) // don't double stim
                {
                    commander.UnitCalculation.TargetPriorityCalculation.Overwhelm = true;
                    return false;
                }

                if (commander.UnitCalculation.EnemiesInRange.Sum(e => e.Unit.Health + e.Unit.Shield) > 100) // stim if more than 100 hitpoints in range
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    TagService.TagAbility("stim");
                    action = commander.Order(frame, Abilities.EFFECT_STIM);
                    return true;
                }
            }

            return false;
        }

        protected override bool GetInBunker(UnitCommander commander, Point2D target, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Loaded) { return false; }

            bool willWin = commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && (commander.UnitCalculation.Unit.Health == commander.UnitCalculation.Unit.HealthMax || (!commander.UnitCalculation.EnemiesInRangeOfAvoid.Any() && !commander.UnitCalculation.EnemiesThreateningDamage.Any()));

            var nearbyBunkers = commander.UnitCalculation.NearbyAllies.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && u.Unit.BuildProgress == 1);
            foreach (var bunker in nearbyBunkers)
            {
                if (bunker.Unit.CargoSpaceMax - bunker.Unit.CargoSpaceTaken >= UnitDataService.CargoSize((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    if (!willWin || Vector2.DistanceSquared(bunker.Position, target.ToVector2()) < 25)
                    {
                        TagService.TagAbility("bunker_load");
                        action = commander.Order(frame, Abilities.SMART, targetTag: bunker.Unit.Tag, allowSpam: true);
                        return true;
                    }
                }
            }

            return false;
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            if (Stiming(commander))
            {
                return base.GetMovementSpeed(commander) + 1.57f;
            }
            return base.GetMovementSpeed(commander);
        }

        public override bool Move(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat))
            {
                if (Retreat(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return true; }

            if (bestTarget != null && Stiming(commander))
            {
                if (MoveToAttackTarget(commander, bestTarget, formation, frame, out action)) { return true; }
                return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
            }

            if (!(formation == Formation.Loose && commander.UnitCalculation.NearbyAllies.Count() > 5))
            {
                if (MoveAway(commander, target, defensivePoint, frame, out action)) { return true; }
            }

            return NavigateToTarget(commander, target, groupCenter, bestTarget, formation, frame, out action);
        }

        public override bool MoveToAttackOnCooldown(UnitCommander commander, UnitCalculation bestTarget, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (Stiming(commander))
            {
                return false;
            }

            return base.MoveToAttackOnCooldown(commander, bestTarget, target, defensivePoint, frame, out action);
        }

        protected override bool MoveToAttackTarget(UnitCommander commander, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (bestTarget != null && Stiming(commander))
            {
                var attackPoint = new Point2D { X = bestTarget.Position.X, Y = bestTarget.Position.Y };
                if (MapDataService.PathWalkable(attackPoint))
                {
                    action = commander.Order(frame, Abilities.MOVE, attackPoint);
                    return true;
                }
            }

            return base.MoveToAttackTarget(commander, bestTarget, formation, frame, out action);
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) 
            {
                if (GetOutOfBunker(commander, defensivePoint, frame, out action)) { return action; }

                return action; 
            }

            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }
    }
}
