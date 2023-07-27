namespace Sharky
{
    public class TrainingTypeData
    {
        public HashSet<UnitTypes> ProducingUnits { get; set; }
        public Abilities Ability { get; set; }
        public Abilities WarpInAbility { get; set; }
        public int Minerals { get; set; }
        public int Gas { get; set; }
        public int Food { get; set; }
        public bool RequiresTechLab { get; set; }
        public bool IsAddOn { get; set; }
    }
}
