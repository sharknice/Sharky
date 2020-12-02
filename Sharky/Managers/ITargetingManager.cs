using SC2APIProtocol;

namespace Sharky.Managers
{
    public interface ITargetingManager : IManager
    {
        Point2D AttackPoint { get; }
        Point2D ForwardDefensePoint { get; }
        Point2D MainDefensePoint { get; }
        Point2D SelfMainBasePoint { get; }
        Point2D EnemyMainBasePoint { get; }

        Point2D GetAttackPoint(Point2D armyPoint);
    }
}
