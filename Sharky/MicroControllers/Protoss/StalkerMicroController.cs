namespace Sharky.MicroControllers.Protoss
{
    public class StalkerMicroController : IndividualMicroController
    {
        public StalkerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            MaximumSupportDistanceSqaured = 9;
        }

        public override bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null)
            {
                if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.KillWorkers && !bestTarget.UnitClassifications.HasFlag(UnitClassification.Worker) && bestTarget.Attributes.Contains(SC2APIProtocol.Attribute.Structure))
                {
                    if (bestTarget.Repairers.Any())
                    {
                        var repairer = commander.UnitCalculation.NearbyEnemies.Where(e => bestTarget.Repairers.Any(u => u.Tag == e.Unit.Tag)).OrderBy(e => e.Unit.Health).ThenBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                        if (repairer != null)
                        {
                            bestTarget = repairer;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible && MapDataService.SelfVisible(bestTarget.Unit.Pos) && bestTarget.FrameLastSeen == frame)
                {
                    if (WeaponReady(commander, frame))
                    {
                        bestTarget.IncomingDamage += GetDamage(commander.UnitCalculation.Weapons, bestTarget.Unit, bestTarget.UnitTypeData);
                        action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    }
                    else
                    {
                        action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    }
                    return true;
                }

                if (WeaponReady(commander, frame) && commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability > 5)
                {
                    var blinkReady = GetBlinkReady(commander, frame);
                    if (blinkReady && bestTarget.FrameLastSeen == frame)
                    {
                        // only blink if can see all around unit, don't blink when entire army is hidden behind first unit
                        var point = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
                        if (MapDataService.SelfVisible(point) &&
                            MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X + 7, Y = bestTarget.Unit.Pos.Y }) && MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X - 7, Y = bestTarget.Unit.Pos.Y }) &&
                            MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y + 7 }) && MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y - 7 }))
                        {
                            TagService.TagAbility("blink");
                            CameraManager.SetCamera(point.ToVector2(), commander.UnitCalculation.Position);
                            action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, point);
                            return true;
                        }
                    }
                }
            }

            return base.AttackBestTargetInRange(commander, target, bestTarget, frame, out action);
        }

        protected override bool AvoidTargetedDamage(UnitCommander commander, Point2D target, UnitCalculation bestTarget, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var blinkReady = GetBlinkReady(commander, frame);
            if (blinkReady)
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
                {
                    var cyclone = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (cyclone != null)
                    {
                        var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, cyclone.Unit.Pos, target, defensivePoint, 15f + cyclone.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                        TagService.TagAbility("blink");
                        CameraManager.SetCamera(avoidPoint.ToVector2(), commander.UnitCalculation.Position);
                        action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                        return true;
                    }
                }

                var attack = commander.UnitCalculation.Attackers.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
                if (attack != null)
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                    TagService.TagAbility("blink");
                    CameraManager.SetCamera(avoidPoint.ToVector2(), commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                    return true;
                }
                return false;
            }

            return base.AvoidTargetedDamage(commander, target, bestTarget, defensivePoint, frame, out action);
        }

        protected bool GetBlinkReady(UnitCommander commander, int frame)
        {
            return SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BLINKTECH) && commander.AbilityOffCooldown(Abilities.EFFECT_BLINK_STALKER, frame, SharkyOptions.FramesPerSecond, SharkyUnitData) && !commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.FUNGALGROWTH);
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, UnitCalculation bestTarget, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var blinkReady = GetBlinkReady(commander, frame);
            if (blinkReady && commander.UnitCalculation.Unit.Shield < 10)
            {
                var attacks = new List<UnitCalculation>();

                foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
                {
                    if (DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, UnitDataService.GetRange(enemyAttack.Unit) + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                    {
                        attacks.Add(enemyAttack);
                    }
                }

                if (attacks.Count > 0)
                {
                    var attack = attacks.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();  // enemies that are closest to being outranged
                    var range = UnitDataService.GetRange(attack.Unit);
                    if (attack.Range > range)
                    {
                        range = attack.Range;
                    }

                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                    TagService.TagAbility("blink");
                    CameraManager.SetCamera(avoidPoint.ToVector2(), commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                    return true;
                }

                if (MaintainRange(commander, defensivePoint, frame, out action)) { return true; }

                return false;
            }

            return base.AvoidDamage(commander, target, bestTarget, defensivePoint, frame, out action);
        }

        public override bool WeaponReady(UnitCommander commander, int frame)
        {
            return commander.UnitCalculation.Unit.WeaponCooldown is < 2 or >= 26;
        }


        protected override bool DealWithCyclones(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
            {
                var blinkReady = GetBlinkReady(commander, frame);
                if (blinkReady)
                {
                    var cyclone = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (cyclone != null)
                    {
                        var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, cyclone.Unit.Pos, target, defensivePoint, 15f + cyclone.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                        TagService.TagAbility("blink");
                        CameraManager.SetCamera(avoidPoint.ToVector2(), commander.UnitCalculation.Position);
                        action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                        return true;
                    }
                }
            }

            return base.DealWithCyclones(commander, target, defensivePoint, frame, out action);
        }
    }
}
