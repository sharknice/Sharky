using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds.MacroServices
{
    public class BuildingCancelService
    {
        ActiveUnitData ActiveUnitData;
        MacroData MacroData;

        public BuildingCancelService(ActiveUnitData activeUnitData, MacroData macroData)
        {
            ActiveUnitData = activeUnitData;
            MacroData = macroData;
        }

        public List<Action> CancelBuildings()
        {
            var commands = new List<Action>();

            foreach (var commander in ActiveUnitData.Commanders)
            {
                var unitCalculation = commander.Value.UnitCalculation;
                if (unitCalculation.Unit.BuildProgress < 1 && unitCalculation.Attributes.Contains(Attribute.Structure))
                {
                    if (unitCalculation.Unit.ShieldMax > 0)
                    {
                        if (unitCalculation.Unit.BuildProgress > 0.1f && unitCalculation.Unit.Shield < 0)
                        {
                            var action = commander.Value.Order(MacroData.Frame, Abilities.CANCEL);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                    }
                    else
                    {
                        if (unitCalculation.Unit.Health < 100 && unitCalculation.PreviousUnit.Health > unitCalculation.Unit.Health)
                        {
                            var action = commander.Value.Order(MacroData.Frame, Abilities.CANCEL);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                    }
                }
            }

            return commands;
        }
    }
}
