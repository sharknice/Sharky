namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossBuildingPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        BaseData BaseData;
        DebugService DebugService;
        MapDataService MapDataService;
        BuildingService BuildingService;
        IBuildingPlacement WallOffPlacement;
        ProtossPylonGridPlacement ProtossPylonGridPlacement;
        ProtossProductionGridPlacement ProtossProductionGridPlacement;
        IBuildingPlacement ProtectNexusPylonPlacement;
        TargetingData TargetingData;
        IBuildingPlacement ProtectNexusCannonPlacement;
        BuildOptions BuildOptions;
        IBuildingPlacement ProtossDefensiveGridPlacement;
        IBuildingPlacement ProtossProxyGridPlacement;

        List<Point2D> LastLocations;

        public ProtossBuildingPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, BaseData baseData, DebugService debugService, MapDataService mapDataService, BuildingService buildingService, IBuildingPlacement wallOffPlacement, ProtossPylonGridPlacement protossPylonGridPlacement, ProtossProductionGridPlacement protossProductionGridPlacement, IBuildingPlacement protectNexusPylonPlacement, TargetingData targetingData, IBuildingPlacement protectNexusCannonPlacement, BuildOptions buildOptions, IBuildingPlacement protossDefensiveGridPlacement, IBuildingPlacement protossProxyGridPlacement)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
            DebugService = debugService;
            MapDataService = mapDataService;
            BuildingService = buildingService;
            WallOffPlacement = wallOffPlacement;
            ProtossPylonGridPlacement = protossPylonGridPlacement;
            ProtossProductionGridPlacement = protossProductionGridPlacement;
            ProtectNexusPylonPlacement = protectNexusPylonPlacement;
            TargetingData = targetingData;
            ProtectNexusCannonPlacement = protectNexusCannonPlacement;
            BuildOptions = buildOptions;
            ProtossDefensiveGridPlacement = protossDefensiveGridPlacement;
            ProtossProxyGridPlacement = protossProxyGridPlacement;

            LastLocations = new List<Point2D>();
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = false)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            if (wallOffType == WallOffType.Full)
            {
                if (!BuildingService.FullyWalled())
                {
                    var point = WallOffPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType, requireVision, allowBlockBase);
                    if (point != null)
                    {
                        return point;
                    }
                }
            }
            else if (wallOffType == WallOffType.Partial)
            {
                if (!BuildingService.PartiallyWalled())
                {
                    var point = WallOffPlacement.FindPlacement(target, unitType, size, ignoreResourceProximity, maxDistance, requireSameHeight, wallOffType, requireVision, allowBlockBase);
                    if (point != null)
                    {
                        return point;
                    }
                }
            }

            if (unitType == UnitTypes.PROTOSS_PYLON)
            {
                return FindPylonPlacement(target, maxDistance, mineralProximity, requireSameHeight, wallOffType, requireVision, allowBlockBase);
            }
            else
            {
                return FindProductionPlacement(target, size, maxDistance, mineralProximity, wallOffType, requireVision, allowBlockBase, requireSameHeight);
            }
        }

        public Point2D FindPylonPlacement(Point2D reference, float maxDistance, float minimumMineralProximinity = 2, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = false)
        {
            if (!allowBlockBase)
            {
                var spot = ProtossPylonGridPlacement.FindPlacement(reference, maxDistance, minimumMineralProximinity);
                if (spot != null) { return spot; }
            }

            var selfBase = BaseData.BaseLocations.FirstOrDefault(b => (b.Location.X == reference.X && b.Location.Y == reference.Y) && !(b.Location.X == TargetingData.SelfMainBasePoint.X && b.Location.Y == TargetingData.SelfMainBasePoint.Y) && !(b.Location.X == TargetingData.NaturalBasePoint.X && b.Location.Y == TargetingData.NaturalBasePoint.Y));
            if (selfBase != null)
            {
                var pylonLocation = ProtectNexusPylonPlacement.FindPlacement(reference, UnitTypes.PROTOSS_PYLON, 1);
                if (pylonLocation != null)
                {
                    return pylonLocation;
                }
            }

            if (PointOk(reference, minimumMineralProximinity, reference, requireSameHeight, allowBlockBase, maxDistance))
            {
                return reference;
            }

            var x = reference.X;
            var y = reference.Y;
            var radius = 1f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < maxDistance / 2.0)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / (8.0 + radius);
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };

                    if (PointOk(point, minimumMineralProximinity, reference, requireSameHeight, allowBlockBase, maxDistance))
                    {
                        return point;
                    }

                    angle += sliceSize;
                }
                radius += 1;
            }

            return null;
        }

        protected bool PointOk(Point2D point, float minimumMineralProximinity, Point2D reference, bool requireSameHeight, bool allowBlockBase, float maxDistance)
        {
            if (BuildingService.AreaBuildable(point.X, point.Y, 1.25f) &&
                        (minimumMineralProximinity == 0 || !BuildingService.BlocksResourceCenter(point.X, point.Y, 1.25f)) &&
                        !BuildingService.Blocked(point.X, point.Y, 1.25f, .1f) && !BuildingService.HasAnyCreep(point.X, point.Y, 1.5f) &&
                        (!requireSameHeight || MapDataService.MapHeight(point) == MapDataService.MapHeight(reference)) &&
                        (minimumMineralProximinity == 0 || !BuildingService.BlocksPath(point.X, point.Y, 1.25f)))
            {
                var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) || SharkyUnitData.GasGeyserTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                var squared = (1 + minimumMineralProximinity + .5) * (1 + minimumMineralProximinity + .5);
                var nexusDistanceSquared = 16f;
                if (minimumMineralProximinity == 0) { nexusDistanceSquared = 0; }
                var vector = new Vector2(point.X, point.Y);

                if (allowBlockBase || !BaseData.BaseLocations.Any(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), vector) < 25))
                {
                    var nexusClashes = ActiveUnitData.SelfUnits.Where(u => (u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON) && Vector2.DistanceSquared(u.Value.Position, vector) < squared + nexusDistanceSquared);
                    if (nexusClashes.Count() == 0)
                    {
                        var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);
                        if (clashes.Count() == 0)
                        {
                            if (Vector2.DistanceSquared(new Vector2(reference.X, reference.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance)
                            {
                                DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public Point2D FindProductionPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity = 2, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = true, bool requireSameHeight = false)
        {
            if (!allowBlockBase && size == 3)
            {
                var spot = ProtossProductionGridPlacement.FindPlacement(target, size, maxDistance, minimumMineralProximinity);
                if (spot != null) { return spot; }
            }

            if (size == 2)
            {
                var selfBase = BaseData.BaseLocations.FirstOrDefault(b => (b.Location.X == target.X && b.Location.Y == target.Y) && !(b.Location.X == TargetingData.SelfMainBasePoint.X && b.Location.Y == TargetingData.SelfMainBasePoint.Y) && !(b.Location.X == TargetingData.NaturalBasePoint.X && b.Location.Y == TargetingData.NaturalBasePoint.Y));
                if (selfBase != null)
                {
                    var location = ProtectNexusCannonPlacement.FindPlacement(target, UnitTypes.PROTOSS_PHOTONCANNON, 1, requireSameHeight: requireSameHeight);
                    if (location != null)
                    {
                        return location;
                    }
                }
                var gridPlacement = ProtossDefensiveGridPlacement.FindPlacement(target, UnitTypes.PROTOSS_PHOTONCANNON, (int)size, minimumMineralProximinity == 0, maxDistance, true, wallOffType, requireVision, allowBlockBase);
                if (gridPlacement != null)
                {
                    return gridPlacement;
                }
            }

            if (size == 3 && allowBlockBase)
            {
                var gridPlacement = ProtossProxyGridPlacement.FindPlacement(target, UnitTypes.PROTOSS_GATEWAY, (int)size, minimumMineralProximinity == 0, maxDistance, true, wallOffType, requireVision, allowBlockBase);
                if (gridPlacement != null)
                {
                    return gridPlacement;
                }
            }

            var startHeight = MapDataService.MapHeight(target);

            var targetVector = new Vector2(target.X, target.Y);
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector));
            foreach (var powerSource in powerSources)
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 8) * (maxDistance + 8)) 
                { 
                    break; 
                }

                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7 - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius <= powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 24.0;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        point = new Point2D { X = (float)Math.Round(point.X * 2f) / 2f, Y = (float)(Math.Round(point.Y * 2f) / 2f) };

                        if (size == 3)
                        {
                            if (point.X % 1 != .5)
                            {
                                point.X -= .5f;
                            }
                            if (point.Y % 1 != .5)
                            {
                                point.Y -= .5f;
                            }
                        }
                        else if (size == 2)
                        {
                            if (point.X % 1 != 0)
                            {
                                point.X -= .5f;
                            }
                            if (point.Y % 1 != 0)
                            {
                                point.Y -= .5f;
                            }
                        }

                        var vector = new Vector2(point.X, point.Y);
                        var tooClose = false;

                        if (!BuildOptions.AllowBlockWall && MapDataService.MapData?.WallData != null && MapDataService.MapData.WallData.Any(d => d.FullDepotWall != null && d.FullDepotWall.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), vector) < 25)))
                        {
                            tooClose = true;
                        }

                        if (!allowBlockBase && BaseData.BaseLocations.Any(b => Vector2.DistanceSquared(new Vector2(b.Location.X, b.Location.Y), vector) < 25))
                        {
                            tooClose = true;
                        }

                        if (!tooClose && BuildingService.SameHeight(point.X, point.Y, size + 1 / 2.0f) && 
                            (minimumMineralProximinity == 0 || !BuildingService.BlocksResourceCenter(point.X, point.Y, size + 1 / 2.0f)) && 
                            BuildingService.AreaBuildable(point.X, point.Y, (size + 1) / 2.0f) && !BuildingService.Blocked(point.X, point.Y, (size + 1) / 2.0f) &&  // size +1 because want 1 space to move around to prevent walling self in
                            !BuildingService.HasAnyCreep(point.X, point.Y, size / 2.0f) && 
                            !BuildingService.BlocksPath(point.X, point.Y, size / 2f))
                        {
                            var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                            var squared = (1 + minimumMineralProximinity + (size / 2f)) * (1 + minimumMineralProximinity + (size / 2f));
                            var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);

                            if (clashes.Count() == 0)
                            {
                                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance && Vector2.DistanceSquared(vector, powerSource.UnitCalculation.Position) <= 36)
                                {
                                    if (!BlocksWall(vector))
                                    {
                                        if (!requireSameHeight || MapDataService.MapHeight(point) == startHeight)
                                        {
                                            if (!LastLocations.Any(l => l.X == x && l.Y == y))
                                            {
                                                DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                                                LastLocations.Add(point);
                                                if (LastLocations.Count() > 5)
                                                {
                                                    LastLocations.RemoveAt(0);
                                                }

                                                return point;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        angle += sliceSize;
                    }
                    radius += 1;
                }
            }

            if (requireSameHeight)
            {
                return null;
            }
            return FindProductionPlacementTryHarder(target, size, maxDistance, minimumMineralProximinity, allowBlockBase);
        }

        bool BlocksWall(Vector2 vector)
        {
            foreach (var wallData in MapDataService.MapData.WallData.Where(b => BaseData.BaseLocations.Take(2).Any(l => l.Location.X == b.BasePosition.X && l.Location.Y == b.BasePosition.Y)))
            {
                if (wallData.Block != null)
                {
                    var blockVector = new Vector2(wallData.Block.X, wallData.Block.Y);
                    if (Vector2.DistanceSquared(blockVector, vector) < 16)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        Point2D FindProductionPlacementTryHarder(Point2D target, float size, float maxDistance, float minimumMineralProximinity, bool allowBlockBase)
        {
            var targetVector = new Vector2(target.X, target.Y);
            var powerSources = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && c.UnitCalculation.Unit.BuildProgress == 1).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector));

            foreach (var powerSource in powerSources)
            {
                if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), powerSource.UnitCalculation.Position) > (maxDistance + 16) * (maxDistance + 16))
                {
                    break;
                }

                var x = powerSource.UnitCalculation.Unit.Pos.X;
                var y = powerSource.UnitCalculation.Unit.Pos.Y;
                var radius = size / 2f;
                var powerRadius = 7 - (size / 2f);

                // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than powerRadius
                while (radius <= powerRadius)
                {
                    var fullCircle = Math.PI * 2;
                    var sliceSize = fullCircle / 48f;
                    var angle = 0.0;
                    while (angle + (sliceSize / 2) < fullCircle)
                    {
                        var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                        point = new Point2D { X = (float)Math.Round(point.X * 2f) / 2f, Y = (float)(Math.Round(point.Y * 2f) / 2f) };

                        if (size == 3)
                        {
                            if (point.X % 1 != .5)
                            {
                                point.X -= .5f;
                            }
                            if (point.Y % 1 != .5)
                            {
                                point.Y -= .5f;
                            }
                        }
                        else if (size == 2)
                        {
                            if (point.X % 1 != 0)
                            {
                                point.X -= .5f;
                            }
                            if (point.Y % 1 != 0)
                            {
                                point.Y -= .5f;
                            }
                        }

                        var vector = new Vector2(point.X, point.Y);
                        var tooClose = false;
                        if (!BuildOptions.AllowBlockWall && MapDataService.MapData?.WallData != null && MapDataService.MapData.WallData.Any(d => d.FullDepotWall != null && d.FullDepotWall.Any(p => Vector2.DistanceSquared(new Vector2(p.X, p.Y), vector) < 16)))
                        {
                            tooClose = true;
                        }

                        if (!allowBlockBase && BuildingService.BlocksResourceCenter(x, y, size/2f))
                        {
                            tooClose = true;
                        }

                        if (BuildingService.Blocked(point.X, point.Y, size / 2.0f, 0))
                        {
                            tooClose = true;
                        }

                        if (!tooClose)
                        {
                            if ((minimumMineralProximinity == 0 || !BuildingService.BlocksResourceCenter(point.X, point.Y, (size - .5f) / 2.0f)))
                            {
                                if (BuildingService.AreaBuildable(point.X, point.Y, size / 2.0f) && !BuildingService.HasAnyCreep(point.X, point.Y, size / 2.0f) && !BuildingService.BlocksGas(point.X, point.Y, size / 2.0f))
                                {
                                    var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                                    var squared = (minimumMineralProximinity + (size / 2f)) * (minimumMineralProximinity + (size / 2f));
                                    var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);

                                    if (clashes.Count() == 0)
                                    {
                                        if (Vector2.DistanceSquared(new Vector2(target.X, target.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance && Vector2.DistanceSquared(vector, powerSource.UnitCalculation.Position) <= (6.5 - size / 2f) * (6.5 - size / 2f))
                                        {
                                            if (BuildOptions.AllowBlockWall || !BlocksWall(vector))
                                            {
                                                if (!LastLocations.Any(l => l.X == x && l.Y == y))
                                                {
                                                    DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });

                                                    LastLocations.Add(point);
                                                    if (LastLocations.Count() > 5)
                                                    {
                                                        LastLocations.RemoveAt(0);
                                                    }

                                                    return point;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                      
                        angle += sliceSize;
                    }
                    radius += 1;
                }
            }
            return null;
        }
    }
}
