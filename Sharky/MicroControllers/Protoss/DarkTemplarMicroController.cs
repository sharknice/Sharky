namespace Sharky.MicroControllers.Protoss
{
    public class DarkTemplarMicroController : IndividualMicroController
    {
        float ShadowStrikeRange = 8;

        public DarkTemplarMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!Detected(commander))
            {
                commander.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.Attack;
            }
            else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat || commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.FullRetreat)
            {
                if (WeaponReady(commander, frame) && commander.UnitCalculation.EnemiesInRange.Any())
                {
                    return false;
                }

                if (commander.UnitCalculation.Unit.Shield < commander.UnitCalculation.Unit.ShieldMax && commander.UnitCalculation.EnemiesThreateningDamage.Any())
                {
                    var blinkReady = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.DARKTEMPLARBLINKUPGRADE) && commander.AbilityOffCooldown(Abilities.EFFECT_SHADOWSTRIDE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData);
                    if (blinkReady)
                    {
                        var attack = commander.UnitCalculation.EnemiesThreateningDamage.OrderBy(e => Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
                        if (attack != null)
                        {
                            TagService.TagAbility("shadowstrike");
                            var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                            CameraManager.SetCamera(avoidPoint.ToVector2(), commander.UnitCalculation.Position);
                            action = commander.Order(frame, Abilities.EFFECT_SHADOWSTRIDE, avoidPoint);
                            return true;
                        }
                        return false;
                    }
                }
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (bestTarget != null && SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.DARKTEMPLARBLINKUPGRADE) && commander.AbilityOffCooldown(Abilities.EFFECT_SHADOWSTRIDE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                var distanceSqaured = Vector2.DistanceSquared(commander.UnitCalculation.Position, bestTarget.Position);

                if (distanceSqaured <= ShadowStrikeRange * ShadowStrikeRange && distanceSqaured > 9)
                {
                    var x = bestTarget.Unit.Radius * Math.Cos(bestTarget.Unit.Facing);
                    var y = bestTarget.Unit.Radius * Math.Sin(bestTarget.Unit.Facing);
                    var blinkPoint = new Point2D { X = bestTarget.Unit.Pos.X + (float)x, Y = bestTarget.Unit.Pos.Y - (float)y };
                    TagService.TagAbility("shadowstrike");
                    CameraManager.SetCamera(blinkPoint.ToVector2(), commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.EFFECT_SHADOWSTRIDE, blinkPoint);
                    return true;
                }
            }

            return false;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (Detected(commander))
            {
                return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
            }
            return false;
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            if (Detected(commander))
            {
                return base.AvoidDamage(commander, target, defensivePoint, frame, out action);
            }

            return false;
        }

        public override List<SC2APIProtocol.Action> NavigateToPoint(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Detector)))
            {
                if (commander.RetreatPathFrame + 2 < frame)
                {
                    commander.RetreatPath = SharkyPathFinder.GetUndetectedGroundPath(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y, target.X, target.Y, frame);
                    commander.RetreatPathFrame = frame;
                }

                if (FollowPath(commander, frame, out action)) { return action; }
            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return action;
            }

            NavigateToTarget(commander, target, groupCenter, null, Formation.Normal, frame, out action);

            return action;
        }


        public override List<SC2APIProtocol.Action> HarassWorkers(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var bestTarget = GetBestHarassTarget(commander, target);

            if (OffensiveAbility(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTarget(commander, target, defensivePoint, null, bestTarget, frame, out action)) { return action; }
            }

            var formation = GetDesiredFormation(commander);
            if (Move(commander, target, defensivePoint, null, bestTarget, formation, frame, out action)) { return action; }

            return MoveToTarget(commander, target, frame);
        }
    }
}
