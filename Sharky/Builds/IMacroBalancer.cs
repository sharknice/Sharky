﻿namespace Sharky.Builds
{
    public interface IMacroBalancer
    {
        void BalanceSupply();
        void BalanceGases();
        void BalanceTech();
        void BalanceAddOns();
        void BalanceDefensiveBuildings();
        void BalanceProduction();
        void BalanceProductionBuildings();
        void BalanceMorphs();
        void BalanceGasWorkers();
    }
}
