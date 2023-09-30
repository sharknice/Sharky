namespace Sharky.Builds
{
    public class BuildingBuilder : IBuildingBuilder
    {
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;
        IBuildingPlacement BuildingPlacement;
        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        MicroTaskData MicroTaskData;
        WorkerBuilderService WorkerBuilderService;

        BuildingService BuildingService;
        MapDataService MapDataService;
        AreaService AreaService;
        CameraManager CameraManager;

        Point2D HadRoomLastPosition;

        public BuildingBuilder(ActiveUnitData activeUnitData, TargetingData targetingData, IBuildingPlacement buildingPlacement, SharkyUnitData sharkyUnitData, BaseData baseData, MicroTaskData microTaskData, BuildingService buildingService, MapDataService mapDataService, WorkerBuilderService workerBuilderService, AreaService areaService, CameraManager cameraManager)
        {
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
            BuildingPlacement = buildingPlacement;
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            MicroTaskData = microTaskData;


            BuildingService = buildingService;
            MapDataService = mapDataService;
            WorkerBuilderService = workerBuilderService;
            AreaService = areaService;
            CameraManager = cameraManager;
        }

        public List<SC2Action> BuildBuilding(MacroData macroData, UnitTypes unitType, BuildingTypeData unitData, Point2D generalLocation = null, bool ignoreMineralProximity = false, float maxDistance = 50, List<UnitCommander> workerPool = null, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool allowBlockBase = false)
        {
            if (unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                bool anyBase = false;
                var location = generalLocation;
                if (location == null)
                {
                    anyBase = true;
                    var addOnSwap = macroData.AddOnSwaps.Values.FirstOrDefault(a => a.Started && !a.Completed && a.AddOnBuilder != null && a.AddOnTaker == null && a.DesiredAddOnTaker == unitType);
                    if (addOnSwap != null)
                    {
                        location = new Point2D { X = addOnSwap.AddOnBuilder.UnitCalculation.Position.X, Y = addOnSwap.AddOnBuilder.UnitCalculation.Position.Y };
                    }
                    else
                    {
                        location = GetReferenceLocation(TargetingData.SelfMainBasePoint);
                    }
                }
                var placementLocation = BuildingPlacement.FindPlacement(location, unitType, unitData.Size, ignoreMineralProximity, maxDistance, requireSameHeight, wallOffType, allowBlockBase: allowBlockBase);
                
                if (placementLocation == null)
                {
                    placementLocation = BuildingPlacement.FindPlacement(location, unitType, unitData.Size, true, maxDistance, requireSameHeight, wallOffType, allowBlockBase: allowBlockBase);
                }
                if (placementLocation == null && anyBase)
                {
                    foreach (var selfBase in BaseData.SelfBases)
                    {
                        placementLocation = BuildingPlacement.FindPlacement(selfBase.Location, unitType, unitData.Size, true, maxDistance, requireSameHeight, wallOffType, allowBlockBase: allowBlockBase);
                        if (placementLocation != null)
                        {
                            break;
                        }
                    }
                }
                if (placementLocation != null)
                {
                    var worker = WorkerBuilderService.GetWorker(placementLocation, workerPool);
                    if (worker != null)
                    {
                        if (workerPool == null)
                        {
                            worker.UnitRole = UnitRole.Build;
                        }

                        CameraManager.SetCamera(placementLocation);

                        if (CouldGetStuckBuilding(worker, placementLocation, ignoreMineralProximity, allowBlockBase))
                        {
                            var probeSpot = GetLocationToAvoidGettingStuckBuilding(placementLocation);
                            if (probeSpot != null)
                            {
                                var safeAction = worker.Order(macroData.Frame, Abilities.MOVE, probeSpot, allowConflict: true);
                                safeAction.AddRange(worker.Order(macroData.Frame, unitData.Ability, placementLocation, queue: true));
                                return safeAction;
                            }
                        }

                        return worker.Order(macroData.Frame, unitData.Ability, placementLocation, allowConflict: true);
                    }
                }
             }

            return null;
        }

        bool TriedThisAddonLocation(UnitCommander commander)
        {
            if (HadRoomLastPosition == null)
            {
                return false;
            }
            if (commander.UnitCalculation.Position.X == HadRoomLastPosition.X &&  commander.UnitCalculation.Position.Y == HadRoomLastPosition.Y)
            {
                return true;
            }
            return false;
        }

        public List<SC2Action> BuildAddOn(MacroData macroData, TrainingTypeData unitData, Point2D location = null, float maxDistance = 50)
        {
            if (unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var building = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && !c.Value.UnitCalculation.Unit.HasAddOnTag);
                if (building.Any())
                {
                    if (location != null)
                    {
                        building = building.Where(b => Vector2.DistanceSquared(new Vector2(location.X, location.Y), b.Value.UnitCalculation.Position) <= maxDistance * maxDistance);
                    }
                    if (building.Any())
                    {
                        var addOnSwap = macroData.AddOnSwaps.Values.FirstOrDefault(a => a.Started && !a.Completed && (a.AddOnBuilder == null || a.AddOnBuilder.UnitCalculation.Unit.Tag == building.FirstOrDefault().Value.UnitCalculation.Unit.Tag) && a.AddOnTaker != null && (uint)a.DesiredAddOnBuilder == building.FirstOrDefault().Value.UnitCalculation.Unit.UnitType);

                        // is there room to build the addon?
                        var buildingWithRoom = building.FirstOrDefault(b => HasRoomForAddon(b.Value)).Value;
                        if (buildingWithRoom != null && !TriedThisAddonLocation(buildingWithRoom))
                        {
                            if (addOnSwap != null && Vector2.DistanceSquared(addOnSwap.AddOnTaker.UnitCalculation.Position, buildingWithRoom.UnitCalculation.Position) > 36)
                            {
                                // get closer to target building
                                var action = buildingWithRoom.Order(macroData.Frame, Abilities.LIFT);
                                if (action != null) { return action; }
                            }
                            else
                            {
                                HadRoomLastPosition = buildingWithRoom.UnitCalculation.Position.ToPoint2D();
                                var action = buildingWithRoom.Order(macroData.Frame, unitData.Ability);
                                if (action != null) { return action; }
                            }
                        }
                        else
                        {
                            var buildingToLift = building.FirstOrDefault(b => b.Value.UnitCalculation.NearbyEnemies.Count(e => Vector2.DistanceSquared(e.Position, b.Value.UnitCalculation.Position) < 25) == 0).Value;
                            if (buildingToLift != null)
                            {
                                var action = buildingToLift.Order(macroData.Frame, Abilities.LIFT);
                                if (action != null) { return action; }
                            }
                        }
                    }
                }
                else
                {
                    var flyingType = UnitTypes.TERRAN_BARRACKSFLYING;
                    if (unitData.ProducingUnits.Contains(UnitTypes.TERRAN_FACTORY)) { flyingType = UnitTypes.TERRAN_FACTORYFLYING; }
                    else if (unitData.ProducingUnits.Contains(UnitTypes.TERRAN_STARPORT)) { flyingType = UnitTypes.TERRAN_STARPORTFLYING; }
                    building = ActiveUnitData.Commanders.Where(c => flyingType == (UnitTypes)c.Value.UnitCalculation.Unit.UnitType);

                    if (location != null)
                    {
                        building = building.Where(b => Vector2.DistanceSquared(new Vector2(location.X, location.Y), b.Value.UnitCalculation.Position) <= maxDistance * maxDistance);
                    }
                    if (building.Any())
                    {
                        var addOnSwap = macroData.AddOnSwaps.Values.FirstOrDefault(a => a.Started && !a.Completed && (a.AddOnBuilder == null || a.AddOnBuilder.UnitCalculation.Unit.Tag == building.FirstOrDefault().Value.UnitCalculation.Unit.Tag) && a.AddOnTaker != null && (uint)a.DesiredAddOnBuilder == building.FirstOrDefault().Value.UnitCalculation.Unit.UnitType);
                        if (addOnSwap != null)
                        {
                            location = new Point2D { X = addOnSwap.AddOnTaker.UnitCalculation.Position.X, Y = addOnSwap.AddOnTaker.UnitCalculation.Position.Y };
                        }
                        if (location == null)
                        {
                            location = new Point2D { X = building.First().Value.UnitCalculation.Position.X, Y = building.First().Value.UnitCalculation.Position.Y };
                        }
                        if (building.First().Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.LAND_BARRACKS || o.AbilityId == (uint)Abilities.LAND_FACTORY || o.AbilityId == (uint)Abilities.LAND_STARPORT))
                        {
                            return null;
                        }
                        var placementLocation = BuildingPlacement.FindPlacement(location, UnitTypes.TERRAN_BARRACKSTECHLAB, 1, true, maxDistance, false, WallOffType.Terran);
                        if (placementLocation != null)
                        {
                            var action = building.First().Value.Order(macroData.Frame, unitData.Ability, placementLocation);
                            if (action != null) { return action; }
                        }
                    }
                }
            }

            return null;
        }

        bool HasRoomForAddon(UnitCommander building)
        {
            return HasRoomForAddon(building.UnitCalculation.Unit);
        }

        public bool HasRoomForAddon(Unit building)
        {
            var addonY = building.Pos.Y - .5f;
            var addonX = building.Pos.X + 2.5f;
            if (addonX >= 0 && addonY >= 0 && addonX < MapDataService.MapData.MapWidth && addonY < MapDataService.MapData.MapHeight &&
                BuildingService.AreaBuildable(addonX, addonY, .5f) && !BuildingService.Blocked(addonX, addonY, .5f, -.5f) && !BuildingService.HasAnyCreep(addonX, addonY, .5f))
            {
                return true;
            }
            return false;
        }

        public List<SC2Action> BuildGas(MacroData macroData, BuildingTypeData unitData, Unit geyser)
        {
            if (unitData.Minerals <= macroData.Minerals && unitData.Gas <= macroData.VespeneGas)
            {
                var worker = WorkerBuilderService.GetWorker(new Point2D { X = geyser.Pos.X, Y = geyser.Pos.Y });
                if (worker != null)
                {
                    worker.UnitRole = UnitRole.Build;
                    return worker.Order(macroData.Frame, unitData.Ability, null, geyser.Tag, allowConflict: true);
                }
            }

            return null;
        }

        public Point2D GetReferenceLocation(Point2D buildLocation)
        {
            var nexus = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.ResourceCenter)).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, new Vector2(buildLocation.X, buildLocation.Y))).FirstOrDefault();
            if (nexus != null)
            {
                return new Point2D { X = nexus.UnitCalculation.Unit.Pos.X, Y = nexus.UnitCalculation.Unit.Pos.Y };
            }
            else
            {
                var worker = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker)).OrderBy(c => c.UnitCalculation.NearbyEnemies.Count()).ThenBy(c => c.UnitCalculation.NearbyAllies.Where(ally => ally.UnitClassifications.Contains(UnitClassification.ArmyUnit)).Count()).FirstOrDefault();
                if (worker != null)
                {
                    return new Point2D { X = worker.UnitCalculation.Unit.Pos.X, Y = worker.UnitCalculation.Unit.Pos.Y };
                }
            }

            return buildLocation;
        }

        protected bool CouldGetStuckBuilding(UnitCommander commander, Point2D spot, bool ignoreMineralProximity, bool allowBlockBase)
        {
            if ((ignoreMineralProximity || allowBlockBase) && commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PROBE)
            {
                if (GetValidPoint(commander.UnitCalculation.Position.X, commander.UnitCalculation.Position.Y, -1, 3) == null || GetValidPoint(spot.X, spot.Y, -1, 3) == null)
                {
                    return true;
                }
            }
            return false;
        }

        Point2D GetValidPoint(float x, float y, int baseHeight, float size = 2f)
        {
            if (x >= 0 && y >= 0 && x < MapDataService.MapData.MapWidth && y < MapDataService.MapData.MapHeight && (baseHeight == -1 || MapDataService.MapHeight((int)x, (int)y) == baseHeight))
            {
                if (BuildingService.AreaBuildable(x, y, size / 2.0f))
                {
                    if (!BuildingService.Blocked(x, y, 1, 0f))
                    {
                        if (!BuildingService.HasAnyCreep(x, y, size / 2.0f))
                        {
                            return new Point2D { X = x, Y = y };
                        }
                    }
                }
            }

            return null;
        }

        Point2D GetLocationToAvoidGettingStuckBuilding(Point2D buildSpot)
        {
            var spots = AreaService.GetTargetArea(buildSpot, 4).Where(p => GetValidPoint(p.X, p.Y, -1, 3) != null);
            var gatewaySpots = spots.Where(s => Vector2.Distance(s.ToVector2(), buildSpot.ToVector2()) > 3).OrderBy(s => Vector2.Distance(s.ToVector2(), buildSpot.ToVector2()));
            return gatewaySpots.FirstOrDefault();
        }
    }
}
