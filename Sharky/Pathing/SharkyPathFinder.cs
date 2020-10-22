using Roy_T.AStar.Grids;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;
using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Pathing
{
    public class SharkyPathFinder
    {
        private Grid MapGrid;
        private Grid BuildingGrid;
        private Grid GroundDamageGrid;
        private Grid AirDamageGrid;
        private Grid EnemyVisionGrid;
        private Grid EnemyVisionGroundGrid;
        private Grid EnemyDetectionGrid;
        private Grid EnemyDetectionGroundGrid; // includes ground and buildings, can't walk through them

        private PathFinder PathFinder;

        public SharkyPathFinder(PathFinder pathFinder)
        {
            PathFinder = pathFinder;
        }

        public void CreateMapGrid(ImageData pathingGrid)
        {
            var gridSize = new GridSize(columns: pathingGrid.Size.X, rows: pathingGrid.Size.Y);
            var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
            var traversalVelocity = Velocity.FromMetersPerSecond(1);
            MapGrid = Grid.CreateGridWithLateralAndDiagonalConnections(gridSize, cellSize, traversalVelocity);
            for (var x = 0; x < pathingGrid.Size.X; x++)
            {
                for (var y = 0; y < pathingGrid.Size.Y; y++)
                {
                    if (!GetDataValueBit(pathingGrid, x, y))
                    {
                        MapGrid.DisconnectNode(new GridPosition(x, y));
                    }
                }
            }
        }

        public void UpdateBuildingGrid(IEnumerable<UnitCalculation> buildings, IEnumerable<Unit> resourceUnits)
        {
            // TODO: store the old buildings, if the buildings are the same don't update, just return
            BuildingGrid = MapGrid;
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

        public void UpdateGroundDamageGrid(IEnumerable<UnitCalculation> enemyUnits)
        {
            GroundDamageGrid = BuildingGrid;
            foreach (var enemy in enemyUnits)
            {
                if (enemy.DamageGround)
                {
                    var nodes = GetNodesInRange(enemy.Unit.Pos, enemy.Range + 2, GroundDamageGrid.Columns, GroundDamageGrid.Rows);
                    foreach (var node in nodes)
                    {
                        GroundDamageGrid.DisconnectNode(node);
                    }
                }
            }
        }

        public void UpdateAirDamageGrid(IEnumerable<UnitCalculation> enemyUnits)
        {
            AirDamageGrid = Grid.CreateGridWithLateralAndDiagonalConnections(MapGrid.GridSize, new Size(Distance.FromMeters(1), Distance.FromMeters(1)), Velocity.FromMetersPerSecond(1));
            foreach (var enemy in enemyUnits)
            {
                if (enemy.DamageAir)
                {
                    var nodes = GetNodesInRange(enemy.Unit.Pos, enemy.Range + 2, AirDamageGrid.Columns, AirDamageGrid.Rows);
                    foreach (var node in nodes)
                    {
                        AirDamageGrid.DisconnectNode(node);
                    }
                }
            }
        }

        public void UpdateEnemyVisionGrid(IEnumerable<UnitCalculation> enemyUnits)
        {
            EnemyVisionGrid = Grid.CreateGridWithLateralAndDiagonalConnections(MapGrid.GridSize, new Size(Distance.FromMeters(1), Distance.FromMeters(1)), Velocity.FromMetersPerSecond(1));
            foreach (var enemy in enemyUnits)
            {
                var nodes = GetNodesInRange(enemy.Unit.Pos, 11, EnemyVisionGrid.Columns, EnemyVisionGrid.Rows); // TODO: get sight range of every unit, // TODO: units on low ground can't see high ground
                foreach (var node in nodes)
                {
                    EnemyVisionGrid.DisconnectNode(node);
                }
            }
        }

        public void UpdateEnemyVisionGroundGrid(IEnumerable<UnitCalculation> enemyUnits)
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

        private IEnumerable<Vector2> GetPath(Grid grid, float startX, float startY, float endX, float endY)
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
                var path = PathFinder.FindPath(new GridPosition((int)startX, (int)startY), new GridPosition((int)endX, (int)endY), grid);
                return path.Edges.Reverse().Select(e => new Vector2(e.End.Position.X, e.End.Position.Y));
            }
            catch (Exception e)
            {
                return new List<Vector2>();
            }
        }

        public IEnumerable<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY)
        {
            return GetPath(AirDamageGrid, startX, startY, endX, endY);
        }

        public IEnumerable<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY)
        {
            return GetPath(GroundDamageGrid, startX, startY, endX, endY);
        }

        public IEnumerable<Vector2> GetHiddenAirPath(float startX, float startY, float endX, float endY)
        {
            return GetPath(EnemyVisionGrid, startX, startY, endX, endY);
        }

        public IEnumerable<Vector2> GetHiddenGroundPath(float startX, float startY, float endX, float endY)
        {
            return GetPath(EnemyVisionGroundGrid, startX, startY, endX, endY);
        }

        private bool GetDataValueBit(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;
            return ((data.Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? false : true;
        }
    }
}
