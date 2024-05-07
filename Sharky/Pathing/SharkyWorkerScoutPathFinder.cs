namespace Sharky.Pathing
{
    public class SharkyWorkerScoutPathFinder : IPathFinder
    {
        Grid GroundDamageGrid;
        int GroundDamageLastUpdate; 
        int MapLastUpdate;

        Grid WalkGrid;

        PathFinder PathFinder;
        MapData MapData;
        MapDataService MapDataService;
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;

        public SharkyWorkerScoutPathFinder(PathFinder pathFinder, MapData mapData, MapDataService mapDataService, DebugService debugService, ActiveUnitData activeUnitData, BaseData baseData)
        {
            PathFinder = pathFinder;
            MapData = mapData;
            MapDataService = mapDataService;
            ActiveUnitData = activeUnitData;
            BaseData = baseData;

            GroundDamageLastUpdate = -1;
            MapLastUpdate = -1;
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, PathFinder pathFinder = null)
        {
            if (pathFinder == null)
            {
                pathFinder = PathFinder;
            }
            var grid = GetMapGrid(frame);
            return GetPath(grid, startX, startY, endX, endY, pathFinder);
        }

        public List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var grid = GetGroundDamageGrid(frame);
            var path = GetPath(grid, startX, startY, endX, endY);
            if (!path.Any())
            {
                var cells = MapDataService.GetCells(startX, startY, 3);
                var end = new Vector2(endX, endY);
                var best = cells.Where(c => c.Walkable && !InMineralLine(c.X, c.Y)).OrderBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
                if (best != null)
                {
                    path = GetPath(grid, best.X, best.Y, endX, endY);
                }
                if (!path.Any())
                {
                    path = new List<Vector2> { new Vector2(startX, startY), new Vector2(endX, endX) };
                }
            }

            return path;
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, float radius)
        {
            var xStart = (int)startX;
            var yStart = (int)startY;
            var range = (int)radius;

            if (xStart - range < 0)
            {
                xStart = 0;
            }
            if (yStart - range < 0)
            {
                yStart = 0;
            }
            if (xStart + range > MapData.MapWidth)
            {
                xStart = MapData.MapWidth - range;
            }
            if ( + range > MapData.MapHeight)
            {
                yStart = MapData.MapHeight - range;
            }

            var xMin = xStart - range;
            var xMax = xStart + range;
            var yMin = yStart - range;
            var yMax = yStart + range;

            if (xMin < 0)
            {
                xMin = 0;
            }
            if (yMin < 0)
            {
                yMin = 0;
            }
            if (xMax > MapData.MapWidth)
            {
                xMax = MapData.MapWidth;
            }
            if (yMax > MapData.MapHeight)
            {
                yMax = MapData.MapHeight;
            }

            var grid = GetMapGrid(xMin, xMax, yMin, yMax);
            var path = GetPath(grid, xStart - xMin, yStart - yMin, endX - xMin, endY - yMin);
            return path;
        }

        public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        Grid GetGroundDamageGrid(int frame)
        {
            if (GroundDamageLastUpdate < frame)
            {
                var gridSize = new GridSize(columns: MapData.MapWidth, rows: MapData.MapHeight);
                var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
                var traversalVelocity = Velocity.FromMetersPerSecond(1);
                GroundDamageGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
                for (var x = 0; x < MapData.MapWidth; x++)
                {
                    for (var y = 0; y < MapData.MapHeight; y++)
                    {
                        if (!MapData.Map[x,y].Walkable || InMineralLine(x, y))
                        {
                            GroundDamageGrid.DisconnectNode(new GridPosition(x, y));
                        }
                    }
                }
                GroundDamageLastUpdate = frame;
            }
            return GroundDamageGrid;
        }

        private bool InMineralLine(int x, int y)
        {
            var vector = new Vector2(x, y);
            foreach (var baseLocation in BaseData.BaseLocations)
            {
                if (Vector2.DistanceSquared(vector, baseLocation.Location.ToVector2()) < 64)
                {
                    foreach(var mineral in baseLocation.MineralFields)
                    {
                        if (Vector2.DistanceSquared(vector, mineral.Pos.ToVector2()) < 64)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        Grid GetMapGrid(int frame)
        {
            if (MapLastUpdate < frame)
            {
                var gridSize = new GridSize(columns: MapData.MapWidth, rows: MapData.MapHeight);
                var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
                var traversalVelocity = Velocity.FromMetersPerSecond(1);
                WalkGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
                for (var x = 0; x < MapData.MapWidth; x++)
                {
                    for (var y = 0; y < MapData.MapHeight; y++)
                    {
                        if (!MapData.Map[x,y].Walkable)
                        {
                            WalkGrid.DisconnectNode(new GridPosition(x, y));
                            continue;
                        }

                        var vector = new Vector2(x, y);
                        if (ActiveUnitData.NeutralUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.UnitTypeData.Name.Contains("Unbuildable") && Vector2.DistanceSquared(u.Position, vector) <= u.Unit.Radius * u.Unit.Radius))
                        {
                            WalkGrid.DisconnectNode(new GridPosition(x, y));
                        }
                    }
                }
                MapLastUpdate = frame;
            }
            return WalkGrid;
        }

        Grid GetMapGrid(int xMin, int xMax, int yMin, int yMax)
        {
            var gridSize = new GridSize(columns: xMax - xMin, rows: yMax - yMin);
            var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
            var traversalVelocity = Velocity.FromMetersPerSecond(1);
            WalkGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
            for (var x = xMin; x < xMax; x++)
            {
                for (var y = yMin; y < yMax; y++)
                {
                    if (!MapData.Map[x,y].Walkable)
                    {
                        WalkGrid.DisconnectNode(new GridPosition(x - xMin, y - yMin));
                        continue;
                    }

                    var vector = new Vector2(x, y);
                    if (ActiveUnitData.NeutralUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.UnitTypeData.Name.Contains("Unbuildable") && Vector2.DistanceSquared(u.Position, vector) <= u.Unit.Radius * u.Unit.Radius))
                    {
                        WalkGrid.DisconnectNode(new GridPosition(x - xMin, y - yMin));
                    }
                }
            }

            return WalkGrid;
        }

        private List<Vector2> GetPath(Grid grid, float startX, float startY, float endX, float endY)
        {
            return GetPath(grid, startX, startY, endX, endY, PathFinder);
        }

        private List<Vector2> GetPath(Grid grid, float startX, float startY, float endX, float endY, PathFinder pathFinder)
        {
            if (startX >= grid.GridSize.Columns)
            {
                startX = grid.GridSize.Columns - 1;
            }
            if (endX >= grid.GridSize.Columns)
            {
                endX = grid.GridSize.Columns - 1;
            }
            if (startY >= grid.GridSize.Rows)
            {
                startY = grid.GridSize.Rows - 1;
            }
            if (endY >= grid.GridSize.Rows)
            {
                endY = grid.GridSize.Rows - 1;
            }
            try
            {
                var path = pathFinder.FindPath(new GridPosition((int)startX, (int)startY), new GridPosition((int)endX, (int)endY), grid);
                return path.Edges.Select(e => new Vector2(e.End.Position.X, e.End.Position.Y)).ToList();
            }
            catch (Exception)
            {
                return new List<Vector2>();
            }
        }
    }
}
