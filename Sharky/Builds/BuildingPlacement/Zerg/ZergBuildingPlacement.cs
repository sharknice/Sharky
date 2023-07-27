namespace Sharky.Builds.BuildingPlacement
{
    public class ZergBuildingPlacement : IBuildingPlacement
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        DebugService DebugService;
        BuildingService BuildingService;

        List<Point2D> LastLocations;

        public ZergBuildingPlacement(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, DebugService debugService, BuildingService buildingService)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            DebugService = debugService;
            BuildingService = buildingService;

            LastLocations = new List<Point2D>();
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size, bool ignoreResourceProximity = false, float maxDistance = 50, bool requireSameHeight = false, WallOffType wallOffType = WallOffType.None, bool requireVision = false, bool allowBlockBase = true)
        {
            var mineralProximity = 2;
            if (ignoreResourceProximity) { mineralProximity = 0; };

            return FindTechPlacement(target, size, maxDistance, mineralProximity, requireVision);
        }

        public Point2D FindTechPlacement(Point2D reference, float size, float maxDistance, float minimumMineralProximinity = 2, bool requireVision = false)
        {
            var x = reference.X;
            var y = reference.Y;
            var radius = size / 2f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < maxDistance / 2.0)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / (4.0 + radius);
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };
                    if (size == 3)
                    {
                        point = GetValidSize3BuildLocation(point);
                    }
                    if (!requireVision || (requireVision && BuildingService.AreaVisible(point.X, point.Y, size / 2f)))
                    {
                        if (BuildingService.HasCreep(point.X, point.Y, size / 2.0f) && BuildingService.AreaBuildable(point.X, point.Y, size / 2f) && !BuildingService.Blocked(point.X, point.Y, size / 2f) && !BuildingService.BlocksResourceCenter(point.X, point.Y, 1))
                        {
                            var mineralFields = ActiveUnitData.NeutralUnits.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType));
                            var squared = (1 + minimumMineralProximinity + (size / 2f)) * (1 + minimumMineralProximinity + (size / 2f));
                            var clashes = mineralFields.Where(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < squared);

                            if (clashes.Count() == 0)
                            {
                                var productionStructures = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_FACTORY || u.Value.Unit.UnitType == (uint)UnitTypes.TERRAN_STARPORT);
                                if (!productionStructures.Any(u => Vector2.DistanceSquared(u.Value.Position, new Vector2(point.X, point.Y)) < 16))
                                {
                                    if (Vector2.DistanceSquared(new Vector2(reference.X, reference.Y), new Vector2(point.X, point.Y)) <= maxDistance * maxDistance)
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

            return null;
        }

        protected Point2D GetValidSize3BuildLocation(Point2D point)
        {
            if (point.X % 1 != .5)
            {
                point.X = (float)(Math.Round(point.X / 0.5) * 0.5);
                if (point.X % 1 != .5)
                {
                    point.X += .5f;
                }
            }
            if (point.Y % 1 != .5)
            {
                point.Y = (float)(Math.Round(point.Y / 0.5) * 0.5);
                if (point.Y % 1 != .5)
                {
                    point.Y += .5f;
                }
            }

            return point;
        }
    }
}
