using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;

namespace Sharky
{
    public class TargetingData
    {
        public Point2D AttackPoint { get; set; }
        public Point2D ForwardDefensePoint { get; set; }
        public Point2D MainDefensePoint { get; set; }
        public Point2D SelfMainBasePoint { get; set; }
        public Point2D EnemyMainBasePoint { get; set; }
        public bool HiddenEnemyBase { get; set; }
        public List<Point2D> ForwardDefenseWallOffPoints { get; set; }
        public ChokePoints ChokePoints { get; set; }
    }
}
