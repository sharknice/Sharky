namespace Sharky.MicroControllers
{
    public class IndividualWorkerMicroController : IndividualMicroController
    {
        protected MineralWalker MineralWalker;
        protected bool EnemySpotted = false;

        public IndividualWorkerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 2;

            MineralWalker = defaultSharkyBot.MineralWalker;
        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var formation = Formation.Normal;
            var bestTarget = GetBestTarget(commander, target, frame);

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (AvoidTargettedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (MicroPriority == MicroPriority.StayOutOfRange)
            {
                return AttackStayOutOfRange(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);
            }

            if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return action; }
            if (AvoidPointlessDamage(commander, target, defensivePoint, formation, frame, out action)) { return action; }

            if (!EnemySpotted)
            {
                if (!ActiveUnitData.EnemyUnits.Any() && frame < SharkyOptions.FramesPerSecond * 60 * 3)
                {
                    if (MineralWalker.MineralWalkTarget(commander, frame, out action)) { return action; }
                }
                else
                {
                    EnemySpotted = true;
                }
            }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
                if (AvoidDeathWhenWeaponReady(commander, target, defensivePoint, frame, out action)) { return action; }            
                if (AttackBestTargetOutOfRange(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            if (MoveToAttackOnCooldown(commander, bestTarget, target, defensivePoint, frame, out action)) { return action; }

            if (RetreatHome(commander, target, defensivePoint, frame, out action)) { return action; }

            if (Move(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action)) { return action; }

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return action; }
            return commander.Order(frame, Abilities.ATTACK, target);
        }

        protected bool AvoidDeathWhenWeaponReady(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.ShieldMax > 0)
            {
                if (commander.UnitCalculation.Unit.Shield < 10)
                {
                    if (AvoidDamage(commander, target, defensivePoint, frame, out action)) { return true; }
                }
            }
            else if (commander.UnitCalculation.Unit.Health < 10)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action)) { return true; }
            }
            
            return false;
        }

        protected override bool MoveToAttackOnCooldown(UnitCommander commander, UnitCalculation bestTarget, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (bestTarget == null || bestTarget.FrameLastSeen != frame || !commander.UnitCalculation.NearbyEnemies.Contains(bestTarget))
            {
                return false;
            }

            if (commander.UnitCalculation.Unit.ShieldMax > 0)
            {
                if (commander.UnitCalculation.Unit.Shield < 15)
                {
                    return false;
                }
            }
            else if (commander.UnitCalculation.Unit.Health < 15)
            {
                return false;
            }

            DebugService.DrawLine(commander.UnitCalculation.Unit.Pos, bestTarget.Unit.Pos, new Color { R = 250, B = 1, G = 250 });

            var rangeDistance = commander.UnitCalculation.Range + commander.UnitCalculation.Unit.Radius + bestTarget.Unit.Radius;
            var actualDistance = Vector2.Distance(commander.UnitCalculation.Position, bestTarget.Position);
            var gap = actualDistance - rangeDistance;
            var framesToRange = gap / (GetMovementSpeed(commander) / SharkyOptions.FramesPerSecond);
            var framesToShoot = commander.UnitCalculation.Unit.WeaponCooldown - 1;
            if (framesToRange >= framesToShoot)
            {
                if (AttackBestTargetOutOfRange(commander, target, defensivePoint, null, bestTarget, frame, out action))
                {
                    return true;
                }

                action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                return true;
            }

            return false;
        }

        protected bool AttackBestTargetOutOfRange(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (IgnoreDistractions && IsDistraction(commander, target, bestTarget, frame))
            {
                return false;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Contains(bestTarget))
            {
                if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return true; }

                // mineral walk towards target if mineral within .05 radians

                var height = MapDataService.MapHeight(bestTarget.Unit.Pos);
                if (height == MapDataService.MapHeight(TargetingData.EnemyMainBasePoint))
                {
                    var angleToTarget = Math.Abs(Math.Atan2(bestTarget.Position.Y - commander.UnitCalculation.Position.Y, commander.UnitCalculation.Position.X - bestTarget.Position.X));

                    var mineralFields = ActiveUnitData.NeutralUnits.Values.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Unit.UnitType));
                    var nearbyMinerals = mineralFields.Where(e => MapDataService.MapHeight(e.Unit.Pos) == height && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 400);
                    var mineralPatch = nearbyMinerals.OrderByDescending(m => 0 - Math.Abs(angleToTarget - Math.Abs(Math.Atan2(m.Position.Y - commander.UnitCalculation.Position.Y, commander.UnitCalculation.Position.X - m.Position.X)))).FirstOrDefault();
                    if (mineralPatch != null)
                    {
                        if (0 - Math.Abs(angleToTarget - Math.Abs(Math.Atan2(mineralPatch.Position.Y - commander.UnitCalculation.Position.Y, commander.UnitCalculation.Position.X - mineralPatch.Position.X))) > -.05)
                        {
                            var distanceToMineral = Vector2.DistanceSquared(mineralPatch.Position, commander.UnitCalculation.Position);
                            var distanceToTarget = Vector2.DistanceSquared(bestTarget.Position, commander.UnitCalculation.Position);
                            if (distanceToMineral > distanceToTarget && distanceToMineral > 4)
                            {
                                action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Unit.Tag, allowSpam: true);
                                return true;
                            }
                        }
                    }
                }

                action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                return true;
            }

            return false;
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Health + commander.UnitCalculation.Unit.Shield < 6 && !(WeaponReady(commander, frame) && commander.UnitCalculation.EnemiesInRange.Count() > 0))
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            if (!commander.UnitCalculation.EnemiesInRange.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
            {
                if (GroupUp(commander, target, groupCenter, false, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 1;
        }

        protected override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

            var priorityAttacks = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.DisplayType == DisplayType.Visible && e.UnitClassifications.Contains(UnitClassification.Worker));

            var attacks = commander.UnitCalculation.EnemiesInRange.Where(e => priorityAttacks.Any(p => p.Unit.Tag == e.Unit.Tag) && AttackersFilter(commander, e));

            UnitCalculation bestAttack = null;
            if (attacks.Count() > 0)
            {
                var oneShotKills = attacks.Where(a => a.Unit.Health + a.Unit.Shield < GetDamage(commander.UnitCalculation.Weapons, a.Unit, a.UnitTypeData));
                if (oneShotKills.Count() > 0)
                {
                    if (existingAttackOrder != null)
                    {
                        var existing = oneShotKills.FirstOrDefault(o => o.Unit.Tag == existingAttackOrder.TargetUnitTag);
                        if (existing != null)
                        {
                            return existing; // just keep attacking the same unit
                        }
                    }

                    var oneShotKill = GetBestTargetFromList(commander, oneShotKills, existingAttackOrder);
                    if (oneShotKill != null)
                    {
                        return oneShotKill;
                    }
                    else
                    {
                        commander.BestTarget = oneShotKills.OrderBy(o => o.Dps).FirstOrDefault();
                        return commander.BestTarget;
                    }
                }

                bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            attacks = priorityAttacks;
            if (attacks.Count() > 0)
            {
                bestAttack = attacks.OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
                if (bestAttack != null)
                {
                    commander.BestTarget = bestAttack;
                    return bestAttack;
                }
            }

            if (bestAttack == null)
            {
                return base.GetBestTarget(commander, target, frame);
            }

            commander.BestTarget = bestAttack;
            return bestAttack;
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var threat = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => e.FrameLastSeen == frame).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
            if (threat != null)
            {
                if (commander.UnitCalculation.Unit.ShieldMax > 0)
                {
                    if (commander.UnitCalculation.Unit.Shield < 10)
                    {             
                        if (AvoidThreatWithMineralWalk(commander, threat, frame, out action))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (commander.UnitCalculation.Unit.Health < 10)
                    {
                        if (AvoidThreatWithMineralWalk(commander, threat, frame, out action))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected bool AvoidThreatWithMineralWalk(UnitCommander commander, UnitCalculation threat, int frame, out List<SC2APIProtocol.Action> action) 
        {
            action = null;

            // get mineral furthest angle away from threat
            // if mineral is at least 2.8 radians within correct direction mineral walk
            // else mineral walk home
            var height = MapDataService.MapHeight(threat.Unit.Pos);
            if (height == MapDataService.MapHeight(TargetingData.EnemyMainBasePoint))
            {
                var angleToTarget = Math.Abs(Math.Atan2(threat.Position.Y - commander.UnitCalculation.Position.Y, commander.UnitCalculation.Position.X - threat.Position.X));

                var mineralFields = ActiveUnitData.NeutralUnits.Values.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Unit.UnitType));
                var nearbyMinerals = mineralFields.Where(e => MapDataService.MapHeight(e.Unit.Pos) == height && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 400);
                var mineralPatch = nearbyMinerals.OrderBy(m => 0 - Math.Abs(angleToTarget - Math.Abs(Math.Atan2(m.Position.Y - commander.UnitCalculation.Position.Y, commander.UnitCalculation.Position.X - m.Position.X)))).FirstOrDefault();
                if (mineralPatch != null)
                {
                    if (0 - Math.Abs(angleToTarget - Math.Abs(Math.Atan2(mineralPatch.Position.Y - commander.UnitCalculation.Position.Y, commander.UnitCalculation.Position.X - mineralPatch.Position.X))) < -2.8)
                    {
                        var distanceToMineral = Vector2.DistanceSquared(mineralPatch.Position, commander.UnitCalculation.Position);
                        var distanceToTarget = Vector2.DistanceSquared(threat.Position, commander.UnitCalculation.Position);
                        if (distanceToMineral > distanceToTarget && distanceToMineral > 4)
                        {
                            action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Unit.Tag, allowSpam: true);
                            return true;
                        }
                    }
                }
            }

            if (MineralWalker.MineralWalkHome(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected bool RetreatHome(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var threat = commander.UnitCalculation.EnemiesThreateningDamage.Where(e => e.FrameLastSeen == frame).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
            if (threat != null)
            {
                var height = MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos);
                var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                var nearbyMinerals = mineralFields.Where(e => MapDataService.MapHeight(e.Value.Unit.Pos) == height);
                // get mineral furthest angle away from threat
                // if mineral is at least 90 degrees away mineral walk
                // else mineral walk home
                if (MineralWalker.MineralWalkHome(commander, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
