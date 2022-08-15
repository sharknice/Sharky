using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.DefaultBot;
using Sharky.MicroTasks;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class Robo : ProtossSharkyBuild
    {
        public Robo(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            MacroData.DesiredGases = 2;

            ChronoData.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
            };

            MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;

            var desiredUnitsClaim = new DesiredUnitsClaim(UnitTypes.PROTOSS_ADEPT, 1);
            if (EnemyData.EnemyRace == Race.Protoss)
            {
                desiredUnitsClaim = new DesiredUnitsClaim(UnitTypes.PROTOSS_STALKER, 1);
            }

            if (MicroTaskData.MicroTasks.ContainsKey("DefenseSquadTask"))
            {
                var defenseSquadTask = (DefenseSquadTask)MicroTaskData.MicroTasks["DefenseSquadTask"];
                defenseSquadTask.DesiredUnitsClaims = new List<DesiredUnitsClaim> { desiredUnitsClaim };
                defenseSquadTask.Enable();

                if (MicroTaskData.MicroTasks.ContainsKey("AttackTask"))
                {
                    MicroTaskData.MicroTasks["AttackTask"].ResetClaimedUnits();
                }
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 1;
                }
            }
            else
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ADEPT] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ADEPT] = 1;
                }
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
            }
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }

            if (UnitCountService.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0 && UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) >= 2)
            {
                // TODO: MacroData.PylonsAtEveryExpansion = true; 
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }

                if (UnitCountService.Count(UnitTypes.PROTOSS_STALKER) + UnitCountService.Count(UnitTypes.PROTOSS_ADEPT) > 0)
                {
                    MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 3)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 3;
                    }
                }
            }

            if (UnitCountService.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) < 1 && UnitCountService.Count(UnitTypes.PROTOSS_STALKER) + UnitCountService.Count(UnitTypes.PROTOSS_ADEPT) > 0)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] = 1;
            }

            if (EnemyData.EnemyRace == SC2APIProtocol.Race.Terran)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] = 1;
                }
            }
            else
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] = 1;
                }
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
            }
        }

        public override bool Transition(int frame)
        {
            return UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0 && UnitCountService.Completed(UnitTypes.PROTOSS_NEXUS) > 0;
        }
    }
}
