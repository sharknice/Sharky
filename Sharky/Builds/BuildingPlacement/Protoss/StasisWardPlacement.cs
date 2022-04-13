using SC2APIProtocol;
using System;

namespace Sharky.Builds.BuildingPlacement
{
    public class StasisWardPlacement
    {
        DebugService DebugService;
        BuildingService BuildingService;

        public StasisWardPlacement(DebugService debugService, BuildingService buildingService)
        {
            DebugService = debugService;
            BuildingService = buildingService;   
        }

        public Point2D FindPlacement(Point2D target)
        {
            var x = target.X;
            var y = target.Y;
            var radius = 1f;

            // start at 12 o'clock then rotate around 12 times, increase radius by 1 until it's more than maxDistance
            while (radius < 10)
            {
                var fullCircle = Math.PI * 2;
                var sliceSize = fullCircle / (8.0 + radius);
                var angle = 0.0;
                while (angle + (sliceSize / 2) < fullCircle)
                {
                    var point = new Point2D { X = x + (float)(radius * Math.Cos(angle)), Y = y + (float)(radius * Math.Sin(angle)) };

                    if (BuildingService.AreaBuildable(point.X, point.Y, 1.25f) && !BuildingService.Blocked(point.X, point.Y, 1.25f, .1f))
                    {                     
                        DebugService.DrawSphere(new Point { X = point.X, Y = point.Y, Z = 12 });
                        return point;
                    }
                    angle += sliceSize;
                }
                radius += 1;
            }

            return null;
        }
    }
}
