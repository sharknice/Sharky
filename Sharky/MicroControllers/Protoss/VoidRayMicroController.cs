namespace Sharky.MicroControllers.Protoss
{
    public class VoidRayMicroController : IndividualMicroController
    {
        public VoidRayMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder, MicroPriority microPriority, bool groupUpEnabled)
            :base(defaultSharkyBot, pathFinder, microPriority, groupUpEnabled)
        {
            MaximumSupportDistanceSqaured = 25f;
            AvoidDamageDistance = 5;
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return true;
        }

        protected override bool AvoidTargetedDamage(UnitCommander commander, Point2D target, UnitCalculation bestTarget, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax) { return false; }

            var dps = commander.UnitCalculation.Attackers.Sum(a => a.Dps);
            var hp = commander.UnitCalculation.Attackers.Sum(a => a.Unit.Health + a.Unit.Shield);
            if (dps <= 0 || hp <= 0) { return false; }

            var timeToLoseShield = commander.UnitCalculation.Unit.Shield / dps;
            var timeToKill = hp / commander.UnitCalculation.Dps;

            if (timeToLoseShield > timeToKill)
            {
                return false;
            }

            return base.AvoidTargetedDamage(commander, target, bestTarget, defensivePoint, frame, out action);
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.AbilityOffCooldown(Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK))
                {
                    foreach (var tag in commander.UnitCalculation.Unit.Orders.Select(o => o.TargetUnitTag))
                    {
                        UnitCalculation unit;
                        if (ActiveUnitData.EnemyUnits.TryGetValue(tag, out unit))
                        {
                            if (unit.Attributes.Contains(SC2Attribute.Armored))
                            {
                                if (commander.UnitCalculation.EnemiesInRange.Where(e => e.Attributes.Contains(SC2Attribute.Armored)).Sum(e => e.Unit.Health) > 200)
                                {
                                    TagService.TagAbility("prismatic");
                                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                                    action = commander.Order(frame, Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.VOIDRAYSWARMDAMAGEBOOST))
            {
                if (!commander.UnitCalculation.EnemiesInRange.Any(e => e.Attributes.Contains(SC2Attribute.Armored)))
                {
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || !commander.UnitCalculation.NearbyEnemies.Any(e => e.Attributes.Contains(SC2Attribute.Armored)))
                    {
                        TagService.TagAbility("cancel_prismatic");
                        CameraManager.SetCamera(commander.UnitCalculation.Position);
                        action = commander.Order(frame, Abilities.CANCEL);
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool RechargeShieldsAtBattery(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 25)
            {
                var shieldBatttery = ActiveUnitData.SelfUnits.Where(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && a.Value.Unit.BuildProgress == 1 && a.Value.Unit.Energy > 5 && a.Value.Unit.Orders.Count() == 0).OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Value.Position)).FirstOrDefault().Value;
                if (shieldBatttery != null)
                {
                    var distanceSquared = Vector2.DistanceSquared(commander.UnitCalculation.Position, shieldBatttery.Position);
                    if (distanceSquared > 35 && distanceSquared < 2500)
                    {
                        action = commander.Order(frame, Abilities.MOVE, new Point2D { X = shieldBatttery.Position.X, Y = shieldBatttery.Position.Y });
                        return true;
                    }
                }
            }
            return false;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.VOIDRAYSWARMDAMAGEBOOST))
            {
                var action = commander.Order(frame, Abilities.CANCEL);
                if (action != null)
                {
                    return action;
                }
            }
            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }

        public override bool MaintainRange(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2Action> action)
        {
            action = null;
            return false;
        }

        protected override bool DoFreeDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2Action> action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir))
            {
                return false;
            }

            return base.MaintainRange(commander, defensivePoint, frame, out action);
        }

        public override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2Action> action)
        {
            action = null;

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.DamageAir))
            {
                if (commander.RetreatPathFrame + 2 < frame || commander.RetreatPathIndex >= commander.RetreatPath.Count())
                {
                    commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, defensivePoint.X, defensivePoint.Y, frame);
                    commander.RetreatPathFrame = frame;
                    commander.RetreatPathIndex = 1;
                }

                if (FollowPath(commander, frame, out action)) { return true; }
            }

            action = MoveToTarget(commander, defensivePoint, frame);
            return true;
        }

        public override List<SC2APIProtocol.Action> Contain(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            if (WeaponReady(commander, frame) && !commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
            {
                var bestTarget = GetBestTarget(commander, target, frame);
                if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
            }

            if (SpecialCaseMove(commander, target, defensivePoint, groupCenter, null, Formation.Normal, frame, out action)) { return action; }

            if (Vector2.Distance(commander.UnitCalculation.Position, target.ToVector2()) < 5 || !commander.UnitCalculation.EnemiesInRangeOfAvoid.Any())
            {
                return commander.Order(frame, Abilities.MOVE, target);
            }

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, null, frame, out action)) { return action; }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => (e.FrameLastSeen > (frame - (SharkyOptions.FramesPerSecond * 60)) || e.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) && DamageService.CanDamage(e, commander.UnitCalculation)))
            {
                if (commander.RetreatPathFrame + 2 < frame)
                {
                    if (commander.UnitCalculation.Unit.IsFlying)
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeAirPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, target.X, target.Y, frame);
                        commander.RetreatPathFrame = frame;
                    }
                    else
                    {
                        commander.RetreatPath = SharkyPathFinder.GetSafeGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, target.X, target.Y, frame);
                        commander.RetreatPathFrame = frame;
                    }
                }

                if (FollowPath(commander, frame, out action)) { return action; }
            }

            if (AvoidTargetedDamage(commander, target, null, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidDamage(commander, target, null, defensivePoint, frame, out action))
            {
                return action;
            }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }

        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            if (commander.UnitCalculation.Loaded) { return action; }

            var bestTarget = GetBestHarassTarget(commander, target);

            if (ContinueInRangeAttack(commander, frame, out action)) { return action; }

            if (SpecialCaseMove(commander, target, defensivePoint, null, bestTarget, Formation.Normal, frame, out action)) { return action; }
            if (PreOffenseOrder(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            if (AvoidTargetedOneHitKills(commander, target, defensivePoint, frame, out action)) { return action; }
            if (OffensiveAbility(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (!commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)) || commander.UnitCalculation.EnemiesInRange.Any(e => e.Damage > 0 || e.Unit.Energy > 0 || e.Unit.IsActive || Vector2.Distance(e.Position, commander.UnitCalculation.Position) < 2))
                {
                    if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action)) { return action; }
                }
                if (bestTarget != null && bestTarget.UnitClassifications.HasFlag(UnitClassification.Worker))
                {
                    if (ShouldStayOutOfRange(commander, frame) && AvoidAllDamage(commander, target, bestTarget, defensivePoint, frame, out action)) { return action; }

                    if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
                }
            }

            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm)
            {
                if (AvoidEnemiesThreateningDamage(commander, target, bestTarget, defensivePoint, frame, true, out action)) { return action; }
                if (AvoidArmyEnemies(commander, target, bestTarget, defensivePoint, frame, true, out action)) { return action; }
            }

            return commander.Order(frame, Abilities.ATTACK, target);
        }
    }
}
