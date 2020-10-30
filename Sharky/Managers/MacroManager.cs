using SC2APIProtocol;
using Sharky.Builds;
using System.Collections.Generic;

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
        public int Minerals { get; private set; }
        public int VespeneGas { get; private set; }

        MacroSetup MacroSetup;
        UnitManager UnitManager;
        UnitDataManager UnitDataManager;
        BuildingBuilder BuildingBuilder;

        public MacroManager(MacroSetup macroSetup, UnitManager unitManager, UnitDataManager unitDataManager, BuildingBuilder buildingBuilder)
        {
            MacroSetup = macroSetup;
            UnitManager = unitManager;
            UnitDataManager = unitDataManager;
            BuildingBuilder = buildingBuilder;
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
            Minerals = (int)observation.Observation.PlayerCommon.Minerals;
            VespeneGas = (int)observation.Observation.PlayerCommon.Vespene;

            // TODO: change pylonsinmineralline etc. to only build when you need a pylon anyways, unless toggle is off for it
            //CannonsInMineralLine();
            //CannonsAtProxy();
            //CannonAtEveryBase();
            //ShieldsAtExpansions();

            commands.AddRange(BuildSupply());

            //BuildGas();
            commands.AddRange(BuildProductionBuildings());

            commands.AddRange(BuildTechBuildings());
            //BuildUnits();

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
