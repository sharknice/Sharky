using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.Terran;
using Sharky.DefaultBot;
using SharkyTerranExampleBot.Builds.BuildServices;

namespace SharkyTerranExampleBot.Builds
{
    public class HellionRush : TerranSharkyBuild
    {
        ExpandForever ExpandForever;

        public HellionRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        { 
            ExpandForever = new ExpandForever(defaultSharkyBot);
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.WallOffType = Sharky.Builds.BuildingPlacement.WallOffType.Full;

            BuildOptions.StrictGasCount = true;
            MacroData.DesiredGases = 0;

            BuildOptions.StrictSupplyCount = true;
            MacroData.DesiredSupplyDepots = 0;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            Opening(frame);
            Factories();

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) >= 2)
            {
                ExpandForever.OnFrame();
                AddProduction();
            }
        }

        private void Opening(int frame)
        {
            SendScvForFirstDepot(frame);

            if (MacroData.FoodUsed >= 14)
            {
                if (MacroData.DesiredSupplyDepots < 1)
                {
                    MacroData.DesiredSupplyDepots = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_SUPPLYDEPOT) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_BARRACKS) > 0)
            {
                BuildOptions.StrictGasCount = false;
            }

            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_REFINERY) >= 2)
            {
                if (MacroData.DesiredSupplyDepots < 2)
                {
                    MacroData.DesiredSupplyDepots = 2;
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_BARRACKS) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] = 2;
                }
                if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARINE] < 2)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARINE] = 2;
                }
            }
        }

        private void Factories()
        {
            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_FACTORY) > 0)
            {
                BuildOptions.StrictSupplyCount = false;

                if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) > 0)
                {
                    if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYTECHLAB] < 1)
                    {
                        MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYTECHLAB] = 1;
                    }

                    if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORYTECHLAB) > 0)
                    {
                        MacroData.DesiredUpgrades[Upgrades.HIGHCAPACITYBARRELS] = true;
                    }

                    if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) >= 2)
                    {
                        if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] < 1)
                        {
                            MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] = 1;
                        }
                    }

                    if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORYTECHLAB) + UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORYREACTOR) > 0)
                    {
                        if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_HELLION] < 50)
                        {
                            MacroData.DesiredUnitCounts[UnitTypes.TERRAN_HELLION] = 50;
                        }
                    }
                }
            }
        }

        void AddProduction()
        {
            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) > 1)
            {
                if (MacroData.Minerals > 500 && MacroData.VespeneGas > 300)
                {
                    if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] <= UnitCountService.Count(UnitTypes.TERRAN_FACTORY))
                    {
                        MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY]++;
                    }
                }
                if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) > 2)
                {
                    if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] < UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) - 1)
                    {
                        MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] = UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) - 1;
                    }
                }
            }
        }
    }
}
