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

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
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

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
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

        protected override bool MaintainRange(UnitCommander commander, Point2D defensivePoint, int frame, out List<SC2Action> action)
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

        protected override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2Action> action)
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
    }
}
