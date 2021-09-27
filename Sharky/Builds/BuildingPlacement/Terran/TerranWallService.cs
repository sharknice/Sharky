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
    }
}
