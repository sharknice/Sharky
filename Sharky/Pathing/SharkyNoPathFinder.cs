﻿namespace Sharky.Pathing
{
    public class SharkyNoPathFinder : IPathFinder
    {
        public SharkyNoPathFinder()
        {
        }

        public List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, PathFinder pathFinder = null)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame)
        {
            return new List<Vector2>();
        }

        public List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, float radius)
        {
            return GetGroundPath(startX, startY, endX, endY, frame);
        }
    }
}
