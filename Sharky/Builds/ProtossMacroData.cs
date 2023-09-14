namespace Sharky
{
    public class ProtossMacroData
    {
        public List<UnitTypes> NexusUnits;
        public List<UnitTypes> GatewayUnits;
        public List<UnitTypes> RoboticsFacilityUnits;
        public List<UnitTypes> StargateUnits;

        public int DesiredPylons;
        public bool BuildPylon;
        public int DesiredPylonsAtEveryBase;
        public int DesiredPylonsAtNextBase;
        public int DesiredPylonsAtDefensivePoint;
        public int DesiredPylonsAtEveryMineralLine;

        public int DesiredExtraBaseSimCityPylons;
        public int DesiredExtraBaseSimCityCannons;
        public int DesiredExtraBaseSimCityBatteries;

        public bool ExtraBasesGatewayCannonSimCity;
    }
}
