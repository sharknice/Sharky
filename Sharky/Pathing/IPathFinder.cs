﻿namespace Sharky.Pathing
{
    public interface IPathFinder
    {
        List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, PathFinder pathFinder = null);
        List<Vector2> GetGroundPath(float startX, float startY, float endX, float endY, int frame, float radius);
        List<Vector2> GetSafeGroundPath(float startX, float startY, float endX, float endY, int frame);
        List<Vector2> GetUndetectedGroundPath(float startX, float startY, float endX, float endY, int frame);
        List<Vector2> GetSafeAirPath(float startX, float startY, float endX, float endY, int frame);
    }
}
