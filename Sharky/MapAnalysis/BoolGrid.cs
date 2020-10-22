using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Sharky.MapAnalysis
{
    public abstract class BoolGrid
    {
        private bool inverted = false;
        internal abstract bool GetInternal(Point2D pos);
        public bool Get(Point2D pos)
        {
            if (pos.X < 0 || pos.Y < 0 || pos.X >= Width() || pos.Y >= Height())
                return false;
            return GetInternal(pos) == (!inverted);
        }

        public BoolGrid Invert()
        {
            BoolGrid result = Clone();
            result.inverted = true;
            return result;
        }

        public abstract BoolGrid Clone();
        public abstract int Width();
        public abstract int Height();

        public bool this[Point2D pos]
        {
            get { return Get(pos); }
        }

        public bool this[int x, int y]
        {
            get { return Get(SC2Util.Point(x, y)); }
        }

        public BoolGrid GetConnected(Point2D point)
        {
            return GetConnected(point, new ArrayBoolGrid(Width(), Height()));
        }

        public BoolGrid GetConnected(Point2D point, ArrayBoolGrid encountered)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            Queue<Point2D> q = new Queue<Point2D>();
            q.Enqueue(point);
            while (q.Count > 0)
            {
                Point2D cur = q.Dequeue();
                if (cur.X < 0 || cur.Y < 0 || cur.X >= Width() || cur.Y >= Height())
                    continue;
                if (Get(cur) && !encountered[cur])
                {
                    result[cur] = true;
                    encountered[cur] = true;
                    q.Enqueue(SC2Util.Point(cur.X + 1, cur.Y));
                    q.Enqueue(SC2Util.Point(cur.X - 1, cur.Y));
                    q.Enqueue(SC2Util.Point(cur.X, cur.Y + 1));
                    q.Enqueue(SC2Util.Point(cur.X, cur.Y - 1));
                }
            }
            return result;
        }

        public BoolGrid GetAdjacent(BoolGrid other)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y] && (other[x + 1, y] || other[x - 1, y] || other[x, y + 1] || other[x, y - 1]);

            return result;
        }

        public BoolGrid GetAnd(BoolGrid other)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y] && other[x, y];

            return result;
        }

        public BoolGrid GetOr(BoolGrid other)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    result[x, y] = this[x, y] || other[x, y];

            return result;
        }

        public int Count()
        {
            int result = 0;
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    if (this[x, y])
                        result++;
            return result;
        }

        public BoolGrid GetConnected(BoolGrid connectedTo, int steps)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            Queue<Point2D> q1 = new Queue<Point2D>();
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    if (connectedTo[x, y])
                        q1.Enqueue(SC2Util.Point(x, y));

            Queue<Point2D> q2 = new Queue<Point2D>();
            for (int i = 0; i < steps; i++)
            {
                while (q1.Count > 0)
                {
                    Point2D cur = q1.Dequeue();
                    if (cur.X < 0 || cur.Y < 0 || cur.X >= Width() || cur.Y >= Height())
                        continue;
                    if (Get(cur) && !result[cur])
                    {
                        result[cur] = true;
                        q2.Enqueue(SC2Util.Point(cur.X + 1, cur.Y));
                        q2.Enqueue(SC2Util.Point(cur.X - 1, cur.Y));
                        q2.Enqueue(SC2Util.Point(cur.X, cur.Y + 1));
                        q2.Enqueue(SC2Util.Point(cur.X, cur.Y - 1));
                    }
                }
                q1 = q2;
                q2 = new Queue<Point2D>();
            }
            return result;
        }

        public List<BoolGrid> GetGroups()
        {
            List<BoolGrid> groups = new List<BoolGrid>();
            ArrayBoolGrid encountered = new ArrayBoolGrid(Width(), Height());

            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                    if (this[x, y] && !encountered[x, y])
                        groups.Add(GetConnected(SC2Util.Point(x, y), encountered));

            return groups;
        }

        public BoolGrid Shrink()
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());

            for (int x = 1; x < Width() - 1; x++)
                for (int y = 1; y < Height() - 1; y++)
                {
                    bool success = true;
                    for (int dx = -1; dx <= 1 && success; dx++)
                        for (int dy = -1; dy <= 1 && success; dy++)
                            success = this[x + dx, y + dy];
                    if (success)
                        result[x, y] = true;
                }

            return result;
        }

        public List<Point2D> ToList()
        {
            List<Point2D> result = new List<Point2D>();

            for (int x = 1; x < Width() - 1; x++)
                for (int y = 1; y < Height() - 1; y++)
                    if (this[x, y])
                        result.Add(SC2Util.Point(x, y));

            return result;
        }

        public BoolGrid Crop(int startX, int startY, int endX, int endY)
        {
            ArrayBoolGrid result = new ArrayBoolGrid(Width(), Height());
            for (int x = 0; x < Width(); x++)
                for (int y = 0; y < Height(); y++)
                {
                    if (x < startX || x >= endX || y < startY || y >= endY)
                        result[x, y] = false;
                    else result[x, y] = this[x, y];
                }
            return result;
        }
    }
}
