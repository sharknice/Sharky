using SC2APIProtocol;
using Sharky.Managers;
using System.Collections.Generic;
using System.Numerics;

namespace Sharky
{
    public class UnitCommander
    {
        public bool Claimed;
        public UnitCalculation UnitCalculation;

        public UnitCalculation BestTarget { get; set; }

        public int RetreatPathFrame { get; set; }
        public IEnumerable<Vector2> RetreatPath { get; set; }
        public int LastOrderFrame { get; private set; }

        Abilities LastAbility;
        Point2D LastTargetLocation;
        ulong LastTargetTag;

        Dictionary<Abilities, int> AbilityOrderTimes;
        int SpamFrames = 10;

        public UnitCommander(UnitCalculation unitCalculation)
        {
            UnitCalculation = unitCalculation;

            BestTarget = null;
            Claimed = false;

            LastAbility = Abilities.INVALID;
            LastTargetLocation = null;
            LastTargetTag = 0;
            AbilityOrderTimes = new Dictionary<Abilities, int>();
            RetreatPathFrame = 0;
            RetreatPath = new List<Vector2>();

            LastOrderFrame = 0;
        }

        public Action Order(int frame, Abilities ability, Point2D targetLocation = null, ulong targetTag = 0, bool allowSpam = false)
        {
            if (!allowSpam && ability == LastAbility && targetTag == LastTargetTag && ((targetLocation == null && LastTargetLocation == null) || (targetLocation.X == LastTargetLocation.X && targetLocation.Y == LastTargetLocation.Y)) && AbilityOrderTimes[ability] > frame + SpamFrames)
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
            AbilityOrderTimes[ability] = frame;

            var action = new Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = command
                }
            };

            LastOrderFrame = frame;

            return action;
        }

        public Action Merge(ulong targetTag)
        {
            var command = new ActionRawUnitCommand();
            command.AbilityId = (int)Abilities.MORPH_ARCHON;
            command.UnitTags.Add(UnitCalculation.Unit.Tag);
            command.UnitTags.Add(targetTag);

            var action = new Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = command
                }
            };

            return action;
        }

        public bool AbilityOffCooldown(Abilities ability, int frame, float framesPerSecond, UnitDataManager unitDataManager)
        {
            if (AbilityOrderTimes.ContainsKey(ability))
            {
                return (frame - AbilityOrderTimes[ability]) / framesPerSecond > unitDataManager.AbilityCooldownTimes[ability];
            }
            return true;
        }

        public bool WarpInOffCooldown(int frame, float framesPerSecond, UnitDataManager unitDataManager)
        {
            foreach (var warpIn in unitDataManager.WarpInCooldownTimes)
            {
                if (AbilityOrderTimes.ContainsKey(warpIn.Key))
                {
                    if ((frame - AbilityOrderTimes[warpIn.Key]) / framesPerSecond < unitDataManager.WarpInCooldownTimes[warpIn.Key])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
