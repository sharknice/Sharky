using SC2APIProtocol;
using System;

namespace Sharky.MapAnalysis
{
    public abstract class SC2Util
    {
        public static int GetDataValue(ImageData data, int x, int y)
        {
            if (data.BitsPerPixel == 1)
                return GetDataValueBit(data, x, y);

            return GetDataValueByte(data, x, y);
        }
        public static int GetDataValueBit(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;
            return ((data.Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? 0 : 1;
        }
        public static int GetDataValueByte(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            return data.Data[pixelID];
        }
        public static int GetDataValueOld(ImageData data, int x, int y)
        {
            int pixelID = x + (data.Size.Y - 1 - y) * data.Size.X;
            return data.Data[pixelID];
        }

        //public static bool GetTilePlacable(int x, int y)
        //{
        //    if (x < 0 || y < 0 || x >= Shark.Bot.GameInfo.StartRaw.PlacementGrid.Size.X || y >= Shark.Bot.GameInfo.StartRaw.PlacementGrid.Size.Y)
        //        return false;
        //    return SC2Util.GetDataValue(Shark.Bot.GameInfo.StartRaw.PlacementGrid, x, y) != 0;
        //}

        public static Point2D Point(float x, float y)
        {
            Point2D result = new Point2D
            {
                X = x,
                Y = y
            };
            return result;
        }

        public static Point Point(float x, float y, float z)
        {
            Point result = new Point
            {
                X = x,
                Y = y,
                Z = z
            };
            return result;
        }

        public static float DistanceSq(Point pos1, Point2D pos2)
        {
            return DistanceSq(To2D(pos1), pos2);
        }

        public static float DistanceSq(Point pos1, Point pos2)
        {
            return DistanceSq(To2D(pos1), To2D(pos2));
        }

        public static float DistanceSq(Point2D pos1, Point pos2)
        {
            return DistanceSq(pos1, To2D(pos2));
        }

        public static float DistanceSq(Point2D pos1, Point2D pos2)
        {
            return (pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Y - pos2.Y) * (pos1.Y - pos2.Y);
        }

        public static float DistanceGrid(Point pos1, Point pos2)
        {
            return DistanceGrid(To2D(pos1), To2D(pos2));
        }

        public static float DistanceGrid(Point pos1, Point2D pos2)
        {
            return DistanceGrid(To2D(pos1), pos2);
        }

        public static float DistanceGrid(Point2D pos1, Point pos2)
        {
            return DistanceGrid(pos1, To2D(pos2));
        }

        public static float DistanceGrid(Point2D pos1, Point2D pos2)
        {
            return Math.Abs(pos1.X - pos2.X) + Math.Abs(pos1.Y - pos2.Y);
        }

        public static Point2D To2D(Point pos)
        {
            return Point(pos.X, pos.Y);
        }

        //public static Point To3D(Point2D pos)
        //{
        //    return Point(pos.X, pos.Y, Shark.Bot.MapAnalyzer.MapHeight((int)pos.X, (int)pos.Y));
        //}

        public static Point2D Normalize(Point2D point)
        {
            float length = (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
            return Point(point.X / length, point.Y / length);
        }

        public static Point2D TowardCardinal(Point2D pos1, Point2D pos2, float distance)
        {
            if (Math.Abs(pos2.X - pos1.X) >= Math.Abs(pos2.Y - pos1.Y))
            {
                if (pos2.X > pos1.X)
                    return Point(pos1.X + distance, pos1.Y);
                else
                    return Point(pos1.X - distance, pos1.Y);
            }
            else
            {
                if (pos2.Y > pos1.Y)
                    return Point(pos1.X, pos1.Y + distance);
                else
                    return Point(pos1.X, pos1.Y - distance);
            }
        }
    }
}
