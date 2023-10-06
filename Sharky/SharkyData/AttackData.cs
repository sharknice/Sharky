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
        public float ContainTrigger { get; set; }
        public float KillTrigger { get; set; }
        public bool ContainBelowKill { get; set; }
        public bool RequireDetection { get; set; }
        public bool RequireMaxOut { get; set; }
        public bool AttackWhenMaxedOut { get; set; }
        public bool RequireBank { get; set; }
        public int RequiredVespeneBank { get; set; }
        public int RequiredMineralBank { get; set; }
        public bool AttackWhenOverwhelm { get; set; }
        public bool GroupUpEnabled { get; set; }

        /// <summary>
        /// if minerals exceed this amount attack no matter what
        /// </summary>
        public int MaxMineralBankEdgeCase { get; set; }

        public TargetPriorityCalculation TargetPriorityCalculation { get; set; }
    }
}
