using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Sharky.Pathing
{
    public class MapDataService
    {
        MapData MapData;

        public MapDataService(MapData mapData)
        {
            MapData = mapData;
        }

        public List<MapCell> GetCells(float x, float y, float radius)
        {
            return new List<MapCell>();
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

        public int MapHeight(Point point)
        {
            return MapData.Map[(int)point.X][(int)point.Y].TerrainHeight;
        }
    }
}
