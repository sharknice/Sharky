using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.MacroServices
{
    public class BuildAddOnSwapService
    {
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;

        public BuildAddOnSwapService(MacroData macroData, ActiveUnitData activeUnitData)
        {
            MacroData = macroData;
            ActiveUnitData = activeUnitData;
        }

        public IEnumerable<Action> BuildAndSwapAddons()
        {
            var commands = new List<Action>();

            foreach (var pair in MacroData.AddOnSwaps)
            {
                if (pair.Value.Started && !pair.Value.Completed)
                {
                    UpdateCommanders(pair.Value);
                    CheckCompletion(pair.Value);
                    if (!pair.Value.Completed)
                    {
                        commands.AddRange(SwapBuildings(pair.Value));
                    }
                }
            }

            return commands;
        }

        private void CheckCompletion(AddOnSwap addOnSwap)
        {
            if (addOnSwap.AddOn != null && addOnSwap.AddOnBuilder != null && addOnSwap.AddOnTaker != null)
            {
                if (addOnSwap.AddOnBuilder.UnitCalculation.Unit.BuildProgress == 1 && addOnSwap.AddOnTaker.UnitCalculation.Unit.BuildProgress == 1)
                {
                    if (!addOnSwap.AddOnBuilder.UnitCalculation.UnitTypeData.Name.Contains("Flying") && addOnSwap.AddOnTaker.UnitCalculation.Unit.HasAddOnTag)
                    {
                        addOnSwap.Completed = true;
                    }
                }
            }
        }

        List<Action> SwapBuildings(AddOnSwap addOnSwap)
        {
            var commands = new List<Action>();

            if (addOnSwap.AddOnBuilder != null && addOnSwap.AddOn != null && addOnSwap.AddOn.UnitCalculation.Unit.BuildProgress == 1)
            {
                List<Action> command;
                if (addOnSwap.AddOnBuilder.UnitCalculation.UnitTypeData.Name.Contains("Flying") && addOnSwap.TakerLocation != null)
                {
                    if (addOnSwap.AddOnTaker != null &&
                        (addOnSwap.AddOnTaker.UnitCalculation.UnitTypeData.Name.Contains("Flying") || addOnSwap.AddOnTaker.UnitCalculation.Position.X != addOnSwap.TakerLocation.X || addOnSwap.AddOnTaker.UnitCalculation.Position.Y != addOnSwap.TakerLocation.Y))
                    {
                        command = addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.LAND, addOnSwap.TakerLocation);
                    }
                    else
                    {
                        command = addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.MOVE, addOnSwap.TakerLocation);
                    }
                }
                else
                {
                    command = addOnSwap.AddOnBuilder.Order(MacroData.Frame, Abilities.LIFT);
                }

                if (command != null)
                {
                    commands.AddRange(command);
                }
            }

            if (addOnSwap.AddOnTaker != null && addOnSwap.AddOnTaker.UnitCalculation.Unit.BuildProgress == 1 && !addOnSwap.AddOnTaker.UnitCalculation.Unit.HasAddOnTag)
            {
                List<Action> command;
                if (addOnSwap.AddOnTaker.UnitCalculation.UnitTypeData.Name.Contains("Flying"))
                {
                    if (addOnSwap.AddOn != null && addOnSwap.AddOn.UnitCalculation.Unit.BuildProgress == 1 && 
                        (addOnSwap.AddOnBuilder.UnitCalculation.UnitTypeData.Name.Contains("Flying") || addOnSwap.AddOnBuilder.UnitCalculation.Position.X != addOnSwap.Location.X || addOnSwap.AddOnBuilder.UnitCalculation.Position.Y != addOnSwap.Location.Y))
                    {
                        command = addOnSwap.AddOnTaker.Order(MacroData.Frame, Abilities.LAND, addOnSwap.Location);
                    }
                    else
                    {
                        command = addOnSwap.AddOnTaker.Order(MacroData.Frame, Abilities.MOVE, addOnSwap.Location);
                    }
                }
                else
                {
                    command = addOnSwap.AddOnTaker.Order(MacroData.Frame, Abilities.LIFT);
                }

                if (command != null)
                {
                    commands.AddRange(command);
                }
            }

            return commands;
        }

        void UpdateCommanders(AddOnSwap addOnSwap)
        {
            if (addOnSwap.AddOnBuilder == null)
            {
                addOnSwap.AddOnBuilder = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.DesiredAddOnBuilder);
                if (addOnSwap.AddOnBuilder != null)
                {
                    addOnSwap.Location = new Point2D { X = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.Y };
                }
            }
            else if (addOnSwap.AddOnBuilder.UnitCalculation.Unit.HasAddOnTag)
            {
                addOnSwap.Location = new Point2D { X = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOnBuilder.UnitCalculation.Unit.Pos.Y };
            }

            if (addOnSwap.AddOnTaker == null)
            {
                addOnSwap.AddOnTaker = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.DesiredAddOnTaker);
                if (addOnSwap.AddOnTaker != null)
                {
                    addOnSwap.TakerLocation = new Point2D { X = addOnSwap.AddOnTaker.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOnTaker.UnitCalculation.Unit.Pos.Y };
                }
            }
            if (addOnSwap.AddOn == null && addOnSwap.AddOnBuilder != null && addOnSwap.AddOnBuilder.UnitCalculation.Unit.HasAddOnTag)
            {
                addOnSwap.AddOn = ActiveUnitData.Commanders.Values.FirstOrDefault(c => c.UnitCalculation.Unit.UnitType == (uint)addOnSwap.AddOnType && c.UnitCalculation.Unit.Tag == addOnSwap.AddOnBuilder.UnitCalculation.Unit.AddOnTag);
                if (addOnSwap.AddOn != null)
                {
                    addOnSwap.AddOnLocation = new Point2D { X = addOnSwap.AddOn.UnitCalculation.Unit.Pos.X, Y = addOnSwap.AddOn.UnitCalculation.Unit.Pos.Y };
                }
            }
        }
    }
}
