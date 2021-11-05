using SC2APIProtocol;
using Sharky.Pathing;
using System.Linq;

namespace Sharky.Builds.BuildingPlacement
{
    public class TerranWallService
    {
        ActiveUnitData ActiveUnitData;
        WallService WallService;
        MapData MapData;
        BaseData BaseData;

        public TerranWallService(ActiveUnitData activeUnitData, MapData mapData, BaseData baseData, WallService wallService)
        {
            ActiveUnitData = activeUnitData;
            WallService = wallService;
            MapData = mapData;
            BaseData = baseData;
        }

        public Point2D FindTerranPlacement(WallData wallData, UnitTypes unitType)
        {
            if (unitType == UnitTypes.TERRAN_SUPPLYDEPOT)
            {
                if (wallData.Depots == null) { return null; }
                var existingDepots = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
                foreach (var spot in wallData.Depots)
                {
                    if (!existingDepots.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y) && WallService.Buildable(spot, .5f))
                    {
                        return spot;
                    }
                }
                return null;
            }
            if (unitType == UnitTypes.TERRAN_BUNKER)
            {
                if (wallData.Bunkers == null) { return null; }
                var existingBunkers = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER);
                foreach (var spot in wallData.Bunkers)
                {
                    if (!existingBunkers.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y) && WallService.Buildable(spot, .5f))
                    {
                        return spot;
                    }
                }
                return null;
            }
            if (wallData.Production == null) { return null; }
            if (unitType == UnitTypes.TERRAN_BARRACKSTECHLAB && wallData.ProductionWithAddon != null)
            {
                foreach (var spot in wallData.ProductionWithAddon)
                {
                    if (WallService.Buildable(spot, 1))
                    {
                        return spot;
                    }
                }
            }
            foreach (var spot in wallData.Production)
            {
                if (WallService.Buildable(spot, 1))
                {
                    return spot;
                }
            }
            return null;
        }

        public Point2D FindProductionWallPlacement(Point2D target, UnitTypes unitType, float size, float maxDistance, float minimumMineralProximinity)
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                foreach (var selfBase in BaseData.SelfBases)
                {
                    var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == selfBase.Location.X && b.BasePosition.Y == selfBase.Location.Y);
                    if (unitType == UnitTypes.TERRAN_BARRACKSTECHLAB)
                    {
                        var spot = GetOpenProductionAddonPlacement(wallData);
                        if (spot != null) { return spot; }
                    }
                    else
                    {
                        var spot = GetOpenProductionPlacement(wallData);
                        if (spot != null) { return spot; }
                    }
                }
            }

            return null;
        }

        Point2D GetOpenProductionAddonPlacement(WallData wallData)
        {
            if (wallData != null && wallData.ProductionWithAddon != null)
            {
                var existingBuildings = ActiveUnitData.SelfUnits.Values.Where(u => !u.Unit.IsFlying && u.Attributes.Contains(Attribute.Structure));
                foreach (var spot in wallData.ProductionWithAddon)
                {
                    if (!existingBuildings.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y) && WallService.Buildable(spot, .5f))
                    {
                        return spot;
                    }
                }
            }
            return null;
        }

        Point2D GetOpenProductionPlacement(WallData wallData)
        {
            if (wallData != null && wallData.Production != null)
            {
                var existingBuildings = ActiveUnitData.SelfUnits.Values.Where(u => !u.Unit.IsFlying && u.Attributes.Contains(Attribute.Structure));
                foreach (var spot in wallData.Production)
                {
                    if (!existingBuildings.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y) && WallService.Buildable(spot, .5f))
                    {
                        return spot;
                    }
                }
            }
            return null;
        }

        public Point2D FindSupplyDepotWallPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                foreach (var selfBase in BaseData.SelfBases)
                {
                    var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == selfBase.Location.X && b.BasePosition.Y == selfBase.Location.Y);
                    var spot = GetOpenDepotPlacement(wallData);
                    if (spot != null) { return spot; }
                }
            }

            return null;
        }

        Point2D GetOpenDepotPlacement(WallData wallData)
        {
            if(wallData != null && wallData.Depots != null)
            {
                var existingDepots = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
                foreach (var spot in wallData.Depots)
                {
                    if (!existingDepots.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y))
                    {
                        if (WallService.Buildable(spot, .9f))
                        {
                            return spot;
                        }
                    }
                }
            }
            return null;
        }

        public Point2D FindBunkerPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                foreach (var selfBase in BaseData.SelfBases)
                {
                    var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == selfBase.Location.X && b.BasePosition.Y == selfBase.Location.Y);
                    var spot = GetOpenBunkerPlacement(wallData);
                    if (spot != null) { return spot; }
                }
            }

            return null;
        }

        Point2D GetOpenBunkerPlacement(WallData wallData)
        {
            if (wallData != null && wallData.Bunkers != null)
            {
                var existing = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER);
                foreach (var spot in wallData.Bunkers)
                {
                    if (!existing.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y))
                    {
                        if (WallService.Buildable(spot, .9f))
                        {
                            return spot;
                        }
                    }
                }
            }
            return null;
        }

        public bool MainWallComplete()
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                var baseData = BaseData.SelfBases.FirstOrDefault();
                if (baseData != null)
                {
                    var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == baseData.Location.X && b.BasePosition.Y == baseData.Location.Y);
                    if (wallData != null)
                    {
                        if (wallData.Depots != null)
                        {
                            var existingDepots = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
                            if (!wallData.Depots.All(spot => existingDepots.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y)))
                            {
                                return false;
                            }
                        }
                        var productionFilled = false;
                        if (wallData.Production == null && wallData.ProductionWithAddon == null)
                        {
                            productionFilled = true;
                        }
                        if (wallData.Production != null)
                        {
                            var existingBuildings = ActiveUnitData.SelfUnits.Values.Where(u => u.Attributes.Contains(Attribute.Structure));
                            if (wallData.Production.All(spot => existingBuildings.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y)))
                            {
                                productionFilled = true;
                            }
                        }
                        if (wallData.ProductionWithAddon != null)
                        {
                            var existingBuildings = ActiveUnitData.SelfUnits.Values.Where(u => u.Attributes.Contains(Attribute.Structure) && u.Unit.HasAddOnTag);
                            if (wallData.ProductionWithAddon.All(spot => existingBuildings.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y)))
                            {
                                productionFilled = true;
                            }
                        }
                        return productionFilled;
                    }
                }
            }
            return false;
        }
    }
}
