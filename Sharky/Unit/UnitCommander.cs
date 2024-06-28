﻿namespace Sharky
{
    public class UnitCommander
    {
        public bool Claimed;
        public UnitCalculation UnitCalculation;

        public UnitCalculation BestTarget { get; set; }
        public UnitRole UnitRole { get; set; }

        public PathData CurrentPath { get; set; }
        public int CurrentPathIndex { get; set; }

        public int RetreatPathFrame { get; set; }
        public List<Vector2> RetreatPath { get; set; }
        public int RetreatPathIndex { get; set; }
        public int LastOrderFrame { get; private set; }
        public int FrameFirstSeen { get; private set; }

        public bool SkipFrame { get; set; }

        public Abilities LastAbility { get; private set; }
        public Point2D LastTargetLocation { get; private set; }
        public ulong LastTargetTag { get; private set; }
        public int LastInRangeAttackFrame { get; set; }
        public CommanderState CommanderState { get; set; }
        public LockOnData LastLockOn { get; set; }
        public bool AutoCastToggled { get; set; }
        public bool RallyPointSet { get; set; }
        public bool AlwaysSpam { get; set; }

        public StuckCheck StuckCheck { get; set; } 

        /// <summary>
        /// The adept for an adept shade, etc.
        /// </summary>
        public UnitCalculation ParentUnitCalculation { get; set; }

        /// <summary>
        /// The shade for an adept, etc.
        /// </summary>
        public UnitCalculation ChildUnitCalculation { get; set; }

        Dictionary<Abilities, int> AbilityOrderTimes;
        public Dictionary<ulong, int> LoadTimes;

        int SpamFrames = 100;

        public UnitCommander(UnitCalculation unitCalculation)
        {
            UnitCalculation = unitCalculation;

            ParentUnitCalculation = null;
            BestTarget = null;
            Claimed = false;

            LastAbility = Abilities.INVALID;
            LastTargetLocation = null;
            LastTargetTag = 0;
            AbilityOrderTimes = new Dictionary<Abilities, int>();
            LoadTimes = new Dictionary<ulong, int>();
            RetreatPathFrame = 0;
            RetreatPath = new List<Vector2>();
            RetreatPathIndex = 0;

            LastInRangeAttackFrame = -100;
            LastOrderFrame = -100;
            FrameFirstSeen = unitCalculation.FrameLastSeen;
            AutoCastToggled = false;
            RallyPointSet = false;
            AlwaysSpam = false;

            StuckCheck = new StuckCheck { AttemptFrame = 0, StuckPosition = unitCalculation.Position };
        }

        public List<SC2Action> Order(int frame, Abilities ability, Point2D targetLocation = null, ulong targetTag = 0, bool allowSpam = false, bool queue = false, bool allowConflict = false)
        {
            if (!allowConflict)
            {
                if (!queue && LastOrderFrame == frame)
                {
                    return new List<SC2Action>(); // don't give a unit conflicting orders, only one order per frame
                }
                if (!allowSpam && !AlwaysSpam)
                {
                    if (ability == LastAbility && targetTag == LastTargetTag && ((targetLocation == null && LastTargetLocation == null) || (LastTargetLocation != null && targetLocation != null && targetLocation.X == LastTargetLocation.X && targetLocation.Y == LastTargetLocation.Y)) && AbilityOrderTimes[ability] > frame - SpamFrames)
                    {
                        LastOrderFrame = frame;
                        return new List<SC2Action>(); // if new action is exactly the same, don't do anything to prevent apm spam
                    }
                    else
                    {
                        UnitOrder unitOrder;
                        if (queue)
                        {
                            unitOrder = UnitCalculation.Unit.Orders.FirstOrDefault(o => EquivalentAbility(ability, o.AbilityId));
                        }
                        else
                        {
                            unitOrder = UnitCalculation.Unit.Orders.FirstOrDefault();
                        }
                        if (unitOrder != null && EquivalentAbility(ability, unitOrder.AbilityId) && targetTag == unitOrder.TargetUnitTag && ((targetLocation == null && unitOrder.TargetWorldSpacePos == null) || (targetLocation != null && Math.Abs(targetLocation.X - unitOrder.TargetWorldSpacePos.X) < .01 && Math.Abs(targetLocation.Y - unitOrder.TargetWorldSpacePos.Y) < .01)))
                        {
                            LastOrderFrame = frame;
                            return new List<SC2APIProtocol.Action>(); // if new action is exactly the same, don't do anything to prevent apm spam
                        }
                    }
                }
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

            var action = new SC2Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = command
                }
            };

            if (queue)
            {
                action.ActionRaw.UnitCommand.QueueCommand = true;
            }

            LastOrderFrame = frame;
            PerformStuckCheck(frame, targetLocation);

            return new List<SC2Action> { action };
        }

        private void PerformStuckCheck(int frame, Point2D targetLocation)
        {
            if (targetLocation == null || Vector2.DistanceSquared(UnitCalculation.Position, StuckCheck.StuckPosition) > 4 || Vector2.DistanceSquared(UnitCalculation.Position, targetLocation.ToVector2()) < 4)
            {
                StuckCheck.AttemptFrame = 0;
                StuckCheck.StuckPosition = UnitCalculation.Position;
            }
            else if (StuckCheck.AttemptFrame == 0)
            {
                if (targetLocation != null && Vector2.DistanceSquared(UnitCalculation.Position, targetLocation.ToVector2()) > 4)
                {
                    StuckCheck.AttemptFrame = frame;
                    StuckCheck.StuckPosition = UnitCalculation.Position;
                }
            }
        }

        public List<SC2Action> ToggleAutoCast(Abilities ability)
        {
            var command = new ActionRawToggleAutocast();
            command.UnitTags.Add(UnitCalculation.Unit.Tag);
            command.AbilityId = (int)ability;

            var action = new SC2Action
            {
                ActionRaw = new ActionRaw
                {
                    ToggleAutocast = command
                }
            };

            return new List<SC2Action> { action };
        }

        public SC2Action Merge(ulong targetTag)
        {
            var command = new ActionRawUnitCommand();
            command.AbilityId = (int)Abilities.MORPH_ARCHON;
            command.UnitTags.Add(UnitCalculation.Unit.Tag);
            command.UnitTags.Add(targetTag);

            var action = new SC2Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = command
                }
            };

            return action;
        }

        public float AbilityCooldown(Abilities ability, int frame, float framesPerSecond, SharkyUnitData sharkyUnitData)
        {
            if (AbilityOrderTimes.ContainsKey(ability))
            {
                var time = (frame - AbilityOrderTimes[ability]) / framesPerSecond;
                if (time > sharkyUnitData.AbilityCooldownTimes[ability])
                {
                    return 0;
                }
                else
                {
                    return time / sharkyUnitData.AbilityCooldownTimes[ability];
                }
            }
            return 0;
        }

        public bool AbilityOffCooldown(Abilities ability, int frame, float framesPerSecond, SharkyUnitData sharkyUnitData)
        {
            if (AbilityOrderTimes.ContainsKey(ability))
            {
                return (frame - AbilityOrderTimes[ability]) / framesPerSecond > sharkyUnitData.AbilityCooldownTimes[ability];
            }
            return true;
        }

        public bool WarpInOffCooldown(int frame, float framesPerSecond, SharkyUnitData sharkyUnitData)
        {
            foreach (var warpIn in sharkyUnitData.WarpInCooldownTimes)
            {
                if (AbilityOrderTimes.ContainsKey(warpIn.Key))
                {
                    if ((frame - AbilityOrderTimes[warpIn.Key]) / framesPerSecond < sharkyUnitData.WarpInCooldownTimes[warpIn.Key])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool WarpInAlmostOffCooldown(int frame, float framesPerSecond, SharkyUnitData sharkyUnitData)
        {
            foreach (var warpIn in sharkyUnitData.WarpInCooldownTimes)
            {
                if (AbilityOrderTimes.ContainsKey(warpIn.Key))
                {
                    if (sharkyUnitData.WarpInCooldownTimes[warpIn.Key] - (frame - AbilityOrderTimes[warpIn.Key]) / framesPerSecond >= 2f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<SC2APIProtocol.Action> UnloadSpecificUnit(int frame, Abilities ability, ulong targetTag, bool allowSpam = false)
        {
            if (!allowSpam && !AlwaysSpam && ability == LastAbility && targetTag == LastTargetTag && AbilityOrderTimes[ability] > frame - SpamFrames)
            {
                return null; // if new action is exactly the same, don't do anything to prevent apm spam
            }

            var passenger = UnitCalculation.Unit.Passengers.Where(p => p.Tag == targetTag).FirstOrDefault();
            if (passenger == null)
            {
                return null;
            }

            var selectionCommand = new ActionRawUnitCommand();
            selectionCommand.UnitTags.Add(UnitCalculation.Unit.Tag);
            selectionCommand.AbilityId = 0;
            var selectAction = new SC2APIProtocol.Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = selectionCommand
                },
            };

            var unloadAction = new SC2APIProtocol.Action
            {
                ActionUi = new ActionUI
                {
                    CargoPanel = new ActionCargoPanelUnload { UnitIndex = UnitCalculation.Unit.Passengers.IndexOf(passenger) }
                },
            };

            LastAbility = ability;
            LastTargetLocation = null;
            LastTargetTag = targetTag;
            AbilityOrderTimes[ability] = frame;
            LastOrderFrame = frame;

            return new List<SC2APIProtocol.Action> { selectAction, unloadAction };
        }

        bool EquivalentAbility(Abilities abilities, uint abilityId)
        {
            if ((uint)abilities == abilityId)
            {
                return true;
            }
            if (abilities == Abilities.HARVEST_RETURN)
            {
                if ((uint)Abilities.HARVEST_RETURN_DRONE == abilityId || (uint)Abilities.HARVEST_RETURN_PROBE == abilityId || (uint)Abilities.HARVEST_RETURN_SCV == abilityId || (uint)Abilities.HARVEST_RETURN_MULE == abilityId)
                {
                    return true;
                }
            }
            if (abilities == Abilities.HARVEST_GATHER)
            {
                if ((uint)Abilities.HARVEST_GATHER_DRONE == abilityId || (uint)Abilities.HARVEST_GATHER_PROBE == abilityId || (uint)Abilities.HARVEST_GATHER_SCV == abilityId)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"UnitCommander: {(UnitTypes)UnitCalculation.Unit.UnitType} UnitRole.{UnitRole} <{ StringHelpers.ToString(UnitCalculation.Unit.Orders.LastOrDefault())}> ({UnitCalculation.Unit.Tag})";
        }
    }
}
