using SC2APIProtocol;

namespace Sharky
{
    public class UnitCommander
    {
        public bool Claimed;
        public UnitCalculation UnitCalculation;

        UnitCalculation BestTarget;

        Abilities LastAbility;
        Point2D LastTargetLocation;
        ulong LastTargetTag;

        public UnitCommander(UnitCalculation unitCalculation)
        {
            UnitCalculation = unitCalculation;

            BestTarget = null;
            Claimed = false;

            LastAbility = Abilities.INVALID;
            LastTargetLocation = null;
            LastTargetTag = 0;
        }

        public ActionRawUnitCommand Order(Abilities ability, Point2D targetLocation = null, ulong targetTag = 0, bool allowSpam = false)
        {
            if (!allowSpam && ability == LastAbility && targetTag == LastTargetTag && ((targetLocation == null && LastTargetLocation == null) || (targetLocation.X == LastTargetLocation.X && targetLocation.Y == LastTargetLocation.Y)))
            {
                return null; // if new action is exactly the same, don't do anything to prevent apm spam
            }

            var command = new ActionRawUnitCommand();
            command.UnitTags.Add(UnitCalculation.Unit.Tag);
            command.AbilityId = (int)ability;
            if (targetLocation != null)
            {
                command.TargetWorldSpacePos = targetLocation;
            }
            if (targetTag != 0)
            {
                command.TargetUnitTag = targetTag;
            }

            LastAbility = ability;
            LastTargetLocation = targetLocation;
            LastTargetTag = targetTag;

            return command;
        }
    }
}
