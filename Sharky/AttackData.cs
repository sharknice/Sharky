using SC2APIProtocol;

namespace Sharky
{
    public class AttackData
    {
        public bool Attacking { get; set; }
        public Point2D ArmyPoint { get; set; }
        public int ArmyFoodAttack { get; set; }
        public bool CustomAttackFunction { get; set; }
    }
}
