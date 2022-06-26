using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingService
    {
        MapData MapData;
        ActiveUnitData ActiveUnitData;
        TargetingData TargetingData;
        BaseData BaseData;

        public BuildingService(MapData mapData, ActiveUnitData activeUnitData, TargetingData targetingData, BaseData baseData)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
            TargetingData = targetingData;
            BaseData = baseData;
        }

        public bool AreaBuildable(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].CurrentlyBuildable && MapData.Map[(int)x][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x + (int)radius][(int)y].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x - (int)radius][(int)y].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].CurrentlyBuildable;
        }

        public bool AreaVisible(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].InSelfVision && MapData.Map[(int)x][(int)y + (int)radius].InSelfVision && MapData.Map[(int)x][(int)y - (int)radius].InSelfVision
                && MapData.Map[(int)x + (int)radius][(int)y].InSelfVision && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].InSelfVision && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].InSelfVision
                && MapData.Map[(int)x - (int)radius][(int)y].InSelfVision && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].InSelfVision && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].InSelfVision;
        }

        public bool RoomBelowAndAbove(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].CurrentlyBuildable
                && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].CurrentlyBuildable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].CurrentlyBuildable;
        }

        public bool SameHeight(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            var height = MapData.Map[(int)x][(int)y].TerrainHeight;
            return (height == MapData.Map[(int)x][(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x][(int)y - (int)radius].TerrainHeight)
               && (height ==  MapData.Map[(int)x + (int)radius][(int)y].TerrainHeight) && (height == MapData.Map[(int)x + (int)radius][(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x + (int)radius][(int)y - (int)radius].TerrainHeight)
               && (height == MapData.Map[(int)x - (int)radius][(int)y].TerrainHeight) && (height == MapData.Map[(int)x - (int)radius][(int)y + (int)radius].TerrainHeight) && (height == MapData.Map[(int)x - (int)radius][(int)y - (int)radius].TerrainHeight);
        }

        public bool Blocked(float x, float y, float radius, float padding = .5f, ulong tag = 0)
        {
            if (ActiveUnitData.NeutralUnits.Any(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius) * (u.Value.Unit.Radius + padding + radius)))
            {
                return true;
            }

            if (ActiveUnitData.NeutralUnits.Where(u => u.Value.Unit.Health == 400).Any(u => Vector2.DistanceSquared(new Vector2(x, y), u.Value.Position) < (u.Value.Unit.Radius + padding + radius + 1.5) * (u.Value.Unit.Radius + padding + radius + 1.5)))
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

        float BuildingPlacementRadius(float radius)
        {
            var result = Math.Floor(radius * 2f) / 2;
            return (float)result;
        }

        public bool BlocksResourceCenter(float x, float y, float radius)
        {
            if (BaseData.BaseLocations.Any(b => System.Drawing.RectangleF.Intersect(new System.Drawing.RectangleF(x, y, radius * 2, radius * 2), new System.Drawing.RectangleF(b.Location.X, b.Location.Y, 5, 5)) != System.Drawing.RectangleF.Empty))
            {
                return true;
            }
            return false;
        }

        public bool BlocksPath(float x, float y, float unitRadius)
        {
            var radius = unitRadius + 4.5f;
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return true;
            }
            var blocked = MapData.Map[(int)x][(int)y].Walkable && MapData.Map[(int)x][(int)y + (int)radius].Walkable && MapData.Map[(int)x][(int)y - (int)radius].Walkable
                && MapData.Map[(int)x + (int)radius][(int)y].Walkable && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].Walkable && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].Walkable
                && MapData.Map[(int)x - (int)radius][(int)y].Walkable && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].Walkable && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].Walkable;

            if (blocked) { return true; }
            return !SameHeight(x, y, radius);
        }

        public bool HasCreep(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].HasCreep && MapData.Map[(int)x][(int)y + (int)radius].HasCreep && MapData.Map[(int)x][(int)y - (int)radius].HasCreep
                && MapData.Map[(int)x + (int)radius][(int)y].HasCreep && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].HasCreep && MapData.Map[(int)x + (int)radius][(int)y - (int)radius].HasCreep
                && MapData.Map[(int)x - (int)radius][(int)y].HasCreep && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].HasCreep && MapData.Map[(int)x - (int)radius][(int)y - (int)radius].HasCreep;
        }

        public bool HasAnyCreep(float x, float y, float radius)
        {
            if (x - radius < 0 || y - radius < 0 || x + radius >= MapData.MapWidth || y + radius >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x][(int)y].HasCreep || MapData.Map[(int)x][(int)y + (int)radius].HasCreep || MapData.Map[(int)x][(int)y - (int)radius].HasCreep
                || MapData.Map[(int)x + (int)radius][(int)y].HasCreep && MapData.Map[(int)x + (int)radius][(int)y + (int)radius].HasCreep || MapData.Map[(int)x + (int)radius][(int)y - (int)radius].HasCreep
                || MapData.Map[(int)x - (int)radius][(int)y].HasCreep && MapData.Map[(int)x - (int)radius][(int)y + (int)radius].HasCreep || MapData.Map[(int)x - (int)radius][(int)y - (int)radius].HasCreep;
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
            if (BaseData.BaseLocations.Any(b => b.VespeneGeysers.Any(g => Vector2.DistanceSquared(new Vector2(x, y), new Vector2(g.Pos.X, g.Pos.Y)) < (4 + radius) * (4 + radius))))
            {
                return true;
            }
            return false;
        }

        public BaseLocation GetNextBaseLocation()
        {
            var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            var openBases = BaseData.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25));

            foreach (var openBase in openBases)
            {
                if (AreaBuildable(openBase.Location.X, openBase.Location.Y, 2) && !Blocked(openBase.Location.X, openBase.Location.Y, 2.5f))
                {
                    return openBase;
                }

            }
            return null;
        }
    }
}
