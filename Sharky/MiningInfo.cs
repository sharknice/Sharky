using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sharky
{
    public class MiningInfo
    {
        public MiningInfo(Unit resourceUnit, Point baseLocation)
        {
            ResourceUnit = resourceUnit;
            Workers = new List<UnitCommander>();

            var baseVector = new Vector2(baseLocation.X, baseLocation.Y);
            var mineralVector = new Vector2(ResourceUnit.Pos.X, ResourceUnit.Pos.Y);

            var angle = Math.Atan2(mineralVector.Y - baseVector.Y, baseVector.X - mineralVector.X);
            DropOffPoint = new Point2D { X = baseVector.X + (float)(-2 * Math.Cos(angle)), Y = baseVector.Y - (float)(-2 * Math.Sin(angle)) };

            var mineAngle = Math.Atan2(baseVector.Y - mineralVector.Y, mineralVector.X - baseVector.X);
            HarvestPoint = new Point2D { X = mineralVector.X + (float)(-.5 * Math.Cos(mineAngle)), Y = mineralVector.Y - (float)(-.5 * Math.Sin(mineAngle)) };
        }

        public List<UnitCommander> Workers { get; set; }
        public Unit ResourceUnit { get; set; }
        public Point2D DropOffPoint { get; set; }
        public Point2D HarvestPoint { get; set; }
    }
}
