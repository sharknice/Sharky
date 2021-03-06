﻿using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky
{
    public class UnitCommander
    {
        public bool Claimed;
        public UnitCalculation UnitCalculation;

        public UnitCalculation BestTarget { get; set; }
        public UnitRole UnitRole { get; set; }

        public int RetreatPathFrame { get; set; }
        public List<Vector2> RetreatPath { get; set; }
        public int RetreatPathIndex { get; set; }
        public int LastOrderFrame { get; private set; }

        public bool SkipFrame { get; set; }

        public Abilities LastAbility { get; private set; }
        public Point2D LastTargetLocation { get; private set; }
        public ulong LastTargetTag { get; private set; }

        Dictionary<Abilities, int> AbilityOrderTimes;
        public Dictionary<ulong, int> LoadTimes;

        int SpamFrames = 100;

        public UnitCommander(UnitCalculation unitCalculation)
        {
            UnitCalculation = unitCalculation;

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

            LastOrderFrame = 0;
        }

        public List<Action> Order(int frame, Abilities ability, Point2D targetLocation = null, ulong targetTag = 0, bool allowSpam = false)
        {
            if (!allowSpam && ability == LastAbility && targetTag == LastTargetTag && ((targetLocation == null && LastTargetLocation == null) || (targetLocation.X == LastTargetLocation.X && targetLocation.Y == LastTargetLocation.Y)) && AbilityOrderTimes[ability] > frame - SpamFrames)
            {
                return new List<Action>(); // if new action is exactly the same, don't do anything to prevent apm spam
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

            return new List<Action> { action };
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
                    if ((frame - AbilityOrderTimes[warpIn.Key] - 10) / framesPerSecond < sharkyUnitData.WarpInCooldownTimes[warpIn.Key])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<Action> UnloadSpecificUnit(int frame, Abilities ability, ulong targetTag, bool allowSpam = false)
        {
            if (!allowSpam && ability == LastAbility && targetTag == LastTargetTag && AbilityOrderTimes[ability] > frame - SpamFrames)
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
            var selectAction = new Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = selectionCommand
                },
            };

            var unloadAction = new Action
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

            return new List<Action> { selectAction, unloadAction };
        }
    }
}
