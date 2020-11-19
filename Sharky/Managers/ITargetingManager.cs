using SC2APIProtocol;

namespace Sharky.Managers
{
    public interface ITargetingManager : IManager
    {
        Point2D AttackPoint { get; }
        Point2D DefensePoint { get; }
        Point2D SelfMainBasePoint { get; }
        Point2D EnemyMainBasePoint { get; }

        Point2D GetAttackPoint(Point2D armyPoint);
    }
}
