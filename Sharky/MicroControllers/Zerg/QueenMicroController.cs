namespace Sharky.MicroControllers.Zerg
{
    public class QueenMicroController : IndividualMicroController
    {
        const int minMissingHealthNormal = 60;
        const int minMissingHealthLow = 20;
        const int queenHealthLow = 40;
        const float healDistance = 6.5f;
        const float healWalkDistance = 12;
        const int healEnergyCost = 50;

        public QueenMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        public override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            if (commander.UnitCalculation.Unit.Health < 50)
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
            if (Transfuse(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool Transfuse(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.Energy < healEnergyCost)
            {
                return false;
            }

            var transfuseTarget = FindTransfuseTarget(commander.UnitCalculation);
            if (transfuseTarget.Item1 != null)
            {
                CameraManager.SetCamera(transfuseTarget.Item1.Pos);
                TagService.TagAbility("transfuse");
                action = commander.Order(frame, Abilities.EFFECT_TRANSFUSION, targetTag: transfuseTarget.Item1.Tag);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queen"></param>
        /// <returns>Returns tuple with tag of the target unit and bool value whether the unit is a bit further and queen needs to walk to the unit first</returns>
        private (Unit?, bool) FindTransfuseTarget(UnitCalculation queen)
        {
            var unitToHeal = queen.NearbyAllies.Where(x => x.Unit.BuildProgress >= 1 && x.Position.Distance(queen.Position) < healDistance).Where(x => (x.Unit.HealthMax - x.Unit.Health > minMissingHealthLow)).OrderByDescending(x => x.Unit.HealthMax - x.Unit.Health).FirstOrDefault()?.Unit;

            bool walkToHeal = false;

            // If no unit in direct heal reach, try to find close target we can walk towards to heal
            if (unitToHeal == null)
            {
                walkToHeal = true;
                unitToHeal = queen.NearbyAllies.Where(x => x.Unit.BuildProgress >= 1 && x.Position.Distance(queen.Position) < healWalkDistance).Where(x => (x.Unit.HealthMax - x.Unit.Health > minMissingHealthLow)).OrderByDescending(x => x.Unit.HealthMax - x.Unit.Health).FirstOrDefault()?.Unit;
            }

            if (unitToHeal == null)
            {
                return (null, false);
            }

            // If the queen is low on health or likely killed soon, try to heal even targets with nearly full HP
            bool healAnything = queen.Unit.Health < queenHealthLow || queen.Unit.Health <= queen.Attackers.Sum(x => x.Damage);

            return ((unitToHeal != null && (healAnything || (unitToHeal.HealthMax - unitToHeal.Health > minMissingHealthNormal))) ? unitToHeal : null, walkToHeal);
        }

        public override float GetMovementSpeed(UnitCommander commander)
        {
            if (commander.UnitCalculation.IsOnCreep)
            {
                return 3.5f;
            }
            return base.GetMovementSpeed(commander);
        }
    }
}
