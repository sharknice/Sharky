namespace Sharky.MicroControllers.Protoss
{
    public class SentryMicroController : IndividualMicroController
    {
        int LastGuardianShieldActivationFrame { get; set; }

        public SentryMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            LastGuardianShieldActivationFrame = -1000;
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.GUARDIANSHIELD) && commander.UnitCalculation.Unit.Shield > 0)
            {
                var unshielded = commander.UnitCalculation.NearbyAllies.Where(a => !a.Unit.BuffIds.Contains((uint)Buffs.GUARDIANSHIELD) && a.EnemiesInRangeOf.Any(e => e.Range > 2)).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                if (unshielded != null)
                {
                    action = commander.Order(frame, Abilities.MOVE, unshielded.Position.ToPoint2D());
                    return true;
                }
            }
            if (commander.UnitCalculation.Unit.Shield < 20)
            {
                if (AvoidDamage(commander, target, bestTarget, defensivePoint, frame, out action))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitRole == UnitRole.SaveEnergy) { return false; }

            if (GuardianShield(commander, frame, out action))
            {
                TagService.TagAbility("guardian");
                return true;
            }

            if (Hallucinate(commander, frame, out action))
            {
                TagService.TagAbility("hallucinate");
                return true;
            }

            return false;
        }

        protected virtual bool GuardianShield(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.GUARDIANSHIELD) || commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            if (LastGuardianShieldActivationFrame + 10 > frame)
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.Range > 2.5f && e.FrameLastSeen == frame && e.EnemiesInRange.Any(a => !a.Unit.BuffIds.Contains((uint)Buffs.GUARDIANSHIELD))) > 5)
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.EFFECT_GUARDIANSHIELD);
                LastGuardianShieldActivationFrame = frame;
                return true;
            }
            return false;
        }

        protected virtual bool Hallucinate(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < 75)
            {
                return false;
            }

            var height = MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos);
            if (!commander.UnitCalculation.NearbyAllies.Any(a => a.Unit.IsFlying || a.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS))
            {
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && MapDataService.MapHeight(e.Unit.Pos) > height) && commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame) > 2)
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.HALLUCINATION_COLOSSUS);
                    return true;
                }
            }

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && e.FrameLastSeen == frame) > 3 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Detector)))
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.HALLUCINATION_ARCHON);
                return true;
            }
            return false;
        }

        public override bool ContinueInRangeAttack(UnitCommander commander, int frame, out List<SC2Action> action)
        {
            action = null;
            return false;
        }
    }
}
