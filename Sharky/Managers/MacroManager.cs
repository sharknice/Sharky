using SC2APIProtocol;
using Sharky.Builds;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class MacroManager : SharkyManager
    {
        public List<UnitTypes> Units;
        public Dictionary<UnitTypes, int> DesiredUnitCounts;
        public Dictionary<UnitTypes, bool> BuildUnits;

        public List<UnitTypes> Production;
        public Dictionary<UnitTypes, int> DesiredProductionCounts;

        public Dictionary<UnitTypes, bool> BuildProduction;

        public List<UnitTypes> Tech;
        public Dictionary<UnitTypes, int> DesiredTechCounts;
        public Dictionary<UnitTypes, bool> BuildTech;

        public List<UnitTypes> DefensiveBuildings;
        public Dictionary<UnitTypes, int> DesiredDefensiveBuildingsCounts;
        public Dictionary<UnitTypes, bool> BuildDefensiveBuildings;

        public Dictionary<Upgrades, bool> DesiredUpgrades;
        public int DesiredGases;
        public bool BuildGas;

        public List<UnitTypes> NexusUnits;
        public List<UnitTypes> GatewayUnits;
        public List<UnitTypes> RoboticsFacilityUnits;
        public List<UnitTypes> StargateUnits;

        public List<UnitTypes> BarracksUnits;
        public List<UnitTypes> FactoryUnits;
        public List<UnitTypes> StarportUnits;

        public int DesiredPylons;
        public bool BuildPylon;

        public Race Race;

        public int FoodUsed { get; private set; }
        public int FoodLeft { get; private set; }
        public int Minerals { get; private set; }
        public int VespeneGas { get; private set; }
        public int Frame { get; private set; }

        MacroSetup MacroSetup;
        IUnitManager UnitManager;
        UnitDataManager UnitDataManager;
        BuildingBuilder BuildingBuilder;
        SharkyOptions SharkyOptions;
        BaseManager BaseManager;
        TargetingManager TargetingManager;

        public MacroManager(MacroSetup macroSetup, IUnitManager unitManager, UnitDataManager unitDataManager, BuildingBuilder buildingBuilder, SharkyOptions sharkyOptions, BaseManager baseManager, TargetingManager targetingManager)
        {
            MacroSetup = macroSetup;
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
            BuildingBuilder = buildingBuilder;
            SharkyOptions = sharkyOptions;
            BaseManager = baseManager;
            TargetingManager = targetingManager;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (var playerInfo in gameInfo.PlayerInfo)
            {
                if (playerInfo.PlayerId == playerId)
                {
                    Race = playerInfo.RaceActual;
                }
            }

            MacroSetup.SetupMacro(this, Race);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var commands = new List<ActionRawUnitCommand>();

            FoodUsed = (int)observation.Observation.PlayerCommon.FoodUsed;
            FoodLeft = (int)observation.Observation.PlayerCommon.FoodCap - FoodUsed;
            Minerals = (int)observation.Observation.PlayerCommon.Minerals;
            VespeneGas = (int)observation.Observation.PlayerCommon.Vespene;
            Frame = (int)observation.Observation.GameLoop;

            // TODO: change pylonsinmineralline etc. to only build when you need a pylon anyways, unless toggle is off for it
            //CannonsInMineralLine();
            //CannonsAtProxy();
            //CannonAtEveryBase();
            //ShieldsAtExpansions();

            commands.AddRange(BuildSupply());

            commands.AddRange(BuildVespeneGas());
            commands.AddRange(BuildProductionBuildings());

            commands.AddRange(BuildTechBuildings());
            commands.AddRange(ProduceUnits());

            var actions = new List<Action>();
            foreach (var command in commands)
            {
                var action = new Action
                {
                    ActionRaw = new ActionRaw
                    {
                        UnitCommand = command
                    }
                };
                actions.Add(action);
            }

            return actions;
        }

        private List<ActionRawUnitCommand> ProduceUnits()
        {
            var commands = new List<ActionRawUnitCommand>();
            foreach (var unit in BuildUnits)
            {
                if (unit.Value && unit.Key != UnitTypes.PROTOSS_ARCHON)
                {
                    var unitData = UnitDataManager.TrainingData[unit.Key];
                    if (unitData.Food <= FoodLeft && unitData.Minerals <= Minerals && unitData.Gas <= VespeneGas)
                    {
                        var building = UnitManager.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.WarpInOffCooldown(Frame, SharkyOptions.FramesPerSecond, UnitDataManager));
                        if (building.Count() > 0)
                        {
                            if (building.First().Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE)
                            {
                                //if (attacking)
                                //{
                                //    location = WarpInPlacer.FindPlacement(attackinglocation);
                                //}
                                //else
                                //{
                                //    location = WarpInPlacer.FindPlacement(defenselocation);
                                //}

                                //building.First().Value.Order((int)unitData.WarpInAbility, location);
                            }
                            else
                            {
                                commands.Add(building.First().Value.Order(Frame, unitData.Ability));
                            }
                        }
                    }
                }
                else if (unit.Value && unit.Key == UnitTypes.PROTOSS_ARCHON)
                {
                    var templar = UnitManager.Commanders.Where(c => c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DARKTEMPLAR);
                    var merges = templar.Count(a => a.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON));
                    if (merges + UnitManager.Count(UnitTypes.PROTOSS_ARCHON) < DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON])
                    {
                        var mergables = templar.Where(c => !c.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON || o.AbilityId == (uint)Abilities.MORPH_ARCHON + 1));
                        if (mergables.Count() >= 2)
                        {
                            var commanders = mergables.OrderBy(c => c.Value.UnitCalculation.Unit.Energy).Take(2);
                            commanders.First().Value.Merge(commanders.Last().Value.UnitCalculation.Unit.Tag);
                        }
                    }
                }
            }

            return commands;
        }

        private List<ActionRawUnitCommand> BuildVespeneGas()
        {
            var commands = new List<ActionRawUnitCommand>();
            if (BuildGas && Minerals >= 75)
            {
                var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_ASSIMILATOR];
                var takenGases = UnitManager.SelfUnits.Where(u => UnitDataManager.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                var openGeysers = BaseManager.BaseLocations.SelectMany(b => b.VespeneGeysers).Where(g => g.VespeneContents > 0 && !takenGases.Any(t => t.Value.Unit.Pos.X == g.Pos.X && t.Value.Unit.Pos.Y == g.Pos.Y));
                if (openGeysers.Count() > 0)
                {
                    var baseLocation = BuildingBuilder.GetReferenceLocation(TargetingManager.DefensePoint);
                    var closestGyeser = openGeysers.OrderBy(o => Vector2.DistanceSquared(new Vector2(baseLocation.X, baseLocation.Y), new Vector2(o.Pos.X, o.Pos.Y))).FirstOrDefault();
                    if (closestGyeser != null)
                    {
                        var command = BuildingBuilder.BuildGas(this, unitData, closestGyeser);
                        if (command != null)
                        {
                            commands.Add(command);
                        }
                    }
                    
                }
            }

            return commands;
        }

        private List<ActionRawUnitCommand> BuildSupply()
        {
            var commands = new List<ActionRawUnitCommand>();

            if (BuildPylon)
            {
                var unitData = UnitDataManager.BuildingData[UnitTypes.PROTOSS_PYLON];
                var command = BuildingBuilder.BuildBuilding(this, UnitTypes.PROTOSS_PYLON, unitData);
                if (command != null)
                {
                    commands.Add(command);
                }
            }

            return commands;
        }

        private List<ActionRawUnitCommand> BuildProductionBuildings()
        {
            var commands = new List<ActionRawUnitCommand>();

            foreach (var unit in BuildProduction)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.BuildingData[unit.Key];
                    var command = BuildingBuilder.BuildBuilding(this, unit.Key, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                    }
                }
            }

            return commands;
        }

        private List<ActionRawUnitCommand> BuildTechBuildings()
        {
            var commands = new List<ActionRawUnitCommand>();

            foreach (var unit in BuildTech)
            {
                if (unit.Value)
                {
                    var unitData = UnitDataManager.BuildingData[unit.Key];
                    var command = BuildingBuilder.BuildBuilding(this, unit.Key, unitData);
                    if (command != null)
                    {
                        commands.Add(command);
                    }
                }
            }

            return commands;
        }
    }
}
