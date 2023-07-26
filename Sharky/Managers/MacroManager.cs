namespace Sharky.Managers
{
    public class MacroManager : SharkyManager
    {
        MacroSetup MacroSetup;
        MacroData MacroData;

        BuildPylonService BuildPylonService;
        BuildDefenseService BuildDefenseService;
        BuildProxyService BuildProxyService;
        BuildAddOnSwapService BuildAddOnSwapService;
        BuildingCancelService BuildingCancelService;
        BuildingRequestCancellingService BuildingRequestCancellingService;
        UpgradeRequestCancellingService UpgradeRequestCancellingService;
        UnitRequestCancellingService UnitRequestCancellingService;

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
        public int SlowRunFrequency { get; set; }

        public override bool NeverSkip { get => true; }

        public MacroManager(DefaultSharkyBot defaultSharkyBot)
        {
            MacroSetup = defaultSharkyBot.MacroSetup;
            MacroData = defaultSharkyBot.MacroData;

            BuildPylonService = defaultSharkyBot.BuildPylonService;
            BuildDefenseService = defaultSharkyBot.BuildDefenseService;
            BuildProxyService = defaultSharkyBot.BuildProxyService;
            BuildAddOnSwapService = defaultSharkyBot.BuildAddOnSwapService;
            BuildingCancelService = defaultSharkyBot.BuildingCancelService;
            BuildingRequestCancellingService = defaultSharkyBot.BuildingRequestCancellingService;
            UpgradeRequestCancellingService = defaultSharkyBot.UpgradeRequestCancellingService;
            UnitRequestCancellingService = defaultSharkyBot.UnitRequestCancellingService;

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
            SlowRunFrequency = 25;
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

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2Action>();

            MacroData.FoodUsed = (int)observation.Observation.PlayerCommon.FoodUsed;
            MacroData.FoodLeft = (int)observation.Observation.PlayerCommon.FoodCap - MacroData.FoodUsed;
            MacroData.FoodArmy = (int)observation.Observation.PlayerCommon.FoodArmy;
            MacroData.FoodWorkers = (int)observation.Observation.PlayerCommon.FoodWorkers;
            MacroData.Minerals = (int)observation.Observation.PlayerCommon.Minerals;
            MacroData.VespeneGas = (int)observation.Observation.PlayerCommon.Vespene;
            MacroData.Frame = (int)observation.Observation.GameLoop;

            if (!CanRunFullProduction(observation))
            {
                if (MacroData.Race == Race.Zerg)
                {
                    actions.AddRange(EveryFrameProduction());
                }
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

            actions.AddRange(BuildAddOnSwapService.BuildAndSwapAddons());

            if (MacroData.Minerals >= 100)
            {
                actions.AddRange(BuildPylonService.BuildPylonsAtEveryMineralLine());
                actions.AddRange(BuildPylonService.BuildPylonsAtDefensivePoint());
                actions.AddRange(BuildPylonService.BuildPylonsAtEveryBase());
                actions.AddRange(BuildPylonService.BuildPylonsAtNextBase());
            }

            actions.AddRange(SupplyBuilder.BuildSupply());

            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtEveryMineralLine());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtDefensivePoint());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtEveryBase());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildingsAtNextBase());
            actions.AddRange(BuildDefenseService.BuildDefensiveBuildings());

            actions.AddRange(VespeneGasBuilder.BuildVespeneGas());

            actions.AddRange(BuildingMorpher.MorphBuildings());
            actions.AddRange(AddOnBuilder.BuildAddOns());
            actions.AddRange(ProductionBuilder.BuildProductionBuildings());
            actions.AddRange(TechBuilder.BuildTechBuildings());
            actions.AddRange(UnfinishedBuildingCompleter.SendScvToFinishIncompleteBuildings());

            actions.AddRange(EveryFrameProduction());

            return actions;
        }

        private bool CanRunFullProduction(ResponseObservation observation)
        {
            var runFrequency = RunFrequency;
            if (observation.Observation.GameLoop > 6720 && TotalFrameTime / observation.Observation.GameLoop > 1)
            {
                runFrequency = SlowRunFrequency;
            }

            if (LastRunFrame + runFrequency > observation.Observation.GameLoop)
            {
                return false;
            }
            return true;
        }

        IEnumerable<SC2Action> EveryFrameProduction()
        {
            var actions = new List<SC2Action>();

            actions.AddRange(UpgradeResearcher.ResearchUpgrades());
            actions.AddRange(UnitBuilder.ProduceUnits());

            actions.AddRange(BuildingCancelService.CancelBuildings());
            actions.AddRange(BuildingRequestCancellingService.CancelBuildings());
            actions.AddRange(UpgradeRequestCancellingService.CancelUpgrades());
            actions.AddRange(UnitRequestCancellingService.CancelUnits());

            return actions;
        }
    }
}
