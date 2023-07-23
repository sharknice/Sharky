using SC2APIProtocol;
using Sharky.MicroTasks;
using System.Numerics;

namespace Sharky.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Calculates distance of two points
        /// </summary>
        /// <param name="thisPos"></param>
        /// <param name="pos">Point to get distance to</param>
        /// <returns></returns>
        public static float Distance(this Point2D thisPos, Point2D pos)
        {
            return Vector2.Distance(pos.ToVector2(), thisPos.ToVector2());
        }

        /// <summary>
        /// Calculates distance of two vectors
        /// </summary>
        /// <param name="thisPos"></param>
        /// <param name="pos">Point to get distance to</param>
        /// <returns></returns>
        public static float Distance(this Vector2 thisPos, Vector2 pos)
        {
            return Vector2.Distance(pos, thisPos);
        }

        /// <summary>
        /// Calculates squared distance of two points
        /// </summary>
        /// <param name="thisPos"></param>
        /// <param name="pos">Point to get distance to</param>
        /// <returns></returns>
        public static float DistanceSquared(this Point2D thisPos, Point2D pos)
        {
            return Vector2.DistanceSquared(pos.ToVector2(), thisPos.ToVector2());
        }

        /// <summary>
        /// Calculates squared distance of two points
        /// </summary>
        /// <param name="thisPos"></param>
        /// <param name="pos">Point to get distance to</param>
        /// <returns></returns>
        public static float DistanceSquared(this Point thisPos, Point pos)
        {
            return Vector2.DistanceSquared(pos.ToVector2(), thisPos.ToVector2());
        }

        /// <summary>
        /// Calculates squared distance of two vectors
        /// </summary>
        /// <param name="thisPos"></param>
        /// <param name="pos">Point to get distance to</param>
        /// <returns></returns>
        public static float DistanceSquared(this Vector2 thisPos, Vector2 pos)
        {
            return Vector2.DistanceSquared(pos, thisPos);
        }

        /// <summary>
        /// Creates Point2D instance
        /// </summary>
        /// <param name="thisPoint"></param>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns></returns>
        public static Point2D Create(this Point2D thisPoint, float x, float y)
        {
            return new Point2D() { X = x, Y = y };
        }

        /// <summary>
        /// Converts Point to Points2D
        /// </summary>
        /// <param name="thisPoint"></param>
        /// <returns></returns>
        public static Point2D ToPoint2D(this Point thisPoint)
        {
            return new Point2D { X = thisPoint.X, Y = thisPoint.Y };
        }

        /// <summary>
        /// Converts Vector2 to Points2D
        /// </summary>
        /// <param name="thisVec"></param>
        /// <returns></returns>
        public static Point2D ToPoint2D(this Vector2 thisVec)
        {
            return new Point2D { X = thisVec.X, Y = thisVec.Y };
        }

        /// <summary>
        /// Converts Vector2 to Points2D
        /// </summary>
        /// <param name="thisVec"></param>
        /// <returns></returns>
        public static Point ToPoint(this Vector2 thisVec, float z = 0)
        {
            return new Point { X = thisVec.X, Y = thisVec.Y, Z = z };
        }

        /// <summary>
        /// Converts Point2 to Point
        /// </summary>
        /// <param name="thisVec"></param>
        /// <returns></returns>
        public static Point ToPoint(this Point2D thisVec, float z = 16)
        {
            return new Point { X = thisVec.X, Y = thisVec.Y, Z = z };
        }

        /// <summary>
        /// Converts Point2D to Vector2
        /// </summary>
        /// <param name="thisPoint"></param>
        /// <returns></returns>
        public static Vector2 ToVector2(this Point2D thisPoint)
        {
            return new Vector2(thisPoint.X, thisPoint.Y);
        }

        /// <summary>
        /// Converts Point to Vector2
        /// </summary>
        /// <param name="thisPoint"></param>
        /// <returns></returns>
        public static Vector2 ToVector2(this Point thisPoint)
        {
            return new Vector2(thisPoint.X, thisPoint.Y);
        }
    }
}
