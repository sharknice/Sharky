namespace Sharky.Builds.BuildingPlacement
{
    public class ProtossWallService
    {
        SharkyUnitData SharkyUnitData;
        ActiveUnitData ActiveUnitData;
        WallService WallService;

        public ProtossWallService(SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, WallService wallService)
        {
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            WallService = wallService;
        }

        public Point2D FindPylonPlacement(WallData wallData, float maxDistance, float minimumMineralProximinity = 0, WallOffType wallOffType = WallOffType.None)
        {
            if (wallOffType == WallOffType.Partial)
            {
                return FindPartialWallPylonPlacement(wallData);
            }

            return FindFullWallPylonPlacement(wallData);
        }

        public Point2D FindPartialWallPylonPlacement(WallData wallData)
        {
            if (wallData?.Pylons == null) { return null; }
            var unitData = SharkyUnitData.BuildingData[UnitTypes.PROTOSS_PYLON];
            var pylonRadius = (unitData.Size / 2f) - .00001f;
            var existingPylons = ActiveUnitData.SelfUnits.Values.Where(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON);
            foreach (var pylon in wallData.Pylons)
            {
                if (!existingPylons.Any(e => e.Position.X == pylon.X && e.Position.Y == pylon.Y) && WallService.Buildable(pylon, pylonRadius))
                {
                    return pylon;
                }
            }
            return null;
        }

        public Point2D FindFullWallPylonPlacement(WallData wallData)
        {
            return FindPartialWallPylonPlacement(wallData);
        }

        public bool Powered(IEnumerable<UnitCommander> powerSources, Point2D point, float radius)
        {
            var vector = new Vector2(point.X, point.Y);
            return powerSources.Any(p => Vector2.DistanceSquared(p.UnitCalculation.Position, vector) <= (7) * (7));
        }
    }
}
