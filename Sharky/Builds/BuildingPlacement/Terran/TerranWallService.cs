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
        TargetingData TargetingData;

        public TerranWallService(ActiveUnitData activeUnitData, MapData mapData, TargetingData targetingData, WallService wallService)
        {
            ActiveUnitData = activeUnitData;
            WallService = wallService;
            MapData = mapData;
            TargetingData = targetingData;
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

        public Point2D FindSupplyDepotWallPlacement(Point2D target, float size, float maxDistance, float minimumMineralProximinity)
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                if (wallData != null && wallData.Depots != null)
                {
                    var existingDepots = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
                    foreach (var spot in wallData.Depots)
                    {
                        if (!existingDepots.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y) && WallService.Buildable(spot, .5f))
                        {
                            return spot;
                        }
                    }
                }
            }

            return null;
        }

        public bool WallComplete()
        {
            if (NaturalWallComplete() || MainWallComplete())
            {
                return true;
            }
            return false;
        }

        public bool MainWallComplete()
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.SelfMainBasePoint.X && b.BasePosition.Y == TargetingData.SelfMainBasePoint.Y);
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
            return false;
        }

        public bool NaturalWallComplete()
        {
            if (MapData != null && MapData.TerranWallData != null)
            {
                var wallData = MapData.TerranWallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                if (wallData != null && wallData.Depots != null)
                {
                    var existingDepots = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOT || u.Unit.UnitType == (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED);
                    return wallData.Depots.All(spot => existingDepots.Any(e => e.Position.X == spot.X && e.Position.Y == spot.Y));
                }
            }
            return false;
        }
    }
}
