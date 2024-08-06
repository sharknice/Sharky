﻿namespace Sharky.Pathing
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
                    cells.Add(MapData.Map[currentX,currentY]);
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
            return MapData.Map[(int)endX,(int)endY].Walkable;
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

        public bool PathWalkable(Vector2 point)
        {
            return PathWalkable(point.X, point.Y, point.X, point.Y);
        }

        public bool PathWalkable(int x, int y)
        {
            return PathWalkable(x, y, x, y);
        }

        public bool PathWalkable(float x, float y)
        {
            return PathWalkable(x, y, x, y);
        }

        public bool PathWalkable(Point2D point, int radius)
        {
            return PathWalkable(point.X, point.Y) &&
                PathWalkable(point.X - radius, point.Y) &&
                PathWalkable(point.X + radius, point.Y) &&
                PathWalkable(point.X, point.Y - radius) &&
                PathWalkable(point.X, point.Y + radius) &&
                PathWalkable(point.X + radius, point.Y + radius) &&
                PathWalkable(point.X - radius, point.Y - radius);
        }

        public bool PathWalkable(Point point, int radius)
        {
            return PathWalkable(point.X, point.Y) &&
                PathWalkable(point.X - radius, point.Y) &&
                PathWalkable(point.X + radius, point.Y) &&
                PathWalkable(point.X, point.Y - radius) &&
                PathWalkable(point.X, point.Y + radius) &&
                PathWalkable(point.X + radius, point.Y + radius) &&
                PathWalkable(point.X - radius, point.Y - radius);
        }

        public bool AnyPathWalkable(Point point, int radius)
        {
            return PathWalkable(point.X, point.Y) ||
                PathWalkable(point.X - radius, point.Y) ||
                PathWalkable(point.X + radius, point.Y) ||
                PathWalkable(point.X, point.Y - radius) ||
                PathWalkable(point.X, point.Y + radius) ||
                PathWalkable(point.X + radius, point.Y + radius) ||
                PathWalkable(point.X - radius, point.Y - radius);
        }

        public bool AnyPathWalkable(Point2D point, int radius)
        {
            return PathWalkable(point.X, point.Y) ||
                PathWalkable(point.X - radius, point.Y) ||
                PathWalkable(point.X + radius, point.Y) ||
                PathWalkable(point.X, point.Y - radius) ||
                PathWalkable(point.X, point.Y + radius) ||
                PathWalkable(point.X + radius, point.Y + radius) ||
                PathWalkable(point.X - radius, point.Y - radius);
        }

        public bool SameHeight(Point2D point, int radius)
        {
            var height = MapHeight(point);
            return
                height == MapHeight(point.X - radius, point.Y) &&
                height == MapHeight(point.X + radius, point.Y) &&
                height == MapHeight(point.X, point.Y - radius) &&
                height == MapHeight(point.X, point.Y + radius) &&
                height == MapHeight(point.X + radius, point.Y + radius) &&
                height == MapHeight(point.X - radius, point.Y - radius);
        }

        public bool PathFlyable(float startX, float startY, float endX, float endY)
        {
            if (endX < 0 || endY < 0 || endX >= MapData.MapWidth || endY >= MapData.MapHeight)
            {
                return false;
            }
            return true;
        }

        public bool PathFlyable(Vector2 end)
        {
            return PathFlyable(end.X, end.Y, end.X, end.Y);
        }

        public bool PathFlyable(Point2D end)
        {
            return PathFlyable(end.X, end.Y, end.X, end.Y);
        }

        public bool PathFlyable(Point start, Point2D end)
        {
            return PathFlyable(start.X, start.Y, end.X, end.Y);
        }

        public bool PathBuildable(Point2D point)
        {
            return PathBuildable(point.X, point.Y, point.X, point.Y);
        }

        public bool PathBuildable(Vector2 point)
        {
            return PathBuildable(point.X, point.Y, point.X, point.Y);
        }

        public bool PathBuildable(float startX, float startY, float endX, float endY)
        {
            if (endX < 0 || endY < 0 || endX >= MapData.MapWidth || endY >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)endX,(int)endY].Buildable;
        }

        public bool SelfVisible(Vector2 point, float radius)
        {
            return SelfVisible(point.X, point.Y) && SelfVisible(point.X - radius, point.Y - radius) && SelfVisible(point.X + radius, point.Y + radius) && SelfVisible(point.X - radius, point.Y + radius) && SelfVisible(point.X + radius, point.Y - radius);
        }

        public bool SelfVisible(float x, float y)
        {
            if (x < 0 || y < 0 || x >= MapData.MapWidth || y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)x,(int)y].InSelfVision;
        }

        public bool SelfVisible(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InSelfVision;
        }

        public bool SelfVisible(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InSelfVision;
        }

        public int Visibility(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].Visibility;
        }

        public int LastFrameVisibility(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].LastFrameVisibility;
        }

        public bool InEnemyDetection(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InEnemyDetection;
        }

        public bool InEnemyDetection(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InEnemyDetection;
        }

        public bool InSelfDetection(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InSelfDetection;
        }

        public bool InSelfDetection(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InSelfDetection;
        }

        public int MapHeight(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].TerrainHeight;
        }

        public int MapHeight(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].TerrainHeight;
        }

        public int MapHeight(Vector2 point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].TerrainHeight;
        }

        public int MapHeight(int x, int y)
        {
            if (x < 0 || y < 0 || x >= MapData.MapWidth || y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[x,y].TerrainHeight;
        }

        public int MapHeight(float x, float y)
        {
            return MapHeight((int)x, (int)y);
        }

        public bool IsOnCreep(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X, (int)point.Y].HasCreep;
        }

        public bool IsOnCreep(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].HasCreep;
        }

        public bool PathBlocked(Vector2 point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return true;
            }
            return MapData.Map[(int)point.X, (int)point.Y].PathBlocked;
        }

        public float EnemyAirDpsInRange(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].EnemyAirDpsInRange;
        }

        public float EnemyAirDpsInRange(Vector2 point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X, (int)point.Y].EnemyAirDpsInRange;
        }

        public float EnemyAirDpsInRange(float x, float y)
        {
            return EnemyAirDpsInRange(new Point2D { X = x, Y = y });
        }

        public float EnemyAirDpsInRange(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X, (int)point.Y].EnemyAirDpsInRange;
        }

        public float EnemyGroundSplashDpsInRange(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].EnemyGroundSplashDpsInRange;
        }

        public float EnemyGroundSplashDpsInRange(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X, (int)point.Y].EnemyGroundSplashDpsInRange;
        }

        public float EnemyGroundDpsInRange(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].EnemyGroundDpsInRange;
        }

        public int LastFrameAlliesTouched(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return 0;
            }
            return MapData.Map[(int)point.X,(int)point.Y].LastFrameAlliesTouched;
        }

        public bool InEnemyVision(Point point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InEnemyVision;
        }

        public bool InEnemyVision(Point2D point)
        {
            if (point == null || point.X < 0 || point.Y < 0 || point.X >= MapData.MapWidth || point.Y >= MapData.MapHeight)
            {
                return false;
            }
            return MapData.Map[(int)point.X,(int)point.Y].InEnemyVision;
        }

        public bool EnemyAirDamageInRange(Point2D point, float radius)
        {
            return
                EnemyAirDpsInRange(point) > 0 ||
                EnemyAirDpsInRange(point.X - radius, point.Y) > 0 ||
                EnemyAirDpsInRange(point.X + radius, point.Y) > 0 ||
                EnemyAirDpsInRange(point.X, point.Y - radius) > 0 ||
                EnemyAirDpsInRange(point.X, point.Y + radius) > 0 ||
                EnemyAirDpsInRange(point.X + radius, point.Y + radius) > 0 ||
                EnemyAirDpsInRange(point.X - radius, point.Y - radius) > 0;
        }

        public bool EnemyAirDamageInRange(Vector2 point, float radius)
        {
            return
                EnemyAirDpsInRange(point) > 0 ||
                EnemyAirDpsInRange(point.X - radius, point.Y) > 0 ||
                EnemyAirDpsInRange(point.X + radius, point.Y) > 0 ||
                EnemyAirDpsInRange(point.X, point.Y - radius) > 0 ||
                EnemyAirDpsInRange(point.X, point.Y + radius) > 0 ||
                EnemyAirDpsInRange(point.X + radius, point.Y + radius) > 0 ||
                EnemyAirDpsInRange(point.X - radius, point.Y - radius) > 0;
        }
    }
}
