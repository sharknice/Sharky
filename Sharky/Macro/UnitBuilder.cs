using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Macro
{
    public class UnitBuilder
    {
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        AttackData AttackData;

        UnitCountService UnitCountService;
        SharkyOptions SharkyOptions;

        IBuildingPlacement WarpInPlacement;

        public UnitBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingPlacement warpInPlacement)
        {
            MacroData = defaultSharkyBot.MacroData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            AttackData = defaultSharkyBot.AttackData;
            UnitCountService = defaultSharkyBot.UnitCountService;
            SharkyOptions = defaultSharkyBot.SharkyOptions;

            WarpInPlacement = warpInPlacement;
        }

        public List<SC2APIProtocol.Action> ProduceUnits()
        {
            var commands = new List<SC2APIProtocol.Action>();
            foreach (var unit in MacroData.BuildUnits)
            {
                if (unit.Value && unit.Key != UnitTypes.PROTOSS_ARCHON)
                {
                    var unitData = SharkyUnitData.TrainingData[unit.Key];
                    if ((unitData.Food == 0 || unitData.Food <= MacroData.FoodLeft) && unitData.Minerals <= MacroData.Minerals && unitData.Gas <= MacroData.VespeneGas)
                    {
                        var building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && (!c.Value.UnitCalculation.Unit.IsActive || !c.Value.UnitCalculation.Attributes.Contains(Attribute.Structure)) && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.WarpInOffCooldown(MacroData.Frame, SharkyOptions.FramesPerSecond, SharkyUnitData));

                        if (unitData.ProducingUnits.Contains(UnitTypes.TERRAN_BARRACKS) || unitData.ProducingUnits.Contains(UnitTypes.TERRAN_FACTORY) || unitData.ProducingUnits.Contains(UnitTypes.TERRAN_STARPORT))
                        {
                            if (unitData.RequiresTechLab)
                            {
                                building = building.Where(b => b.Value.UnitCalculation.Unit.HasAddOnTag && SharkyUnitData.TechLabTypes.Contains((UnitTypes)ActiveUnitData.SelfUnits[b.Value.UnitCalculation.Unit.AddOnTag].Unit.UnitType));
                            }
                            else
                            {
                                building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.UnitCalculation.Unit.HasAddOnTag && // reactors first
                                        SharkyUnitData.ReactorTypes.Contains((UnitTypes)ActiveUnitData.SelfUnits[c.Value.UnitCalculation.Unit.AddOnTag].Unit.UnitType) && c.Value.UnitCalculation.Unit.Orders.Count() <= 1);
                                if (building.Count() == 0)
                                {
                                    building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && !c.Value.UnitCalculation.Unit.HasAddOnTag); // no add on second
                                    if (building.Count() == 0)
                                    {
                                        building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1); // tech lab last
                                    }
                                }
                            }
                        }


                        if (building.Count() > 0)
                        {
                            if (building.First().Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY && SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.WARPGATERESEARCH))
                            {
                                var action = building.First().Value.Order(MacroData.Frame, Abilities.RESEARCH_WARPGATE);
                                if (action != null)
                                {
                                    commands.AddRange(action);
                                    return commands;
                                }
                            }
                            else if (building.First().Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE)
                            {
                                var targetLocation = TargetingData.ForwardDefensePoint;
                                var undefendedNexus = ActiveUnitData.SelfUnits.FirstOrDefault(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && u.Value.NearbyEnemies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit)) && !u.Value.NearbyAllies.Any(a => a.UnitClassifications.Contains(UnitClassification.ArmyUnit))).Value;
                                if (undefendedNexus != null)
                                {
                                    targetLocation = new Point2D { X = undefendedNexus.Position.X, Y = undefendedNexus.Position.Y };
                                }
                                else if (AttackData.Attacking)
                                {
                                    targetLocation = AttackData.ArmyPoint;
                                }

                                var location = WarpInPlacement.FindPlacement(targetLocation, unit.Key, 1);
                                if (location != null)
                                {
                                    var action = building.First().Value.Order(MacroData.Frame, unitData.WarpInAbility, location);
                                    if (action != null)
                                    {
                                        commands.AddRange(action);
                                        return commands;
                                    }
                                }
                            }
                            else
                            {
                                var allowSpam = false;
                                if (building.First().Value.UnitCalculation.Unit.HasAddOnTag && SharkyUnitData.ReactorTypes.Contains((UnitTypes)ActiveUnitData.SelfUnits[building.First().Value.UnitCalculation.Unit.AddOnTag].Unit.UnitType))
                                {
                                    allowSpam = true;
                                }
                                var action = building.First().Value.Order(MacroData.Frame, unitData.Ability, allowSpam: allowSpam);
                                if (action != null)
                                {
                                    commands.AddRange(action);
                                    return commands;
                                }
                            }
                        }
                    }
                }
                else if (unit.Value && unit.Key == UnitTypes.PROTOSS_ARCHON)
                {
                    var templar = ActiveUnitData.Commanders.Where(c => c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DARKTEMPLAR);
                    var merges = templar.Count(a => a.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON));
                    if (merges + UnitCountService.Count(UnitTypes.PROTOSS_ARCHON) < MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON])
                    {
                        var mergables = templar.Where(c => !c.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON || o.AbilityId == (uint)Abilities.MORPH_ARCHON2));
                        if (mergables.Count() >= 2)
                        {
                            var commanders = mergables.OrderBy(c => c.Value.UnitCalculation.Unit.Energy).Take(2);
                            var action = commanders.First().Value.Merge(commanders.Last().Value.UnitCalculation.Unit.Tag);
                            if (action != null)
                            {
                                commanders.First().Value.UnitRole = UnitRole.Morph;
                                commanders.Last().Value.UnitRole = UnitRole.Morph;
                                commands.Add(action);
                                return commands;
                            }
                        }
                    }
                }
            }

            return commands;
        }
    }
}
