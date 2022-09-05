using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.Terran;
using Sharky.DefaultBot;
using Sharky.MicroTasks;
using SharkyTerranExampleBot.Builds.BuildServices;
using System.Collections.Generic;

namespace SharkyTerranExampleBot.Builds
{
    public class MassVikings : TerranSharkyBuild
    {
        ExpandForever ExpandForever;

        public MassVikings(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        { 
            ExpandForever = new ExpandForever(defaultSharkyBot);
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            MacroData.DesiredGases = 0;

            MacroData.AddOnSwaps[this.Name() + "reactor"] = new AddOnSwap(UnitTypes.TERRAN_FACTORYREACTOR, UnitTypes.TERRAN_FACTORY, UnitTypes.TERRAN_STARPORT, true);

            var defenseSquadTask = (DefenseSquadTask)MicroTaskData["DefenseSquadTask"];
            defenseSquadTask.DesiredUnitsClaims = new List<DesiredUnitsClaim> { new DesiredUnitsClaim(UnitTypes.TERRAN_MARINE, 1) };
            defenseSquadTask.Enable();
        }

        public override void OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            Opening(frame);
            Starports();
            ExpandForever.OnFrame();
            AddProduction();
        }

        private void Opening(int frame)
        {
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

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_BARRACKS) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_FACTORY] = 1;
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

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_FACTORY) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_STARPORT] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_STARPORT] = 2;
                }
                if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] < 1)
                {
                    MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] = 1; // build reactor on the factory as starport is building
                }
            }

            if (UnitCountService.Completed(UnitTypes.TERRAN_FACTORYREACTOR) > 0 || UnitCountService.Completed(UnitTypes.TERRAN_REACTOR) > 0 || UnitCountService.Completed(UnitTypes.TERRAN_STARPORTREACTOR) > 0)
            {
                MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_FACTORYREACTOR] = 0; // swapped reactor to starport, don't build more on the factory
            }
        }

        private void Starports()
        {
            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_STARPORT) > 0)
            {
                if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_STARPORT) > 0)
                {
                    if (MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_STARPORTREACTOR] < UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_STARPORT))
                    {
                        MacroData.DesiredAddOnCounts[UnitTypes.TERRAN_STARPORTREACTOR] = UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_STARPORT);
                    }

                    if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_STARPORTREACTOR) > 0)
                    {
                        if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_VIKINGFIGHTER] < 50)
                        {
                            MacroData.DesiredUnitCounts[UnitTypes.TERRAN_VIKINGFIGHTER] = 50;
                        }
                    }
                }
            }
        }

        void AddProduction()
        {
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_STARPORT) >= 2 && UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) > 1)
            {
                if (MacroData.Minerals > 500 && MacroData.VespeneGas > 300)
                {
                    if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_STARPORT] <= UnitCountService.Count(UnitTypes.TERRAN_STARPORT))
                    {
                        MacroData.DesiredProductionCounts[UnitTypes.TERRAN_STARPORT]++;
                    }
                }

            }
        }
    }
}
