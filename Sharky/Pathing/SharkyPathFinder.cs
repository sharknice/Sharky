namespace Sharky.Pathing
{
    public class SharkyPathFinder : IPathFinder
    {
        Grid GroundDamageGrid; // TDOO: include buildings for ground grids, can't walk through them
        int GroundDamageLastUpdate; 
        Grid GroundDetectionGrid;
        int GroundDetectionLastUpdate;
        int AirDamageLastUpdate;
        int MapLastUpdate;

        Grid WalkGrid;
        Grid BuildingGrid;
        Grid AirDamageGrid;
        Grid EnemyVisionGrid;
        Grid EnemyVisionGroundGrid;
        Grid EnemyDetectionGrid;
        

        PathFinder PathFinder;
        MapData MapData;
        MapDataService MapDataService;
        DebugService DebugService;
        ActiveUnitData ActiveUnitData;

        public SharkyPathFinder(PathFinder pathFinder, MapData mapData, MapDataService mapDataService, DebugService debugService, ActiveUnitData activeUnitData)
        {
            PathFinder = pathFinder;
            MapData = mapData;
            MapDataService = mapDataService;
            DebugService = debugService;
            ActiveUnitData = activeUnitData;

            GroundDamageLastUpdate = -1;
            GroundDetectionLastUpdate = -1;
            AirDamageLastUpdate = -1;
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
            if (grid == null) { return new List<Vector2>(); }
            var path = GetPath(grid, startX, startY, endX, endY);
            if (!path.Any())
            {
                var cells = MapDataService.GetCells(startX, startY, 1);
                var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).FirstOrDefault();
                if (best != null)
                {
                    path = new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
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
            if (Vector2.DistanceSquared(new Vector2(startX, startY), new Vector2(endX, endY)) < 4)
            {
                return GetSafeAirFallback(startX, startY, endX, endY);
            }
            var grid = GetAirDamageGrid(frame);
            var path = GetPath(grid, startX, startY, endX, endY);
            if (!path.Any())
            {
                path = GetSafeAirFallback(startX, startY, endX, endY);
            }

            return path;
        }

        private List<Vector2> GetSafeAirFallback(float startX, float startY, float endX, float endY)
        {
            var cells = MapDataService.GetCells(startX, startY, 1);
            var best = cells.OrderBy(c => c.EnemyAirDpsInRange).ThenBy(c => Vector2.DistanceSquared(new Vector2(c.X, c.Y), new Vector2(endX, endY))).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }

            return new List<Vector2> { new Vector2(startX, startY), new Vector2(endX, endY) };
        }

        public List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var grid = GetGroundDetectionGrid(frame);
            var path = GetPath(grid, startX, startY, endX, endY);
            if (!path.Any())
            {
                var cells = MapDataService.GetCells(startX, startY, 1);
                var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).FirstOrDefault();
                if (best != null)
                {
                    path = new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
                }
            }

            return path;
        }

        Grid GetGroundDamageGrid(int frame)
        {
            if (GroundDamageLastUpdate < frame)
            {
                var gridSize = new GridSize(columns: MapData.MapWidth, rows: MapData.MapHeight);
                var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
                var traversalVelocity = Velocity.FromMetersPerSecond(1);
                GroundDamageGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
                if (GroundDamageGrid == null) { return null; }
                for (var x = 0; x < MapData.MapWidth; x++)
                {
                    for (var y = 0; y < MapData.MapHeight; y++)
                    {
                        if (!MapData.Map[x,y].Walkable || MapData.Map[x,y].EnemyGroundDpsInRange > 0)
                        {
                            GroundDamageGrid.DisconnectNode(new GridPosition(x, y));
                        }
                    }
                }
                GroundDamageLastUpdate = frame;
            }
            return GroundDamageGrid;
        }

        Grid GetGroundDetectionGrid(int frame)
        {
            if (GroundDetectionLastUpdate < frame)
            {
                var gridSize = new GridSize(columns: MapData.MapWidth, rows: MapData.MapHeight);
                var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
                var traversalVelocity = Velocity.FromMetersPerSecond(1);
                GroundDetectionGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
                for (var x = 0; x < MapData.MapWidth; x++)
                {
                    for (var y = 0; y < MapData.MapHeight; y++)
                    {
                        if (!MapData.Map[x,y].Walkable || MapData.Map[x,y].InEnemyDetection)
                        {
                            GroundDetectionGrid.DisconnectNode(new GridPosition(x, y));
                        }
                    }
                }
                GroundDetectionLastUpdate = frame;
            }
            return GroundDetectionGrid;
        }

        Grid GetAirDamageGrid(int frame)
        {
            if (AirDamageLastUpdate < frame)
            {
                var gridSize = new GridSize(columns: MapData.MapWidth, rows: MapData.MapHeight);
                var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
                var traversalVelocity = Velocity.FromMetersPerSecond(1);
                AirDamageGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
                for (var x = 0; x < MapData.MapWidth; x++)
                {
                    for (var y = 0; y < MapData.MapHeight; y++)
                    {
                        if (MapData.Map[x,y].EnemyAirDpsInRange > 0)
                        {
                            AirDamageGrid.DisconnectNode(new GridPosition(x, y));
                        }
                    }
                }
                AirDamageLastUpdate = frame;
            }
            return AirDamageGrid;
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

        //Grid GetMapGrid(float xStart, float yStart, float range)
        //{
        //    var start = Stopwatch.StartNew();

        //    var gridSize = new GridSize(columns: MapData.MapWidth, rows: MapData.MapHeight);
        //    var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
        //    var traversalVelocity = Velocity.FromMetersPerSecond(1);
        //    WalkGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
        //    for (var x = 0; x < MapData.MapWidth; x++)
        //    {
        //        for (var y = 0; y < MapData.MapHeight; y++)
        //        {
        //            if (x < xStart - range || y < yStart - range || x > xStart + range || y > yStart + range)
        //            {
        //                WalkGrid.DisconnectNode(new GridPosition(x, y));
        //                continue;
        //            }

        //            if (!MapData.Map[x][y].Walkable)
        //            {
        //                WalkGrid.DisconnectNode(new GridPosition(x, y));
        //                continue;
        //            }

        //            var vector = new Vector2(x, y);
        //            if (ActiveUnitData.NeutralUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.UnitTypeData.Name.Contains("Unbuildable") && Vector2.DistanceSquared(u.Position, vector) <= u.Unit.Radius * u.Unit.Radius))
        //            {
        //                WalkGrid.DisconnectNode(new GridPosition(x, y));
        //            }
        //        }
        //    }

        //    Console.WriteLine($"GetMapGrid: {start.ElapsedMilliseconds}");

        //    return WalkGrid;
        //}

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

        void UpdateBuildingGrid(IEnumerable<UnitCalculation> buildings, IEnumerable<Unit> resourceUnits)
        {
            // TODO: store the old buildings, if the buildings are the same don't update, just return
            BuildingGrid = WalkGrid;
            foreach (var building in buildings)
            {
                var nodes = GetNodesInRange(building.Unit.Pos, building.Unit.Radius, BuildingGrid.Columns, BuildingGrid.Rows);
                foreach (var node in nodes)
                {
                    BuildingGrid.DisconnectNode(node);
                }
            }
            foreach (var resource in resourceUnits)
            {
                var nodes = GetNodesInRange(resource.Pos, resource.Radius, BuildingGrid.Columns, BuildingGrid.Rows);
                foreach (var node in nodes)
                {
                    BuildingGrid.DisconnectNode(node);
                }
            }
        }

        void UpdateEnemyVisionGrid(IEnumerable<UnitCalculation> enemyUnits)
        {
            EnemyVisionGrid = Grid.CreateGridWithLateralAndDiagonalConnections(WalkGrid.GridSize, new Size(Distance.FromMeters(1), Distance.FromMeters(1)), Velocity.FromMetersPerSecond(1));
            foreach (var enemy in enemyUnits)
            {
                var nodes = GetNodesInRange(enemy.Unit.Pos, 11, EnemyVisionGrid.Columns, EnemyVisionGrid.Rows); // TODO: get sight range of every unit, // TODO: units on low ground can't see high ground
                foreach (var node in nodes)
                {
                    EnemyVisionGrid.DisconnectNode(node);
                }
            }
        }

        void UpdateEnemyVisionGroundGrid(IEnumerable<UnitCalculation> enemyUnits)
        {
            EnemyVisionGroundGrid = BuildingGrid;
            foreach (var enemy in enemyUnits)
            {
                var nodes = GetNodesInRange(enemy.Unit.Pos, 11, EnemyVisionGroundGrid.Columns, EnemyVisionGroundGrid.Rows); // TODO: get sight range of every unit // TODO: units on low ground can't see high ground
                foreach (var node in nodes)
                {
                    EnemyVisionGroundGrid.DisconnectNode(node);
                }
            }
        }

        private List<GridPosition> GetNodesInRange(Point position, float range, int columns, int rows)
        {
            var nodes = new List<GridPosition>();
            //nodes.Add(new GridPosition((int)position.X, (int)position.Y));
            var xMin = (int)Math.Floor(position.X - range);
            var xMax = (int)Math.Ceiling(position.X + range);
            int yMin = (int)Math.Floor(position.Y - range);
            int yMax = (int)Math.Ceiling(position.Y + range);

            if (xMin < 0)
            {
                xMin = 0;
            }
            if (xMax >= columns)
            {
                xMax = columns - 1;
            }
            if (yMin < 0)
            {
                yMin = 0;
            }
            if (yMax >= rows)
            {
                yMax = rows - 1;
            }

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    nodes.Add(new GridPosition(x, y));
                }
            }

            return nodes;
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

        private List<Vector2> GetPath(Grid grid, float startX, float startY, float endX, float endY, float range)
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
                var path = PathFinder.FindPath(new GridPosition((int)startX, (int)startY), new GridPosition((int)endX, (int)endY), grid, Velocity.FromMetersPerSecond(range));
                return path.Edges.Select(e => new Vector2(e.End.Position.X, e.End.Position.Y)).ToList();
            }
            catch (Exception)
            {
                return new List<Vector2>();
            }
        }

        List<Vector2> GetHiddenAirPath(float startX, float startY, float endX, float endY)
        {
            return GetPath(EnemyVisionGrid, startX, startY, endX, endY);
        }

        List<Vector2> GetHiddenGroundPath(float startX, float startY, float endX, float endY)
        {
            return GetPath(EnemyVisionGroundGrid, startX, startY, endX, endY);
        }
    }
}
