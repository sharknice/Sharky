namespace Sharky
{
    public class TargetingData
    {
        public Point2D AttackPoint { get; set; }
        public Point2D ForwardDefensePoint { get; set; }
        public Point2D MainDefensePoint { get; set; }
        public Point2D NaturalBasePoint { get; set; }
        public Point2D SelfMainBasePoint { get; set; }
        public Point2D EnemyMainBasePoint { get; set; }
        public bool HiddenEnemyBase { get; set; }
        public List<Point2D> ForwardDefenseWallOffPoints { get; set; }
        public ChokePoints ChokePoints { get; set; }
        public WallOffBasePosition WallOffBasePosition { get; set; }
        public AttackState AttackState { get; set; }
        public List<UnitCalculation> WallBuildings { get; set; }
        public Point2D NaturalFrontScoutPoint;
        public Vector2 EnemyArmyCenter;
    }
}
