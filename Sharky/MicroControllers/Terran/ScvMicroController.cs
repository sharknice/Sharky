namespace Sharky.MicroControllers.Terran
{
    public class ScvMicroController : IndividualWorkerMicroController
    {
        MacroData MacroData;

        public ScvMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            MacroData = defaultSharkyBot.MacroData;
        }

        protected bool Repair(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (MacroData.Minerals < 5) { return false; }

            IOrderedEnumerable<UnitCalculation> repairTargets = null;
            if (supportTargets != null)
            {
                repairTargets = supportTargets.Select(c => c.UnitCalculation).Where(a => a.Attributes.Contains(SC2Attribute.Mechanical) && a.Unit.BuildProgress == 1 && a.Unit.Health < a.Unit.HealthMax).OrderByDescending(a => a.Unit.HealthMax - a.Unit.Health);
            }
            if (repairTargets == null || repairTargets.Count() == 0)
            {
                repairTargets = commander.UnitCalculation.NearbyAllies.Where(a => a.Attributes.Contains(SC2Attribute.Mechanical) && a.Unit.BuildProgress == 1 && a.Unit.Health < a.Unit.HealthMax).OrderBy(a => Vector2.DistanceSquared(a.Position, commander.UnitCalculation.Position));
            }

            var repairTarget = repairTargets.FirstOrDefault();
            
            if (repairTarget != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_REPAIR, targetTag: repairTarget.Unit.Tag);
                return true;
            }

            return false;
        }

        public override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AutoCastToggled)
            {
                action = commander.ToggleAutoCast(Abilities.EFFECT_REPAIR_SCV);
                commander.AutoCastToggled = true;
                return true;
            }

            if (commander.UnitRole == UnitRole.Support)
            {
                if (Repair(commander, null, frame, out action)) { return true; }
            }

            return false;
        }

        public override List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if ((commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax / 4) ||
                (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax && commander.UnitCalculation.EnemiesInRangeOfAvoid.Count(e => e.EnemiesInRangeOf.Count() == 0) > 0))
            {
                return Retreat(commander, defensivePoint, groupCenter, frame);
            }

            if (Repair(commander, supportTargets, frame, out action)) { return action; }

            return base.Support(commander, supportTargets, target, defensivePoint, groupCenter, frame);
        }

        public override List<SC2APIProtocol.Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            List<SC2APIProtocol.Action> action = null;
            UpdateState(commander, defensivePoint, defensivePoint, null, null, Formation.Normal, frame);
            if (Repair(commander, null, frame, out action)) { return action; }
            return action;
        }
    }
}
