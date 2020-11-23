using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
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

        public MacroManager(MacroSetup macroSetup, IUnitManager unitManager, UnitDataManager unitDataManager, BuildingBuilder buildingBuilder, SharkyOptions sharkyOptions, IBaseManager baseManager, TargetingManager targetingManager, AttackData attackData, IBuildingPlacement warpInPlacement, MacroData macroData)
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

            // TODO: change pylonsinmineralline etc. to only build when you need a pylon anyways, unless toggle is off for it
            //CannonsInMineralLine();
            //CannonsAtProxy();
            //CannonAtEveryBase();
            //ShieldsAtExpansions();

            actions.AddRange(BuildSupply());

            actions.AddRange(BuildVespeneGas());
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
                        var building = UnitManager.Commanders.Where(c => upgradeData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1);
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
                    if (unitData.Food <= MacroData.FoodLeft && unitData.Minerals <= MacroData.Minerals && unitData.Gas <= MacroData.VespeneGas)
                    {
                        var building = UnitManager.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.WarpInOffCooldown(MacroData.Frame, SharkyOptions.FramesPerSecond, UnitDataManager));
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
                var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_ASSIMILATOR];
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
    }
}
