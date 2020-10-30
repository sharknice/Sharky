namespace Sharky.Builds
{
    public interface IMacroBalancer
    {
        void BalanceSupply();
        void BalanceGases();
        void BalanceTech();
        void BalanceProduction();
        void BalanceProductionBuildings();
        void BalanceGasWorkers();
    }
}
