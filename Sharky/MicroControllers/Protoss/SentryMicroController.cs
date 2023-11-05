namespace Sharky.MicroControllers.Protoss
{
    public class SentryMicroController : IndividualMicroController
    {
        public SentryMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (commander.UnitCalculation.Unit.Shield < 20)
            {
                if (AvoidDamage(commander, target, defensivePoint, frame, out action))
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

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.Range > 1 && e.FrameLastSeen == frame && e.EnemiesInRange.Any()) > 5)
            {
                CameraManager.SetCamera(commander.UnitCalculation.Position);
                action = commander.Order(frame, Abilities.EFFECT_GUARDIANSHIELD);
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
                if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && MapDataService.MapHeight(e.Unit.Pos) > height) && commander.UnitCalculation.NearbyEnemies.Count(e => e.FrameLastSeen == frame) > 2)
                {
                    CameraManager.SetCamera(commander.UnitCalculation.Position);
                    action = commander.Order(frame, Abilities.HALLUCINATION_COLOSSUS);
                    return true;
                }
            }

            if (commander.UnitCalculation.NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.FrameLastSeen == frame) > 3 && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Detector)))
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
