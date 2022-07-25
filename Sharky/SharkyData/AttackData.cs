using SC2APIProtocol;

namespace Sharky
{
    public class AttackData
    {
        public bool Attacking { get; set; }
        public Point2D ArmyPoint { get; set; }
        public int ArmyFoodAttack { get; set; }
        public int ArmyFoodRetreat { get; set; }
        public bool CustomAttackFunction { get; set; }
        public bool UseAttackDataManager { get; set; }
        public float RetreatTrigger { get; set; }
        public float AttackTrigger { get; set; }
        public bool RequireDetection { get; set; }
        public bool RequireMaxOut { get; set; }
        public bool AttackWhenMaxedOut { get; set; }
        public bool AttackWhenOverwhelm { get; set; }

        public TargetPriorityCalculation TargetPriorityCalculation { get; set; }
    }
}
