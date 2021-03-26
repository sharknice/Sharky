using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Sharky.Pathing
{
    public class MapDataService
    {
        public MapData MapData { get; set; }

        public MapDataService(MapData mapData)
        {
            MapData = mapData;
        }

        public List<MapCell> GetCells(float x, float y, float radius)
        {
            var cells = new List<MapCell>();

            var xMin = (int)Math.Floor(x - radius);
            var xMax = (int)Math.Ceiling(x + radius);
            int yMin = (int)Math.Floor(y - radius);
            int yMax = (int)Math.Ceiling(y + radius);

            if (xMin < 0)
            {
                xMin = 0;
            }
            if (xMax >= MapData.MapWidth)
            {
                xMax = MapData.MapWidth - 1;
            }
            if (yMin < 0)
            {
                yMin = 0;
            }
            if (yMax >= MapData.MapHeight)
            {
                yMax = MapData.MapHeight - 1;
            }

            for (var currentX = xMin; currentX <= xMax; currentX++)
            {
                for (var currentY = yMin; currentY <= yMax; currentY++)
                {
                    cells.Add(MapData.Map[currentX][currentY]);
                }
            }

            return cells;
        }

        public bool PathWalkable(float startX, float startY, float endX, float endY)
        {
            if (endX < 0 || endY < 0 || endX >= MapData.MapWidth || endY >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)endX][(int)endY].Walkable;
        }

        public bool PathWalkable(Point start, Point2D end)
        {
            return PathWalkable(start.X, start.Y, end.X, end.Y);
        }

        public bool PathWalkable(Point2D point)
        {
            return PathWalkable(point.X, point.Y, point.X, point.Y);
        }

        public bool PathWalkable(Point point)
        {
            return PathWalkable(point.X, point.Y, point.X, point.Y);
        }

        public bool PathWalkable(int x, int y)
        {
            return PathWalkable(x, y, x, y);
        }

        public bool PathFlyable(float startX, float startY, float endX, float endY)
        {
            if (endX < 0 || endY < 0 || endX >= MapData.MapWidth || endY >= MapData.MapHeight)
            {
                return false;
            }
            return true;
        }

        public bool PathFlyable(Point start, Point2D end)
        {
            return PathFlyable(start.X, start.Y, end.X, end.Y);
        }

        public bool SelfVisible(Point2D point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].InSelfVision;
        }

        public bool SelfVisible(Point point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].InSelfVision;
        }

        public int Visibility(Point2D point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].Visibility;
        }

        public int LastFrameVisibility(Point2D point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].LastFrameVisibility;
        }

        public bool InEnemyDetection(Point point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].InEnemyDetection;
        }

        public bool InEnemyDetection(Point2D point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].InEnemyDetection;
        }

        public int MapHeight(Point point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].TerrainHeight;
        }

        public int MapHeight(Point2D point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].TerrainHeight;
        }

        public int MapHeight(int x, int y)
        {
            return MapData.Map[x][y].TerrainHeight;
        }

        public float EnemyAirDpsInRange(Point point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].EnemyAirDpsInRange;
        }

        public int LastFrameAlliesTouched(Point2D point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].LastFrameAlliesTouched;
        }

        internal bool InEnemyVision(Point point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].InEnemyVision;
        }
    }
}
