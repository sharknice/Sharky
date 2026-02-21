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

        public List<SC2Action> CancelBuildings()
        {
            var commands = new List<SC2Action>();

            foreach (var commander in ActiveUnitData.Commanders)
            {
                var unitCalculation = commander.Value.UnitCalculation;
                if (unitCalculation.Unit.BuildProgress < 1 && unitCalculation.Attributes.Contains(SC2Attribute.Structure))
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
                        if (unitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && unitCalculation.Unit.BuildProgress > .9f && unitCalculation.Unit.Shield < 1 && unitCalculation.EnemiesThreateningDamage.Sum(e => e.Damage) >= 25)
                        {
                            if (unitCalculation.NearbyAllies.Any(c => c.Unit.BuildProgress > .9f && !c.Unit.IsPowered && (c.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON || c.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY) && Vector2.DistanceSquared(c.Position, unitCalculation.Position) < 50))
                            {
                                continue;
                            }
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
