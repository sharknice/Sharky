using Roy_T.AStar.Grids;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Pathing
{

    // find node within 20 range that is safe, closest to desired position
    // get the path to that node using A*
    public class SharkyNearPathFinder : IPathFinder
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

        public SharkyNearPathFinder(PathFinder pathFinder, MapData mapData, MapDataService mapDataService, DebugService debugService)
        {
            PathFinder = pathFinder;
            MapData = mapData;
            MapDataService = mapDataService;
            DebugService = debugService;

            GroundDamageLastUpdate = -1;
            GroundDetectionLastUpdate = -1;
            AirDamageLastUpdate = -1;
            MapLastUpdate = -1;
        }

        public List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var end = new Vector2(endX, endY);
            var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).ThenBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, float radius)
        {
            return GetGroundPath(startX, startY, endX, endY, frame);
        }

        public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        {
            var size = 40;
            var cells = MapDataService.GetCells(startX, startY, size / 2);
            var end = new Vector2(endX, endY);
            var best = cells.OrderBy(c => c.EnemyAirDpsInRange).ThenBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
            if (best != null)
            {
                var grid = GetAirDamageGrid((int)startX, (int)startY, size);
                var path = GetPath(grid, startX, startY, best.X, best.Y, size);
                return path;
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var best = cells.Where(c => c.Walkable).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        public List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            var cells = MapDataService.GetCells(startX, startY, 2);
            var end = new Vector2(endX, endY);
            var best = cells.Where(c => c.Walkable).OrderBy(c => c.InEnemyDetection).ThenBy(c => Vector2.DistanceSquared(end, new Vector2(c.X, c.Y))).FirstOrDefault();
            if (best != null)
            {
                return new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
            }
            return new List<Vector2>();
        }

        //public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame)
        //{
        //    var grid = GetMapGrid(frame);
        //    return GetPath(grid, startX, startY, endX, endY);
        //}

        //public List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame)
        //{
        //    var grid = GetGroundDamageGrid(frame);
        //    var path = GetPath(grid, startX, startY, endX, endY);
        //    if (path.Count() == 0)
        //    {
        //        var cells = MapDataService.GetCells(startX, startY, 1);
        //        var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).FirstOrDefault();
        //        if (best != null)
        //        {
        //            path = new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
        //        }
        //    }

        //    return path;
        //}

        //public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        //{
        //    var grid = GetAirDamageGrid(frame);
        //    var path = GetPath(grid, startX, startY, endX, endY);
        //    if (path.Count() == 0)
        //    {
        //        var cells = MapDataService.GetCells(startX, startY, 1);
        //        var best = cells.OrderBy(c => c.EnemyAirDpsInRange).FirstOrDefault();
        //        if (best != null)
        //        {
        //            path = new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
        //        }
        //    }

        //    return path;
        //}

        //public List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame)
        //{
        //    var grid = GetGroundDetectionGrid(frame);
        //    var path = GetPath(grid, startX, startY, endX, endY);
        //    if (path.Count() == 0)
        //    {
        //        var cells = MapDataService.GetCells(startX, startY, 1);
        //        var best = cells.Where(c => c.Walkable).OrderBy(c => c.EnemyGroundDpsInRange).FirstOrDefault();
        //        if (best != null)
        //        {
        //            path = new List<Vector2> { new Vector2(startX, startY), new Vector2(best.X, best.Y) };
        //        }
        //    }

        //    return path;
        //}

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
                        if (!MapData.Map[x][y].Walkable || MapData.Map[x][y].EnemyGroundDpsInRange > 0)
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
                        if (!MapData.Map[x][y].Walkable || MapData.Map[x][y].InEnemyDetection)
                        {
                            GroundDetectionGrid.DisconnectNode(new GridPosition(x, y));
                        }
                    }
                }
                GroundDetectionLastUpdate = frame;
            }
            return GroundDetectionGrid;
        }

        Grid GetAirDamageGrid(int xPos, int yPos, int size)
        {
            var gridSize = new GridSize(columns: size, rows: size);
            var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
            var traversalVelocity = Velocity.FromMetersPerSecond(1);
            AirDamageGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
            var halfSize = size / 2;
            var startX = xPos - halfSize;
            var startY = yPos - halfSize;
            for (var x = startX; x < xPos + halfSize; x++)
            {
                for (var y = startY; y < yPos + halfSize; y++)
                {
                    if (x < 0 || y < 0 || x >= MapData.MapWidth || y >= MapData.MapHeight || MapData.Map[x][y].EnemyAirDpsInRange > 0)
                    {
                        if (x != xPos && y != yPos)
                        {
                            var translatedX = x - startX;
                            var translatedY = y - startY;
                            AirDamageGrid.DisconnectNode(new GridPosition(translatedX, translatedY));
                        }
                    }
                }
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
                        if (!MapData.Map[x][y].Walkable)
                        {
                            WalkGrid.DisconnectNode(new GridPosition(x, y));
                            //DebugService.DrawSphere(new Point { X = x, Y = y, Z = MapData.Map[x][y].TerrainHeight + 1 }, 2, new Color { R = 0, G = 255, B = 0 });
                        }
                        else
                        {
                            //DebugService.DrawSphere(new Point { X = x, Y = y, Z = MapData.Map[x][y].TerrainHeight + 1 }, 2, new Color { R = 255, G = 0, B = 0 });
                        }
                    }
                }
            }
            return WalkGrid;
        }

        private List<Vector2> GetPath(Grid grid, float startX, float startY, float endX, float endY, int size)
        {
            var translatedStartX = (int)startX - (size / 2);
            var translatedStartY = (int)startY - (size / 2);

            try
            {
                var startPosition = new GridPosition((int)startX - translatedStartX, (int)startY - translatedStartY);
                var translatedEndX = (int)endX - translatedStartX;
                if (translatedEndX > size - 1)
                {
                    translatedEndX = size - 1;
                }
                var translatedEndY = (int)endY - translatedStartY;
                if (translatedEndY > size - 1)
                {
                    translatedEndY = size - 1;
                }
                var endPosition = new GridPosition(translatedEndX, translatedEndY);
                var path = PathFinder.FindPath(startPosition, endPosition, grid);
                return path.Edges.Select(e => new Vector2(e.End.Position.X + translatedStartX, e.End.Position.Y + translatedStartY)).ToList();
            }
            catch (Exception)
            {
                return new List<Vector2>();
            }
        }
    }
}
