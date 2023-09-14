namespace Sharky.Builds
{
    public class SimCityService
    {
        int LastFrame;
        int FrameInterval;

        MacroData MacroData;
        BaseData BaseData;
        TargetingData TargetingData;
        WorkerBuilderService WorkerBuilderService;
        ActiveUnitData ActiveUnitData;
        RequirementService RequirementService;
        MapDataService MapDataService;
        BuildingService BuildingService;
        AreaService AreaService;
        MicroTaskData MicroTaskData;
        SharkyUnitData SharkyUnitData;

        IBuildingPlacement ProtectNexusPylonPlacement;
        IBuildingPlacement ProtectNexusCannonPlacement;
        IBuildingPlacement ProtectNexusBatteryPlacement;
        IBuildingPlacement GatewayCannonPlacement;

        UnitCommander Builder;

        public SimCityService(DefaultSharkyBot defaultSharkyBot)
        {
            MacroData = defaultSharkyBot.MacroData;
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;
            ProtectNexusPylonPlacement = defaultSharkyBot.ProtectNexusPylonPlacement;
            ProtectNexusCannonPlacement = defaultSharkyBot.ProtectNexusCannonPlacement;
            ProtectNexusBatteryPlacement = defaultSharkyBot.ProtectNexusBatteryPlacement;
            WorkerBuilderService = defaultSharkyBot.WorkerBuilderService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            RequirementService = defaultSharkyBot.RequirementService;
            GatewayCannonPlacement = defaultSharkyBot.GatewayCannonPlacement;
            MapDataService = defaultSharkyBot.MapDataService;
            BuildingService = defaultSharkyBot.BuildingService;
            AreaService = defaultSharkyBot.AreaService;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;

            FrameInterval = 20;
            LastFrame = 0;
        }

        public IEnumerable<SC2Action> OnFrame()
        {
            if (MacroData.Frame - LastFrame < FrameInterval && TargetingData.SelfMainBasePoint != null && TargetingData.NaturalBasePoint != null)
            {
                return null;
            }

            LastFrame = MacroData.Frame;

            if (MacroData.Minerals < 100) { return null; }

            if (AlreadyBuilding())
            {
                return null;
            }

            if (MacroData.ProtossMacroData.ExtraBasesGatewayCannonSimCity)
            {
                foreach (var data in GetExtraBases())
                {
                    var pAction = BuildGatewayCannonPylons(data.Location);
                    if (pAction != null) { return pAction; }

                    if (MacroData.Minerals >= 150)
                    {
                        if (RequirementService.HaveCompleted(UnitTypes.PROTOSS_FORGE))
                        {
                            var cAction = BuildGatewayCannonPhotonCannon(data.Location);
                            if (cAction != null) { return cAction; }
                        }

                        var gAction = BuildGatewayCannonGateway(data.Location);
                        if (gAction != null) { return gAction; }
                    }

                    var sAction = BuildGatewayCannonBattery(data.Location);
                    if (sAction != null) { return sAction; }
                } 
            }

            if (MacroData.ProtossMacroData.DesiredExtraBaseSimCityPylons > 0)
            {
                foreach (var data in GetExtraBases())
                {
                    var pylonLocation = ProtectNexusPylonPlacement.FindPlacement(data.Location, UnitTypes.PROTOSS_PYLON, 1);
                    if (pylonLocation != null)
                    {
                        var action = Build(pylonLocation, Abilities.BUILD_PYLON);
                        if (action != null) { return action; }
                    }
                }
            }

            if (MacroData.ProtossMacroData.DesiredExtraBaseSimCityCannons > 0 && MacroData.Minerals >= 150 && RequirementService.HaveCompleted(UnitTypes.PROTOSS_FORGE))
            {
                foreach (var data in GetExtraBases())
                {
                    var cannonLocation = ProtectNexusCannonPlacement.FindPlacement(data.Location, UnitTypes.PROTOSS_PHOTONCANNON, 1);
                    if (cannonLocation != null)
                    {
                        var action = Build(cannonLocation, Abilities.BUILD_PHOTONCANNON);
                        if (action != null) { return action; }
                    }
                }
            }

            if (MacroData.ProtossMacroData.DesiredExtraBaseSimCityBatteries > 0 && RequirementService.HaveCompleted(UnitTypes.PROTOSS_CYBERNETICSCORE))
            {
                foreach (var data in GetExtraBases())
                {
                    var cannonLocation = ProtectNexusBatteryPlacement.FindPlacement(data.Location, UnitTypes.PROTOSS_SHIELDBATTERY, 1);
                    if (cannonLocation != null)
                    {
                        var action = Build(cannonLocation, Abilities.BUILD_SHIELDBATTERY);
                        if (action != null) { return action; }
                    }
                }
            }

            return null;
        }

        bool AlreadyBuilding()
        {
            if (Builder != null)
            {
                if (Builder.UnitRole == UnitRole.Build)
                {
                    if (Builder.UnitCalculation.Unit.Orders.Any(o => SharkyUnitData.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId)))
                    {
                        return true;
                    }
                }
                Builder = null;
            }
            return false;
        }

        IEnumerable<SC2Action> BuildGatewayCannonPylons(Point2D location)
        {
            var buildingLocation = GatewayCannonPlacement.FindPlacement(location, UnitTypes.PROTOSS_PYLON, 1);
            if (buildingLocation != null)
            {
                var action = Build(buildingLocation, Abilities.BUILD_PYLON);
                if (action != null) { return action; }
            }
            return null;
        }

        IEnumerable<SC2Action> BuildGatewayCannonPhotonCannon(Point2D location)
        {
            var buildingLocation = GatewayCannonPlacement.FindPlacement(location, UnitTypes.PROTOSS_PHOTONCANNON, 1);
            if (buildingLocation != null)
            {
                var action = Build(buildingLocation, Abilities.BUILD_PHOTONCANNON);
                if (action != null) { return action; }
            }
            return null;
        }

        IEnumerable<SC2Action> BuildGatewayCannonGateway(Point2D location)
        {
            var buildingLocation = GatewayCannonPlacement.FindPlacement(location, UnitTypes.PROTOSS_GATEWAY, 1);
            if (buildingLocation != null)
            {
                var action = Build(buildingLocation, Abilities.BUILD_GATEWAY);
                if (action != null) { return action; }
            }
            return null;
        }

        IEnumerable<SC2Action> BuildGatewayCannonBattery(Point2D location)
        {
            var buildingLocation = GatewayCannonPlacement.FindPlacement(location, UnitTypes.PROTOSS_SHIELDBATTERY, 1);
            if (buildingLocation != null)
            {
                var action = Build(buildingLocation, Abilities.BUILD_SHIELDBATTERY);
                if (action != null) { return action; }
            }
            return null;
        }

        private IEnumerable<BaseLocation> GetExtraBases()
        {
            return BaseData.SelfBases.Where(b => !(b.Location.X == TargetingData.SelfMainBasePoint.X && b.Location.Y == TargetingData.SelfMainBasePoint.Y) && !(b.Location.X == TargetingData.NaturalBasePoint.X && b.Location.Y == TargetingData.NaturalBasePoint.Y));
        }

        IEnumerable<SC2Action> Build(Point2D location, Abilities ability)
        {
            if (ActiveUnitData.Commanders.Values.Any(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE && c.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)ability && o.TargetWorldSpacePos.X == location.X && o.TargetWorldSpacePos.Y == location.Y)))
            {
                return null;
            }

            var worker = WorkerBuilderService.GetWorker(location);
            if (worker != null)
            {
                worker.UnitRole = UnitRole.Build;
                Builder = worker;

                if (CouldGetStuckBuilding(worker, location))
                {
                    var probeSpot = GetLocationToAvoidGettingStuckBuilding(location);
                    if (probeSpot != null)
                    {
                        var safeAction = worker.Order(MacroData.Frame, Abilities.MOVE, probeSpot, allowConflict: true);
                        safeAction.AddRange(worker.Order(MacroData.Frame, ability, location, queue: true));
                        return safeAction;
                    }
                }

                return worker.Order(MacroData.Frame, ability, location, allowConflict: true);
            }
            return null;
        }


        bool CouldGetStuckBuilding(UnitCommander commander, Point2D spot)
        {
            if (GetSafePoint(commander.UnitCalculation.Position.X, commander.UnitCalculation.Position.Y, -1, 3) == null || GetSafePoint(spot.X, spot.Y, -1, 3) == null)
            {
                return true;
            }
            
            return false;
        }

        Point2D GetSafePoint(float x, float y, int baseHeight, float size = 2f)
        {
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight && (baseHeight == -1 || MapDataService.MapHeight((int)x, (int)y) == baseHeight))
            {
                if (BuildingService.AreaBuildable(x, y, size / 2.0f))
                {
                    if (!BuildingService.Blocked(x, y, 1, 0f))
                    {
                        return new Point2D { X = x, Y = y };
                    }
                }
            }

            return null;
        }

        Point2D GetLocationToAvoidGettingStuckBuilding(Point2D buildSpot)
        {
            var spots = AreaService.GetTargetArea(buildSpot, 4).Where(p => GetSafePoint(p.X, p.Y, -1, 3) != null);
            var gatewaySpots = spots.Where(s => Vector2.Distance(s.ToVector2(), buildSpot.ToVector2()) > 3).OrderBy(s => Vector2.Distance(s.ToVector2(), buildSpot.ToVector2()));
            return gatewaySpots.FirstOrDefault();
        }
    }
}
