namespace Sharky
{
    public class MicroData
    {
        public Dictionary<UnitTypes, IIndividualMicroController> IndividualMicroControllers { get; set; }
        public IIndividualMicroController IndividualMicroController { get; set; }
    }
}
