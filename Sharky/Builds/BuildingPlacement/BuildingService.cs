namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingService
    {
        MapData MapData;
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;
        BaseData BaseData;
        SharkyUnitData SharkyUnitData;

        public BuildingService(MapData mapData, ActiveUnitData activeUnitData, TargetingData targetingData, BaseData baseData, SharkyUnitData sharkyUnitData)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
            BaseData = baseData;
            SharkyUnitData = sharkyUnitData;
        }

        public bool AreaBuildable(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }

            radius = radius - .01f;

            var rectangle = new System.Drawing.RectangleF(x - radius, y - radius, (radius * 2), (radius * 2));
            
            var currentX = x - radius - 1;
            var currentY = y - radius - 1;
            while (currentX <= x + radius + 1)
            {
                while (currentY <= y + radius + 1)
                {
                    if (currentX < 0 || currentY < 0 || currentX >= MapData.MapWidth || currentY >= MapData.MapHeight)
                    {

                    }
                    else if (!MapData.Map[(int)currentX,(int)currentY].CurrentlyBuildable)
                    {
                        if (rectangle.Contains(currentX, currentY))
                        {
                            return false;
                        }
                    }
                    currentY++;
                }
                currentY = y - radius - 1;
                currentX++;
            }

            return true;
        }

        public bool AreaVisible(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x,(int)y].InSelfVision && MapData.Map[(int)x,(int)y + (int)radius].InSelfVision && MapData.Map[(int)x,(int)y - (int)radius].InSelfVision
                && MapData.Map[(int)x + (int)radius,(int)y].InSelfVision && MapData.Map[(int)x + (int)radius,(int)y + (int)radius].InSelfVision && MapData.Map[(int)x + (int)radius,(int)y - (int)radius].InSelfVision
                && MapData.Map[(int)x - (int)radius,(int)y].InSelfVision && MapData.Map[(int)x - (int)radius,(int)y + (int)radius].InSelfVision && MapData.Map[(int)x - (int)radius,(int)y - (int)radius].InSelfVision;
        }

        public bool RoomBelowAndAbove(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x,(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x,(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x + (int)radius,(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x + (int)radius,(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x - (int)radius,(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x - (int)radius,(int)y - (int)radius].CurrentlyBuildable;
        }

        public bool RoomForAddonsOnOtherBuildings(float x, float y, float radius)
        {
            if (ActiveUnitData.SelfUnits.Values.Any(c => 
                (c.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || c.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || c.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT) &&
                CoordinatesBlocks(x, y, radius, c.Position.X + 2.5f, c.Position.Y - .5f, 1)))
            {
                return false;
            }
            return true;
        }

        public bool SameHeight(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            var height = MapData.Map[(int)x,(int)y].TerrainHeight;
            return (height == MapData.Map[(int)x,(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x,(int)y - (int)radius].TerrainHeight)
               && (height ==  MapData.Map[(int)x + (int)radius,(int)y].TerrainHeight) && (height == MapData.Map[(int)x + (int)radius,(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x + (int)radius,(int)y - (int)radius].TerrainHeight)
               && (height == MapData.Map[(int)x - (int)radius,(int)y].TerrainHeight) && (height == MapData.Map[(int)x - (int)radius,(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x - (int)radius,(int)y - (int)radius].TerrainHeight);
        }

        public bool Blocked(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            if (ActiveUnitData.NeutralUnits.Any(u => !u.Value.UnitTypeData.Name.Contains("MineralField") && u.Value.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && BuildingBlocks(x, y, radius, u.Value.Unit)))
            {
                return true;
            }

            if (ActiveUnitData.NeutralUnits.Any(u => u.Value.UnitTypeData.Name.Contains("MineralField") && MineralBlocks(x, y, radius, u.Value.Unit)))
            {
                return true;
            }

            if (ActiveUnitData.NeutralUnits.Where(u => u.Value.Unit.HealthMax == 400).Any(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius + 1.5) * (u.Value.Unit.Radius + padding + radius + 1.5)))
            {
                return true;
            }

            if (ActiveUnitData.EnemyUnits.Any(u => !u.Value.Unit.IsFlying && BuildingBlocks(x, y, radius, u.Value.Unit)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag && !c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) &&
                (c.Value.UnitCalculation.Unit.BuildProgress < 1 || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_EGG || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_LARVA) &&
                BuildingBlocks(x, y, radius, c.Value.UnitCalculation.Unit)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag && 
                (c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !c.Value.UnitCalculation.Unit.IsFlying && BuildingBlocks(x, y, radius, c.Value.UnitCalculation.Unit))))
            {
                return true;
            }

            return false;
        }

        public bool BlockedByUnits(float x, float y, float radius, UnitCalculation unitCalculation)
        {
            if (unitCalculation.NearbyAllies.Any(c => !c.Unit.IsFlying && BuildingBlocks(x, y, radius, c.Unit)))
            {
                return true;
            }

            return false;
        }

        public bool BlockedByStructuresOrMinerals(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            foreach (var neutralUnit in ActiveUnitData.NeutralUnits.Where(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                if (SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)neutralUnit.Value.Unit.UnitType))
                {
                    //if (neutralUnit.Value.Position.X >= x + (1 + radius) && neutralUnit.Value.Position.X <= x - (1 + radius) &&
                    //    neutralUnit.Value.Position.Y >= y + (.5 + radius) && neutralUnit.Value.Position.Y <= y - (.5 + radius))
                    //{
                    //    continue;
                    //}
                    if (!MineralBlocks(x, y, radius, neutralUnit.Value.Unit))
                    {
                        continue;
                    }
                }
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag && !c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) &&
                (c.Value.UnitCalculation.Unit.BuildProgress < 1 || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_EGG || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_LARVA) &&
                BuildingBlocks(x, y, radius, c.Value.UnitCalculation.Unit)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag &&
                (c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !c.Value.UnitCalculation.Unit.IsFlying && BuildingBlocks(x, y, radius, c.Value.UnitCalculation.Unit))))
            {
                return true;
            }

            return false;
        }

        public bool BlockedByEnemyUnits(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            if (ActiveUnitData.EnemyUnits.Any(c => c.Key != tag && !c.Value.Unit.IsFlying && BuildingBlocks(x, y, radius, c.Value.Unit)))
            {
                return true;
            }

            return false;
        }

        public bool BlockedByStructures(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            foreach (var neutralUnit in ActiveUnitData.NeutralUnits.Where(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                if (!neutralUnit.Value.UnitTypeData.Name.Contains("MineralField"))
                {
                    return true;
                }
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag && !c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) &&
                (c.Value.UnitCalculation.Unit.BuildProgress < 1 || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_EGG || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.ZERG_LARVA) &&
                BuildingBlocks(x, y, radius, c.Value.UnitCalculation.Unit)))
            {
                return true;
            }

            if (ActiveUnitData.Commanders.Any(c => c.Key != tag &&
                (c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !c.Value.UnitCalculation.Unit.IsFlying && BuildingBlocks(x, y, radius, c.Value.UnitCalculation.Unit))))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<UnitCalculation> BlockedByMinerals(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            return ActiveUnitData.NeutralUnits.Values.Where(u => u.UnitTypeData.Name.Contains("MineralField") && MineralBlocks(x, y, radius, u.Unit));
        }

        bool MineralBlocks(float x, float y, float radius, Unit mineral)
        {
            var rectangle = new System.Drawing.RectangleF(x - radius, y - radius, (radius * 2), (radius * 2));
            var existing = new System.Drawing.RectangleF(mineral.Pos.X - 1, mineral.Pos.Y - .5f, 2, 1);
            var intersection = System.Drawing.RectangleF.Intersect(rectangle, existing);
            if (intersection.Width == 0 || intersection.Height == 0)
            {
                return false;
            }
            return true;
        }

        bool BuildingBlocks(float x, float y, float radius, Unit building)
        {
            var rectangle = new System.Drawing.RectangleF(x - radius, y - radius, (radius * 2), (radius * 2));
            var buildingRadius = BuildingPlacementRadius(building.Radius);
            var existing = new System.Drawing.RectangleF(building.Pos.X - buildingRadius, building.Pos.Y - buildingRadius, buildingRadius * 2, buildingRadius * 2);
            var intersection = System.Drawing.RectangleF.Intersect(rectangle, existing);
            if (intersection.Width == 0 || intersection.Height == 0)
            {
                return false;
            }
            return true;
        }

        bool CoordinatesBlocks(float x, float y, float radius, float x2, float y2, float radius2)
        {
            var rectangle = new System.Drawing.RectangleF(x - radius, y - radius, (radius * 2), (radius * 2));
            var buildingRadius = BuildingPlacementRadius(radius2);
            var existing = new System.Drawing.RectangleF(x2 - buildingRadius, y2 - buildingRadius, buildingRadius * 2, buildingRadius * 2);
            var intersection = System.Drawing.RectangleF.Intersect(rectangle, existing);
            if (intersection.Width == 0 || intersection.Height == 0)
            {
                return false;
            }
            return true;
        }

        float BuildingPlacementRadius(float radius)
        {
            var result = Math.Floor(radius * 2f) / 2;
            if (result == 0)
            {
                return radius;
            }
            return (float)result;
        }

        public bool BlocksResourceCenter(float x, float y, float radius)
        {
            foreach (var baseLocation in BaseData.BaseLocations)
            {
                var rectangle = new System.Drawing.RectangleF(x - radius, y - radius, (radius * 2), (radius * 2));
                var existing = new System.Drawing.RectangleF(baseLocation.Location.X - 2.5f, baseLocation.Location.Y - 2.5f, 5, 5);
                var intersection = System.Drawing.RectangleF.Intersect(rectangle, existing);
                if (intersection.Width != 0 && intersection.Height != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool BlocksPath(float x, float y, float unitRadius)
        {
            var radius = unitRadius + 2f;
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return true;
            }
            var blocked = MapData.Map[(int)x,(int)y].Walkable && MapData.Map[(int)x,(int)y + (int)radius].Walkable && MapData.Map[(int)x,(int)y - (int)radius].Walkable
                && MapData.Map[(int)x + (int)radius,(int)y].Walkable && MapData.Map[(int)x + (int)radius,(int)y + (int)radius].Walkable && MapData.Map[(int)x + (int)radius,(int)y - (int)radius].Walkable
                && MapData.Map[(int)x - (int)radius,(int)y].Walkable && MapData.Map[(int)x - (int)radius,(int)y + (int)radius].Walkable && MapData.Map[(int)x - (int)radius,(int)y - (int)radius].Walkable;

            if (blocked) { return true; }
            return !SameHeight(x, y, radius);
        }

        public bool InRangeOfEnemy(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x,(int)y].EnemyGroundDpsInRange > 0 || MapData.Map[(int)x,(int)y + (int)radius].EnemyGroundDpsInRange > 0 || MapData.Map[(int)x,(int)y - (int)radius].EnemyGroundDpsInRange > 0
                || MapData.Map[(int)x + (int)radius,(int)y].EnemyGroundDpsInRange > 0 || MapData.Map[(int)x + (int)radius,(int)y + (int)radius].EnemyGroundDpsInRange > 0 || MapData.Map[(int)x + (int)radius,(int)y - (int)radius].EnemyGroundDpsInRange > 0
                || MapData.Map[(int)x - (int)radius,(int)y].EnemyGroundDpsInRange > 0 || MapData.Map[(int)x - (int)radius,(int)y + (int)radius].EnemyGroundDpsInRange > 0 || MapData.Map[(int)x - (int)radius,(int)y - (int)radius].EnemyGroundDpsInRange > 0;
        }

        public bool HasCreep(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x,(int)y].HasCreep && MapData.Map[(int)x,(int)y + (int)radius].HasCreep && MapData.Map[(int)x,(int)y - (int)radius].HasCreep
                && MapData.Map[(int)x + (int)radius,(int)y].HasCreep && MapData.Map[(int)x + (int)radius,(int)y + (int)radius].HasCreep && MapData.Map[(int)x + (int)radius,(int)y - (int)radius].HasCreep
                && MapData.Map[(int)x - (int)radius,(int)y].HasCreep && MapData.Map[(int)x - (int)radius,(int)y + (int)radius].HasCreep && MapData.Map[(int)x - (int)radius,(int)y - (int)radius].HasCreep;
        }

        public bool HasAnyCreep(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x,(int)y].HasCreep || MapData.Map[(int)x,(int)y + (int)radius].HasCreep || MapData.Map[(int)x,(int)y - (int)radius].HasCreep
                || MapData.Map[(int)x + (int)radius,(int)y].HasCreep && MapData.Map[(int)x + (int)radius,(int)y + (int)radius].HasCreep || MapData.Map[(int)x + (int)radius,(int)y - (int)radius].HasCreep
                || MapData.Map[(int)x - (int)radius,(int)y].HasCreep && MapData.Map[(int)x - (int)radius,(int)y + (int)radius].HasCreep || MapData.Map[(int)x - (int)radius,(int)y - (int)radius].HasCreep;
        }

        public bool FullyWalled()
        {
            if (TargetingData.ForwardDefenseWallOffPoints == null) { return true; }

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    return false;
                }
            }

            return true;
        }

        public bool PartiallyWalled()
        {
            if (TargetingData.ForwardDefenseWallOffPoints == null) { return true; }

            var gaps = 0;

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    gaps++;
                    if (gaps > 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<Point2D> UnWalledPoints()
        {
            if (TargetingData.ForwardDefenseWallOffPoints == null) { return new List<Point2D>(); }

            var gaps = new List<Point2D>();

            foreach (var point in TargetingData.ForwardDefenseWallOffPoints)
            {
                var vector = new Vector2(point.X, point.Y);
                if (!ActiveUnitData.Commanders.Any(c => c.Value.UnitCalculation.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && Vector2.DistanceSquared(vector, c.Value.UnitCalculation.Position) < c.Value.UnitCalculation.Unit.Radius * c.Value.UnitCalculation.Unit.Radius))
                {
                    gaps.Add(point);
                }
            }

            return gaps;
        }

        public bool BlocksGas(float x, float y, float radius)
        {
            if (BaseData.BaseLocations.Any(b => b.VespeneGeysers.Any(g => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(b.Location.X, b.Location.Y)) < 16)))
            {
                return true;
            }
            return false;
        }

        public BaseLocation GetNextBaseLocation()
        {
            var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ResourceCenter));
            var openBases = BaseData.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25));

            foreach (var openBase in openBases)
            {
                if (AreaBuildable(openBase.Location.X, openBase.Location.Y, 2))
                {
                    if (!Blocked(openBase.Location.X, openBase.Location.Y, 2.5f))
                    {
                        return openBase;
                    }
                    return openBase;
                }

            }
            return null;
        }

        public IEnumerable<UnitCalculation> GetMineralsBlockingNextBase()
        {
            var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.HasFlag(UnitClassification.ResourceCenter));
            var openBases = BaseData.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25));

            foreach (var openBase in openBases)
            {
                if (AreaBuildable(openBase.Location.X, openBase.Location.Y, 2) && !BlockedByStructures(openBase.Location.X, openBase.Location.Y, 2.5f))
                {
                    return BlockedByMinerals(openBase.Location.X, openBase.Location.Y, 2.5f);
                }

            }
            return new List<UnitCalculation>();
        }
    }
}
