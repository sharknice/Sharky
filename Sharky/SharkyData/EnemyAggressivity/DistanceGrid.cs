using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sharky
{
    /// <summary>
    /// Grid with distances to enemy and self resource center
    /// </summary>
    public class DistanceGrid
    {
        private int[,] SelfGroundDistance;
        private int[,] EnemyGroundDistance;

        private int[,] SelfAirDistance;
        private int[,] EnemyAirDistance;

        private ActiveUnitData ActiveUnitData;
        private MapData MapData;
        private BaseData BaseData;
        SharkyOptions SharkyOptions;

        private int Width;
        private int Height;

        private int lastUpdate = -1000;

        private struct Point
        {
            public readonly int X;
            public readonly int Y;

            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>
        /// Gets distance to nearest self or enemy resource center on ground / air.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="self"></param>
        /// <param name="ground"></param>
        /// <returns></returns>
        public float GetDist(float x, float y, bool self, bool ground = true)
        {
            return GetDist((int)(x + 0.5f), (int)(y + 0.5f), self, ground);
        }

        /// <summary>
        /// Gets distance to nearest self or enemy resource center on ground / air.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="self"></param>
        /// <param name="ground"></param>
        /// <returns></returns>
        public int GetDist(int x, int y, bool self, bool ground = true)
        {
            // Clamp to map - not sure if pos can be out of map
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= Width) x = Width - 1;
            if (y >= Height) y = Height - 1;

            if (self)
            {
                if (ground)
                    return SelfGroundDistance[x, y];
                else
                    return SelfAirDistance[x, y];
            }
            else
            {
                if (ground)
                    return EnemyGroundDistance[x, y];
                else
                    return EnemyAirDistance[x, y];
            }
        }

        public DistanceGrid(DefaultSharkyBot defaultSharkyBot)
        {
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            MapData = defaultSharkyBot.MapData;
            BaseData = defaultSharkyBot.BaseData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
        }

        private void Init()
        {
            // Late init, we have to wait for MapData to be initialized and we want to init only once.
            if (MapData.MapWidth == 0 || Width != 0)
            {
                return;
            }

            Width = MapData.MapWidth;
            Height = MapData.MapHeight;

            SelfGroundDistance = new int[Width, Height];
            EnemyGroundDistance = new int[Width, Height];
            SelfAirDistance = new int[Width, Height];
            EnemyAirDistance = new int[Width, Height];
        }

        public void Update(int frame)
        {
            Init();

            if (frame - lastUpdate < SharkyOptions.FramesPerSecond * 5)
            {
                return;
            }

            CalcDistances(GetResourceCenterPositions(ActiveUnitData.SelfUnits), SelfGroundDistance);
            CalcDistances(GetResourceCenterPositions(ActiveUnitData.EnemyUnits), EnemyGroundDistance);

            CalcDistances(GetResourceCenterPositions(ActiveUnitData.SelfUnits), SelfAirDistance, false);
            CalcDistances(GetResourceCenterPositions(ActiveUnitData.EnemyUnits), EnemyAirDistance, false);

            lastUpdate = frame;
        }

        private IEnumerable<Point> GetResourceCenterPositions(ConcurrentDictionary<ulong, UnitCalculation> units)
        {
            // Use resource centers if we can
            var centerPositions = units.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter)).Select(u => u.Unit.Pos);
            
            // Use remaining buildings if no resource centers
            if (!centerPositions.Any())
            {
                centerPositions = units.Values.Where(u => u.UnitTypeData.Attributes.Contains(SC2APIProtocol.Attribute.Structure)).Select(u => u.Unit.Pos);
            }

            // Use enemy natural if we do not see any of his buildings yet
            if (!centerPositions.Any())
            {
                centerPositions = new List<SC2APIProtocol.Point>() { new SC2APIProtocol.Point() { X = BaseData.EnemyNaturalBase.Location.X, Y = BaseData.EnemyNaturalBase.Location.Y } };
            }

            return centerPositions.Select(p => new Point((int)(p.X + 0.5f), (int)(p.Y + 0.5f)));
        }

        private void CalcDistances(IEnumerable<Point> points, int[,] distances, bool groundOnly = true)
        {
            Queue<Point> openSet = new();

            for (int x=0; x<Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    distances[x, y] = -1;
                }

            foreach (var p in points)
            {
                SafeOpenSetAdd(openSet, p.X, p.Y, 0, distances, groundOnly);
            }

            while (openSet.Count > 0)
            {
                var p = openSet.Dequeue();
                var dist = distances[p.X, p.Y] + 1;
                SafeOpenSetAdd(openSet, p.X+1, p.Y, dist, distances, groundOnly);
                SafeOpenSetAdd(openSet, p.X, p.Y+1, dist, distances, groundOnly);
                SafeOpenSetAdd(openSet, p.X-1, p.Y, dist, distances, groundOnly);
                SafeOpenSetAdd(openSet, p.X, p.Y-1, dist, distances, groundOnly);
            }
        }

        private void SafeOpenSetAdd(Queue<Point> openSet, int x, int y, int distance, int[,] distances, bool groundOnly)
        {
            // Skip out of map
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;

            // Skip already processed
            if (distances[x, y] != -1)
                return;

            // Skip unwalkable
            if (!groundOnly && !MapData.Map[x][y].Walkable)
                return;

            distances[x, y] = distance;
            openSet.Enqueue(new Point(x,y));
        }
    }
}
