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
                        if (unitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && unitCalculation.Unit.Health > 5)
                        {
                            continue;
                        }

                        if (unitCalculation.Unit.BuildProgress > 0.1f && unitCalculation.Unit.Shield < 1 && unitCalculation.Unit.Health < 100 && unitCalculation.Unit.Health <= unitCalculation.EnemiesThreateningDamage.Sum(e => e.Damage))
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
                            if (unitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_SPINECRAWLER)
                            {
                                if (unitCalculation.EnemiesThreateningDamage.Sum(a => a.Damage) < unitCalculation.Unit.Health)
                                {
                                    continue;
                                }
                            }
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
