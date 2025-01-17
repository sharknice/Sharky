using SC2APIProtocol;
using Sharky;
using SC2Action = SC2APIProtocol.Action;

namespace SharkyMachineLearningExample.Action
{
    public class ActionService
    {
        public int GetActionSpaceSize()
        {
            const int actionsPerUnit = 5;
            const int maxUnits = 5;
            return maxUnits * actionsPerUnit;
        }

        public class MultiUnitAction
        {
            public List<UnitAction> UnitActions { get; set; }
        }

        public List<SC2Action> GetSC2Actions(float[] encodedAction)
        {
            var multiUnitActions = DecodeMultiUnitAction(encodedAction);
            return multiUnitActions.UnitActions.Select(a => GetSC2Action(a)).ToList();
        }

        private SC2Action GetSC2Action(UnitAction unitAction)
        {
            var command = new ActionRawUnitCommand();
            command.UnitTags.Add(unitAction.UnitTag);

            if (unitAction.Type == ActionType.Move)
            {
                command.AbilityId = (int)Abilities.MOVE;
                command.TargetWorldSpacePos = new Point2D { X = unitAction.TargetX, Y = unitAction.TargetY };
            }
            else if (unitAction.Type == ActionType.AttackUnit)
            {
                command.AbilityId = (int)Abilities.ATTACK;
                command.TargetUnitTag = (ulong)unitAction.TargetUnitTag;
            }
            else if (unitAction.Type == ActionType.AttackPosition)
            {
                command.AbilityId = (int)Abilities.ATTACK;
                command.TargetWorldSpacePos = new Point2D { X = unitAction.TargetX, Y = unitAction.TargetY };
            }

            var action = new SC2Action
            {
                ActionRaw = new ActionRaw
                {
                    UnitCommand = command
                }
            };

            return action;
        }

        public float[] EncodeMultiUnitAction(MultiUnitAction multiAction)
        {
            const int actionsPerUnit = 5; // Increased to 5 to accommodate 64-bit unit tags
            const int maxUnits = 5; // right now it's 5v5, but may need to adjust this in the future

            float[] encodedAction = new float[maxUnits * actionsPerUnit];

            for (int i = 0; i < maxUnits; i++)
            {
                int baseIndex = i * actionsPerUnit;
                if (i < multiAction.UnitActions.Count)
                {
                    var action = multiAction.UnitActions[i];
                    encodedAction[baseIndex] = (float)(action.UnitTag & 0xFFFFFFFF); // Lower 32 bits
                    encodedAction[baseIndex + 1] = (float)(action.UnitTag >> 32); // Upper 32 bits
                    encodedAction[baseIndex + 2] = (float)action.Type;
                    encodedAction[baseIndex + 3] = action.TargetX;
                    encodedAction[baseIndex + 4] = action.TargetY;

                    if (action.Type == ActionType.AttackUnit)
                    {
                        encodedAction[baseIndex + 3] = (float)(action.TargetUnitTag.Value & 0xFFFFFFFF);
                        encodedAction[baseIndex + 4] = (float)(action.TargetUnitTag.Value >> 32);
                    }
                }
                else
                {
                    // Fill with placeholder values for unused unit slots
                    encodedAction[baseIndex] = -1; // Indicates no action for this unit
                    encodedAction[baseIndex + 1] = -1;
                    encodedAction[baseIndex + 2] = 0;
                    encodedAction[baseIndex + 3] = 0;
                    encodedAction[baseIndex + 4] = 0;
                }
            }

            return encodedAction;
        }

        private MultiUnitAction DecodeMultiUnitAction(float[] encodedAction)
        {
            const int actionsPerUnit = 5;
            const int maxUnits = 5;

            MultiUnitAction multiAction = new MultiUnitAction { UnitActions = new List<UnitAction>() };

            for (int i = 0; i < maxUnits; i++)
            {
                int baseIndex = i * actionsPerUnit;
                if (encodedAction[baseIndex] != -1) // Check if there's an action for this unit
                {
                    UnitAction action = new UnitAction
                    {
                        UnitTag = ((ulong)(uint)encodedAction[baseIndex + 1] << 32) | (uint)encodedAction[baseIndex],
                        Type = (ActionType)(int)encodedAction[baseIndex + 2],
                        TargetX = encodedAction[baseIndex + 3],
                        TargetY = encodedAction[baseIndex + 4]
                    };

                    if (action.Type == ActionType.AttackUnit)
                    {
                        action.TargetUnitTag = ((ulong)(uint)encodedAction[baseIndex + 4] << 32) | (uint)encodedAction[baseIndex + 3];
                        action.TargetX = 0;
                        action.TargetY = 0;
                    }

                    multiAction.UnitActions.Add(action);
                }
            }

            return multiAction;
        }
    }
}
