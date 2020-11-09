using SC2APIProtocol;

namespace Sharky.Managers
{
    public interface ITargetingManager : IManager
    {
        Point2D AttackPoint { get; }
        Point2D DefensePoint { get; }

        Point2D GetAttackPoint(Point2D armyPoint);
    }
}
