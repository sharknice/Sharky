using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.Builds.MacroServices;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class MacroManager : SharkyManager
    {
        MacroSetup MacroSetup;
        IUnitManager UnitManager;
        UnitDataManager UnitDataManager;
        BuildingBuilder BuildingBuilder;
        SharkyOptions SharkyOptions;
        IBaseManager BaseManager;
        TargetingManager TargetingManager;
        AttackData AttackData;
        IBuildingPlacement WarpInPlacement;
        MacroData MacroData;
        Morpher Morpher;
        BuildPylonService BuildPylonService;
        BuildDefenseService BuildDefenseService;

        public MacroManager(MacroSetup macroSetup, IUnitManager unitManager, UnitDataManager unitDataManager, BuildingBuilder buildingBuilder, SharkyOptions sharkyOptions, IBaseManager baseManager, TargetingManager targetingManager, AttackData attackData, IBuildingPlacement warpInPlacement, MacroData macroData, Morpher morpher, BuildPylonService buildPylonService, BuildDefenseService buildDefenseService)
        {
            MacroSetup = macroSetup;
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
            BuildingBuilder = buildingBuilder;
            SharkyOptions = sharkyOptions;
            BaseManager = baseManager;
            TargetingManager = targetingManager;
            AttackData = attackData;
            WarpInPlacement = warpInPlacement;

            MacroData = macroData;
            Morpher = morpher;
            BuildPylonService = buildPylonService;
            BuildDefenseService = buildDefenseService;

            MacroData.DesiredUpgrades = new Dictionary<Upgrades, bool>();
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    MacroData.Race = playerInfo.RaceActual;
                }
            }

            MacroSetup.SetupMacro(MacroData);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<Action>();

            MacroData.FoodUsed = (int)observation.Observation.PlayerCommon.FoodUsed;
            MacroData.FoodLeft = (int)observation.Observation.PlayerCommon.FoodCap - MacroData.FoodUsed;
            MacroData.FoodArmy = (int)observation.Observation.PlayerCommon.FoodArmy;
            MacroData.Minerals = (int)observation.Observation.PlayerCommon.Minerals;
            MacroData.VespeneGas = (int)observation.Observation.PlayerCommon.Vespene;
            MacroData.Frame = (int)observation.Observation.GameLoop;

            actions.AddRange(BuildPylonService.BuildPylonsAtEveryMineralLine());
            actions.AddRange(BuildPylonService.BuildPylonsAtDefensivePoint());
            actions.AddRange(BuildPylonService.BuildPylonsAtEveryBase());
            actions.AddRange(BuildSupply());

            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtEveryMineralLine());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtDefensivePoint());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtEveryBase());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildings());

            actions.AddRange(BuildVespeneGas());

            actions.AddRange(MorphBuildings());
            actions.AddRange(BuildAddOns());
            actions.AddRange(BuildProductionBuildings());
            actions.AddRange(BuildTechBuildings());

            actions.AddRange(ResearchUpgrades());
            actions.AddRange(ProduceUnits());

            return actions;
        }

        private List<Action> ResearchUpgrades()
        {
            var commands = new List<Action>();

            foreach (var upgrade in MacroData.DesiredUpgrades)
            {
                if (upgrade.Value && !UnitDataManager.ResearchedUpgrades.Contains((uint)upgrade.Key))
                {
                    var upgradeData = UnitDataManager.UpgradeData[upgrade.Key];

                    if (!UnitManager.Commanders.Any(c => upgradeData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && c.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (int)upgradeData.Ability)))
                    {
                        var building = UnitManager.Commanders.Where(c => upgradeData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.LastOrderFrame != MacroData.Frame);
                        if (building.Count() > 0)
                        {
                            if (upgradeData.Minerals <= MacroData.Minerals && upgradeData.Gas <= MacroData.VespeneGas)
                            {
                                commands.Add(building.First().Value.Order(MacroData.Frame, upgradeData.Ability));
                            }
                        }
                    }
                }
            }

            return commands;
        }

        private List<Action> ProduceUnits()
        {
            var commands = new List<Action>();
            foreach (var unit in MacroData.BuildUnits)
            {
                if (unit.Value && unit.Key != UnitTypes.PROTOSS_ARCHON)
                {
                    var unitData = UnitDataManager.TrainingData[unit.Key];
                    if ((unitData.Food == 0 || unitData.Food <= MacroData.FoodLeft) && unitData.Minerals <= MacroData.Minerals && unitData.Gas <= MacroData.VespeneGas)
                    {
                        var building = UnitManager.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.WarpInOffCooldown(MacroData.Frame, SharkyOptions.FramesPerSecond, UnitDataManager));
                        
                        if (unitData.RequiresTechLab)
                        {
                            building = building.Where(b => b.Value.UnitCalculation.Unit.HasAddOnTag && UnitDataManager.TechLabTypes.Contains((UnitTypes)UnitManager.SelfUnits[b.Value.UnitCalculation.Unit.AddOnTag].Unit.UnitType));
                        }
                        else if (building.Count() == 0)
                        {
                            if (unitData.ProducingUnits.Contains(UnitTypes.TERRAN_BARRACKS) || unitData.ProducingUnits.Contains(UnitTypes.TERRAN_FACTORY) || unitData.ProducingUnits.Contains(UnitTypes.TERRAN_STARPORT))
                            {
                                building = UnitManager.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.UnitCalculation.Unit.HasAddOnTag && 
                                    UnitDataManager.ReactorTypes.Contains((UnitTypes)UnitManager.SelfUnits[c.Value.UnitCalculation.Unit.AddOnTag].Unit.UnitType) && c.Value.UnitCalculation.Unit.Orders.Count() == 1);
                            }
                        }


                        if (building.Count() > 0)
                        {
                            if (building.First().Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY && UnitDataManager.ResearchedUpgrades.Contains((uint)Upgrades.WARPGATERESEARCH))
                            {
                                var action = building.First().Value.Order(MacroData.Frame, Abilities.RESEARCH_WARPGATE);
                                if (action != null)
                                {
                                    commands.Add(action);
                                    return commands;
                                }
                            }
                            else if (building.First().Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE)
                            {
                                var targetLocation = TargetingManager.DefensePoint;
                                if (AttackData.Attacking)
                                {
                                    targetLocation = AttackData.ArmyPoint;
                                }

                                var location = WarpInPlacement.FindPlacement(targetLocation, unit.Key, 1);
                                var action = building.First().Value.Order(MacroData.Frame, unitData.WarpInAbility, location);
                                if (action != null)
                                {
                                    commands.Add(action);
                                    return commands;
                                }
                            }
                            else
                            {
                                var action = building.First().Value.Order(MacroData.Frame, unitData.Ability);
                                if (action != null)
                                {
                                    commands.Add(action);
                                    return commands;
                                }
                            }
                        }
                    }
                }
                else if (unit.Value && unit.Key == UnitTypes.PROTOSS_ARCHON)
                {
                    var templar = UnitManager.Commanders.Where(c => c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DARKTEMPLAR);
                    var merges = templar.Count(a => a.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON));
                    if (merges + UnitManager.Count(UnitTypes.PROTOSS_ARCHON) < MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON])
                    {
                        var mergables = templar.Where(c => !c.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON || o.AbilityId == (uint)Abilities.MORPH_ARCHON + 1));
                        if (mergables.Count() >= 2)
                        {
                            var commanders = mergables.OrderBy(c => c.Value.UnitCalculation.Unit.Energy).Take(2);
                            var action = commanders.First().Value.Merge(commanders.Last().Value.UnitCalculation.Unit.Tag);
                            if (action != null)
                            {
                                commands.Add(action);
                                return commands;
                            }
                        }
                    }
                }
            }

            return commands;
        }

        private List<Action> BuildVespeneGas()
        {
            var commands = new List<Action>();
            if (MacroData.BuildGas && MacroData.Minerals >= 75)
            {
                var unitData = GetGasTypeData();
                var takenGases = UnitManager.SelfUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)).Concat(UnitManager.EnemyUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)));
                var openGeysers = BaseManager.BaseLocations.SelectMany(b => b.VespeneGeysers).Where(g => g.VespeneContents > 0 && !takenGases.Any(t => t.Value.Unit.Pos.X == g.Pos.X && t.Value.Unit.Pos.Y == g.Pos.Y));
                if (openGeysers.Count() > 0)
                {
                    var baseLocation = BuildingBuilder.GetReferenceLocation(TargetingManager.SelfMainBasePoint);
                    var closestGyeser = openGeysers.OrderBy(o => Vector2.DistanceSquared(new Vector2(baseLocation.X, baseLocation.Y), new Vector2(o.Pos.X, o.Pos.Y))).FirstOrDefault();
                    if (closestGyeser != null)
                    {
                        var command = BuildingBuilder.BuildGas(MacroData, unitData, closestGyeser);
                        if (command != null)
                        {
                            commands.Add(command);
                            return commands;
                        }
                    }
                    
                }
            }

            return commands;
        }

        private BuildingTypeData GetGasTypeData()
        {
            if (MacroData.Race == Race.Protoss)
            {
                return UnitDataManager.BuildingData[UnitTypes.PROTOSS_ASSIMILATOR];
            }
            else if (MacroData.Race == Race.Terran)
            {
                return UnitDataManager.BuildingData[UnitTypes.TERRAN_REFINERY];
            }
            else
            {
                return UnitDataManager.BuildingData[UnitTypes.ZERG_EXTRACTOR];
            }
        }

        private List<Action> BuildSupply()
        {
            var commands = new List<Action>();

            if (MacroData.BuildPylon)
            {
                var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];
                var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.PROTOSS_PYLON, unitData);
                if (command != null)
                {
                    commands.Add(command);
                    return commands;
                }
            }

            if (MacroData.BuildSupplyDepot)
            {
                var unitData = UnitDataManager.BuildingData[UnitTypes.TERRAN_SUPPLYDEPOT];
                var command = BuildingBuilder.BuildBuilding(MacroData, UnitTypes.TERRAN_SUPPLYDEPOT, unitData);
                if (command != null)
                {
                    commands.Add(command);
                    return commands;
                }
            }

            if (MacroData.BuildOverlord)
            {
                MacroData.BuildUnits[UnitTypes.ZERG_OVERLORD] = true;
            }

            return commands;
        }

        private List<Action> BuildProductionBuildings()
        {
            var commands = new List<Action>();

            foreach (var unit in MacroData.BuildProduction)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.BuildingData[unit.Key];
                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                        return commands;
                    }
                }
            }

            return commands;
        }

        private List<Action> MorphBuildings()
        {
            var commands = new List<Action>();

            foreach (var unit in MacroData.Morph)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.MorphData[unit.Key];
                    var command = Morpher.MorphBuilding(MacroData, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                        return commands;
                    }
                }
            }

            return commands;
        }

        private List<Action> BuildTechBuildings()
        {
            var commands = new List<Action>();

            foreach (var unit in MacroData.BuildTech)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.BuildingData[unit.Key];
                    var command = BuildingBuilder.BuildBuilding(MacroData, unit.Key, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                        return commands;
                    }
                }
            }

            return commands;
        }

        private List<Action> BuildAddOns()
        {
            var commands = new List<Action>();

            foreach (var unit in MacroData.BuildAddOns)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.AddOnData[unit.Key];
                    var command = BuildingBuilder.BuildAddOn(MacroData, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                        continue;
                    }
                }
            }

            return commands;
        }
    }
}
