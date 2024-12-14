﻿namespace Sharky.MicroControllers.Protoss
{
    public class PhoenixInGroupMicroController : PhoenixMicroController
    {
        float PhoenixKiteDistance = 2.975f; // 5.95 movement speed / 2 = 2.975
        float HalfWeaponCooldown = 8.5f; // frames until half cooldown (SharkyOptions.FramesPerSecond * 0.79f) / 2f) = 8.848 frames

        public PhoenixInGroupMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            AvoidDamageDistance = 5;
            LooseFormationDistance = 1;
        }

        public UnitCalculation GetBestTargetForGroup(UnitCommander commander, Point2D target, int frame)
        {
            var bestTarget = GetBestTarget(commander, target, frame);
            if (bestTarget == null)
            {
                var existingAttackOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK).FirstOrDefault();

                var range = commander.UnitCalculation.Range;

                var attacks = commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType == DisplayType.Visible && (u.Unit.IsFlying || (!u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive))) && AttackersFilter(commander, u)); // units that are in range right now

                UnitCalculation bestAttack = null;
                if (attacks.Any())
                {
                    bestAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                    if (bestAttack != null)
                    {
                        commander.BestTarget = bestAttack;
                        return bestAttack;
                    }
                }

                if (!MapDataService.SelfVisible(target)) // if enemy main is unexplored, march to enemy main
                {
                    var fakeMainBase = new Unit(commander.UnitCalculation.Unit);
                    fakeMainBase.Pos = new Point { X = TargetingData.EnemyMainBasePoint.X, Y = TargetingData.EnemyMainBasePoint.Y, Z = 1 };
                    fakeMainBase.Alliance = Alliance.Enemy;
                    return new UnitCalculation(fakeMainBase, new List<Unit>(), SharkyUnitData, SharkyOptions, UnitDataService, MapDataService.IsOnCreep(fakeMainBase.Pos), frame);
                }
                var unitsNearEnemyMain = ActiveUnitData.EnemyUnits.Values.Where(e => !AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType) && e.Unit.UnitType != (uint)UnitTypes.ZERG_LARVA && InRange(new Vector2(target.X, target.Y), e.Position, 20));
                if (unitsNearEnemyMain.Any() && InRange(new Vector2(target.X, target.Y), commander.UnitCalculation.Position, 100))
                {
                    attacks = unitsNearEnemyMain.Where(enemyAttack => enemyAttack.Unit.DisplayType == DisplayType.Visible && (enemyAttack.Unit.IsFlying || (!enemyAttack.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !enemyAttack.Attributes.Contains(SC2APIProtocol.Attribute.Massive))) && AttackersFilter(commander, enemyAttack));
                    if (attacks.Any())
                    {
                        var bestMainAttack = GetBestTargetFromList(commander, attacks, existingAttackOrder);
                        if (bestMainAttack != null)
                        {
                            commander.BestTarget = bestMainAttack;
                            return bestMainAttack;
                        }
                        if (bestAttack == null)
                        {
                            bestAttack = bestMainAttack;
                        }
                    }
                }

                commander.BestTarget = bestAttack;
                return bestAttack;
            }
            if (commander.UnitCalculation.NearbyEnemies.Contains(bestTarget) && bestTarget.FrameLastSeen == frame)
            {
                return bestTarget;
            }
            return null;
        }

        public Point2D GetKiteSpot(UnitCommander commander, UnitCalculation bestTarget, Point2D target, Vector2 groupVector, int frame)
        {
            var range = PhoenixKiteDistance + commander.UnitCalculation.Range + commander.UnitCalculation.Unit.Radius + bestTarget.Unit.Radius;

            var threat = commander.UnitCalculation.NearbyEnemies.Where(e => e.FrameLastSeen > frame - 5 && (DamageService.CanDamage(e, commander.UnitCalculation) || (!e.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !e.Attributes.Contains(SC2APIProtocol.Attribute.Massive)))).OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
            if (threat != null)
            {
                DebugService.DrawSphere(new Point { X = groupVector.X, Y = groupVector.Y, Z = commander.UnitCalculation.Unit.Pos.Z }, .5f, new Color { R = 250, B = 1, G = 250 });

                if (threat == bestTarget || Vector2.DistanceSquared(commander.UnitCalculation.Position, threat.Position) < Vector2.DistanceSquared(commander.UnitCalculation.Position, bestTarget.Position))
                {
                    return GetPositionFromRange(commander, bestTarget.Unit.Pos, groupVector, range);
                }

                // kite away from the threat
                var angle = Math.Atan2(threat.Position.Y - bestTarget.Position.Y, bestTarget.Position.X - threat.Position.X);
                var x = range * Math.Cos(angle);
                var y = range * Math.Sin(angle);
                return new Point2D { X = bestTarget.Position.X + (float)x, Y = bestTarget.Position.Y - (float)y };
            }

            var secondBestTarget = GetSecondBestTarget(commander, target, frame, bestTarget);
            if (secondBestTarget != null)
            {
                DebugService.DrawLine(bestTarget.Unit.Pos, secondBestTarget.Unit.Pos, new Color { R = 250, B = 1, G = 250 });
                DebugService.DrawSphere(secondBestTarget.Unit.Pos, .5f, new Color { R = 250, B = 1, G = 250 });

                // kite to next target
                var angle = Math.Atan2(bestTarget.Position.Y - secondBestTarget.Position.Y, secondBestTarget.Position.X - bestTarget.Position.X);
                var x = range * Math.Cos(angle);
                var y = range * Math.Sin(angle);
                return new Point2D { X = bestTarget.Position.X + (float)x, Y = bestTarget.Position.Y - (float)y };
            }

            // group up while kiting
            return GetPositionFromRange(commander, bestTarget.Unit.Pos, groupVector, range);
        }

        public List<SC2APIProtocol.Action> AttackDesignatedTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Point2D kiteSpot, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var formation = GetDesiredFormation(commander);

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame, out action))
            {
                return action;
            }

            if (bestTarget != null && bestTarget.FrameLastSeen + 1 < frame)
            {
                return commander.Order(frame, Abilities.MOVE, bestTarget.Position.ToPoint2D());
            }

            if (commander.UnitCalculation.Unit.Energy >= 50 && !DamageService.CanDamage(commander.UnitCalculation, bestTarget))
            {
                return commander.Order(frame, Abilities.EFFECT_GRAVITONBEAM, targetTag: bestTarget.Unit.Tag);
            }

            if (formation == Formation.Loose)
            {
                if (commander.UnitCalculation.Unit.WeaponCooldown <= .1f)
                {
                    return commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                }
                var closestAlly = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.IsFlying).OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
                if (closestAlly != null)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestAlly.Position) < (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius) * (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius))
                    {
                        var avoidPoint = GetPositionFromRange(commander, closestAlly.Unit.Pos, commander.UnitCalculation.Unit.Pos, LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius + .5f);
                        return commander.Order(frame, Abilities.MOVE, avoidPoint);
                    }
                }
            }

            if (commander.UnitCalculation.Unit.WeaponCooldown < HalfWeaponCooldown)
            {
                if (DamageService.CanDamage(commander.UnitCalculation, bestTarget))
                {
                    return commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                }
                else
                {
                    return commander.Order(frame, Abilities.MOVE, bestTarget.Position.ToPoint2D());
                }
            }

            return commander.Order(frame, Abilities.MOVE, kiteSpot, allowSpam: true);
        }

        public List<SC2APIProtocol.Action> AttackInRange(UnitCommander commander, int frame)
        {
            var bestTarget = GetBestTargetFromList(commander, commander.UnitCalculation.EnemiesInRange, null);
            if (bestTarget != null)
            {
                return commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
            }
            return null;
        }

        public override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (commander.UnitCalculation.EnemiesThreateningDamage.Any() && !commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                return false;
            }

            if (IgnoreDistractions && IsDistraction(commander, target, bestTarget, frame))
            {
                return false;
            }

            if (TargetEnemyMainFirst && TargetingData.AttackPoint.X == TargetingData.EnemyMainBasePoint.X && TargetingData.AttackPoint.Y == TargetingData.EnemyMainBasePoint.Y)
            {
                if (Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), commander.UnitCalculation.Position) > 100)
                {
                    if (bestTarget == null) { return false; }
                    if (!bestTarget.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !bestTarget.UnitClassifications.HasFlag(UnitClassification.DefensiveStructure) && !bestTarget.UnitClassifications.HasFlag(UnitClassification.Worker))
                    {
                        return false;
                    }
                }
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && MicroPriority != MicroPriority.NavigateToLocation)
            {
                if (GetHighGroundVision(commander, target, defensivePoint, bestTarget, frame, out action)) { return true; }

                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }

                if (WeaponReady(commander, frame) && bestTarget.FrameLastSeen == frame)
                {
                    action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                }
                else
                {
                    if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }
                    if (AvoidDeceleration(commander, enemyPosition, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                }
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation)
            {
                var enemyPosition = GetBestTargetAttackPoint(commander, bestTarget);
                if (SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType) || commander.UnitCalculation.NearbyEnemies.Any(e => AvoidedUnitTypes.Contains((UnitTypes)e.Unit.UnitType)))
                {
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    return true;
                }
                if (WeaponReady(commander, frame))
                {
                    if (bestTarget.Unit.Tag == commander.UnitCalculation.Unit.Tag)
                    {
                        action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.ATTACK, targetTag: bestTarget.Unit.Tag);
                    }
                    return true;
                }
                else
                {
                    if (GroupUpEnabled && GroupUp(commander, target, groupCenter, false, frame, out action)) { return true; }
                    if (AvoidDeceleration(commander, enemyPosition, false, frame, out action)) { return true; }
                    action = commander.Order(frame, Abilities.MOVE, enemyPosition);
                }
                return true;
            }

            if (AvoidDeceleration(commander, target, true, frame, out action)) { return true; }

            // no damaging targets in range, attack towards the main target
            action = commander.Order(frame, Abilities.MOVE, target);

            return true;
        }

        public override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (AvoidEnemiesThreateningDamage(commander, target, defensivePoint, frame, false, out action)) { return true; }

            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority != TargetPriority.FullRetreat && commander.UnitCalculation.EnemiesInRange.Any() && WeaponReady(commander, frame) && !SharkyUnitData.NoWeaponCooldownTypes.Contains((UnitTypes)commander.UnitCalculation.Unit.UnitType)) // keep shooting as you retreat
            {
                var bestTarget = GetBestTarget(commander, target, frame);
                if (bestTarget != null && MapDataService.SelfVisible(bestTarget.Unit.Pos))
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }
            }

            var closestEnemy = commander.UnitCalculation.NearbyEnemies.Take(25).Where(e => DamageService.CanDamage(e, commander.UnitCalculation)).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            if (closestEnemy == null)
            {
                closestEnemy = commander.UnitCalculation.NearbyEnemies.Take(25).OrderBy(u => Vector2.DistanceSquared(u.Position, commander.UnitCalculation.Position)).FirstOrDefault();
            }


            if (closestEnemy != null)
            {
                var formation = GetDesiredFormation(commander);
                if (formation == Formation.Loose)
                {
                    var closestAlly = commander.UnitCalculation.NearbyAllies.Where(a => a.Unit.IsFlying).OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Position)).FirstOrDefault();
                    if (closestAlly != null)
                    {
                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, closestAlly.Position) < (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius) * (LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius))
                        {
                            var avoidPoint = GetPositionFromRange(commander, closestAlly.Unit.Pos, commander.UnitCalculation.Unit.Pos, LooseFormationDistance + commander.UnitCalculation.Unit.Radius + closestAlly.Unit.Radius + .5f);
                            action = commander.Order(frame, Abilities.MOVE, avoidPoint);
                            return true;
                        }
                    }
                }

                if (commander.UnitCalculation.NearbyEnemies.Take(25).All(e => e.Range < commander.UnitCalculation.Range && e.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed))
                {
                    if (MapDataService.SelfVisible(closestEnemy.Unit.Pos))
                    {
                        if (commander.UnitCalculation.Range > closestEnemy.Range)
                        {
                            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed;
                            var enemySpeed = closestEnemy.UnitTypeData.MovementSpeed;
                            if (closestEnemy.Unit.BuffIds.Contains((uint)Buffs.MEDIVACSPEEDBOOST))
                            {
                                enemySpeed = 5.94f;
                            }
                            if (closestEnemy.Unit.BuffIds.Contains((uint)Buffs.STIMPACK) || closestEnemy.Unit.BuffIds.Contains((uint)Buffs.STIMPACKMARAUDER))
                            {
                                enemySpeed += 1.57f;
                            }

                            if (speed > enemySpeed || closestEnemy.Range + 3 < commander.UnitCalculation.Range)
                            {
                                if (MaintainRange(commander, defensivePoint, frame, out action)) { return true; }
                            }
                        }
                    }
                }

                if (commander.RetreatPathFrame + 1 < frame || commander.RetreatPathIndex >= commander.RetreatPath.Count())
                {
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    }
                    else
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    }
                    commander.RetreatPathFrame = frame;
                    commander.RetreatPathIndex = 1;
                }

                if (FollowPath(commander, frame, out action)) { return true; }
            }

            action = commander.Order(frame, Abilities.MOVE, defensivePoint);
            return true;
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown < 2;
        }

        public override bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool IsDistraction(UnitCommander commander, Point2D target, UnitCalculation enemy, int frame)
        {
            if (enemy == null) { return false; }
            if (enemy.UnitTypeData.MovementSpeed < commander.UnitCalculation.UnitTypeData.MovementSpeed)
            {
                return false;
            }
            return base.IsDistraction(commander, target, enemy, frame);
        }

        public override List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            var markedForDeath = commander.UnitCalculation.NearbyAllies.FirstOrDefault(a => ActiveUnitData.Commanders.ContainsKey(a.Unit.Tag) && ActiveUnitData.Commanders[a.Unit.Tag].UnitRole == UnitRole.Die);
            if (markedForDeath != null)
            {
                return commander.Order(frame, Abilities.ATTACK, targetTag: markedForDeath.Unit.Tag);
            }
            return commander.Order(frame, Abilities.MOVE, defensivePoint);
        }

        public override bool AvoidDeceleration(UnitCommander commander, Point2D target, bool attackMove, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override UnitCalculation GetBestTarget(UnitCommander commander, Point2D target, int frame)
        {
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillWorkers && commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))
            {
                return base.GetBestHarassTarget(commander, target);
            }
            return base.GetBestTarget(commander, target, frame);
        }

        protected UnitCalculation GetSecondBestTarget(UnitCommander commander, Point2D target, int frame, UnitCalculation bestTarget)
        {
            if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillWorkers && commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)) > 1)
            {
                return GetBestTargetFromList(commander, commander.UnitCalculation.NearbyEnemies.Where(e => bestTarget.Unit.Tag != e.Unit.Tag && e.UnitClassifications.HasFlag(UnitClassification.Worker)), null);
            }
            return GetBestTargetFromList(commander, commander.UnitCalculation.NearbyEnemies.Where(e => bestTarget.Unit.Tag != e.Unit.Tag), null);
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }
            UpdateState(commander, defensivePoint, defensivePoint, groupCenter, null, Formation.Normal, frame);

            var bestTarget = GetBestTarget(commander, defensivePoint, frame);

            if (AvoidTargetedOneHitKills(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }

            // do not do offensive ability, just run

            if (DoFreeDamage(commander, defensivePoint, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            // TODO: setup a concave above the ramp if there is a ramp, get earch grid point on high ground, make sure at least one unit on each point
            if (commander.UnitCalculation.NearbyEnemies.Any() || Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(defensivePoint.X, defensivePoint.Y)) > 25 || MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos) < MapDataService.MapHeight(defensivePoint))
            {
                if (Retreat(commander, defensivePoint, defensivePoint, frame, out action)) { return action; }
                return commander.Order(frame, Abilities.MOVE, defensivePoint);
            }
            else
            {
                return Idle(commander, defensivePoint, frame);
            }
        }

        protected override bool GetInFormation(UnitCommander commander, Formation formation, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitRole == UnitRole.Leader)
            {
                return false;
            }

            return base.GetInFormation(commander, formation, target, bestTarget, frame, out action);
        }
    }
}
