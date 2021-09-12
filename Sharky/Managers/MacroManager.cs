using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.MacroServices;
using Sharky.DefaultBot;
using Sharky.Macro;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class MacroManager : SharkyManager
    {
        MacroSetup MacroSetup;
        MacroData MacroData;

        BuildPylonService BuildPylonService;
        BuildDefenseService BuildDefenseService;
        BuildProxyService BuildProxyService;
        BuildingCancelService BuildingCancelService;

        VespeneGasBuilder VespeneGasBuilder;
        UnitBuilder UnitBuilder;
        UpgradeResearcher UpgradeResearcher;
        SupplyBuilder SupplyBuilder;
        ProductionBuilder ProductionBuilder;
        TechBuilder TechBuilder;
        AddOnBuilder AddOnBuilder;
        BuildingMorpher BuildingMorpher;
        UnfinishedBuildingCompleter UnfinishedBuildingCompleter;

        int LastRunFrame;

        public int RunFrequency { get; set; }

        public override bool NeverSkip { get => true; }

        public MacroManager(DefaultSharkyBot defaultSharkyBot)
        {
            MacroSetup = defaultSharkyBot.MacroSetup;
            MacroData = defaultSharkyBot.MacroData;

            BuildPylonService = defaultSharkyBot.BuildPylonService;
            BuildDefenseService = defaultSharkyBot.BuildDefenseService;
            BuildProxyService = defaultSharkyBot.BuildProxyService;
            BuildingCancelService = defaultSharkyBot.BuildingCancelService;

            VespeneGasBuilder = defaultSharkyBot.VespeneGasBuilder;
            UnitBuilder = defaultSharkyBot.UnitBuilder;
            UpgradeResearcher = defaultSharkyBot.UpgradeResearcher;
            SupplyBuilder = defaultSharkyBot.SupplyBuilder;
            ProductionBuilder = defaultSharkyBot.ProductionBuilder;
            TechBuilder = defaultSharkyBot.TechBuilder;
            AddOnBuilder = defaultSharkyBot.AddOnBuilder;
            BuildingMorpher = defaultSharkyBot.BuildingMorpher;
            UnfinishedBuildingCompleter = defaultSharkyBot.UnfinishedBuildingCompleter;

            MacroData.DesiredUpgrades = new Dictionary<Upgrades, bool>();

            LastRunFrame = -10;
            RunFrequency = 5;
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

            if (LastRunFrame + RunFrequency > observation.Observation.GameLoop)
            {
                return actions;
            }
            LastRunFrame = (int)observation.Observation.GameLoop;

            actions.AddRange(BuildProxyService.BuildPylons());
            actions.AddRange(BuildProxyService.MorphBuildings());
            actions.AddRange(BuildProxyService.BuildAddOns());
            actions.AddRange(BuildProxyService.BuildDefensiveBuildings());
            actions.AddRange(BuildProxyService.BuildProductionBuildings());    
            actions.AddRange(BuildProxyService.BuildTechBuildings());
            // TODO: send new SCVs to any incomplete proxy building without one

            actions.AddRange(BuildPylonService.BuildPylonsAtEveryMineralLine());
            actions.AddRange(BuildPylonService.BuildPylonsAtDefensivePoint());
            actions.AddRange(BuildPylonService.BuildPylonsAtEveryBase());
            actions.AddRange(BuildPylonService.BuildPylonsAtNextBase());
            actions.AddRange(SupplyBuilder.BuildSupply());

            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtEveryMineralLine());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtDefensivePoint());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtEveryBase());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildings());

            actions.AddRange(VespeneGasBuilder.BuildVespeneGas());

            actions.AddRange(BuildingMorpher.MorphBuildings());
            actions.AddRange(AddOnBuilder.BuildAddOns());
            actions.AddRange(ProductionBuilder.BuildProductionBuildings());
            actions.AddRange(TechBuilder.BuildTechBuildings());
            actions.AddRange(UnfinishedBuildingCompleter.SendScvToFinishIncompleteBuildings());

            actions.AddRange(UpgradeResearcher.ResearchUpgrades());
            actions.AddRange(UnitBuilder.ProduceUnits());

            actions.AddRange(BuildingCancelService.CancelBuildings());

            return actions;
        }
    }
}
