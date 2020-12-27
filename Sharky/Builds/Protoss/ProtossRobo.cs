using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using Sharky.MicroTasks;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class ProtossRobo : ProtossSharkyBuild
    {
        SharkyOptions SharkyOptions;
        MicroManager MicroManager;
        EnemyRaceManager EnemyRaceManager;

        public ProtossRobo(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, ChronoData chronoData, SharkyOptions sharkyOptions, MicroManager microManager, EnemyRaceManager enemyRaceManager, ICounterTransitioner counterTransitioner, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatManager, chronoData, counterTransitioner, unitCountService)
        {
            SharkyOptions = sharkyOptions;
            MicroManager = microManager;
            EnemyRaceManager = enemyRaceManager;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            ChronoData.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_IMMORTAL,
            };

            MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;

            var desiredUnitsClaim = new DesiredUnitsClaim(UnitTypes.PROTOSS_ADEPT, 1);
            if (EnemyRaceManager.EnemyRace == Race.Protoss)
            {
                desiredUnitsClaim = new DesiredUnitsClaim(UnitTypes.PROTOSS_STALKER, 1);
            }
            
            if (MicroManager.MicroTasks.ContainsKey("DefenseSquadTask"))
            {
                var defenseSquadTask = (DefenseSquadTask)MicroManager.MicroTasks["DefenseSquadTask"];
                defenseSquadTask.DesiredUnitsClaims = new List<DesiredUnitsClaim> { desiredUnitsClaim };
                defenseSquadTask.Enable();

                if (MicroManager.MicroTasks.ContainsKey("AttackTask"))
                {
                    MicroManager.MicroTasks["AttackTask"].ResetClaimedUnits();
                }
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) > 0)
            {
                // TODO: MacroData.ShieldsAtEveryExpansion = 2;
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 4)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 4;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 2)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 2;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] = 1;
                }
            }

            if (UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) >= 2)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_WARPPRISM] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_WARPPRISM] = 1;
                }
                if (UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) >= 3 && MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0)
            {
                MacroData.DesiredGases = 3;
                MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;

                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] < 10)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] = 10;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) * 3)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) * 3;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL))
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL);
                }
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2) // start expanding
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] < 1)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] = 1;
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] < 2 && UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) > 0)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] = 2;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) >= 2)
            {
                BuildOptions.StrictGasCount = false;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) >= 2 && MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] < 1)
            {
                MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] = 1;
            }

            // TODO: EnemyManager get EnemyRace
            //if (EnemyRace == SC2APIProtocol.Race.Terran)
            //{
            //    if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0 && MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] < 1)
            //    {
            //        MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] = 1;
            //    }
            //}

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) >= 2 && UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) > 3)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 3)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 3;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_NEXUS) > 2 && UnitCountService.Completed(UnitTypes.PROTOSS_FORGE) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] = 1;
                }
                MacroData.DesiredUpgrades[Upgrades.BLINKTECH] = true;
                ChronoData.ChronodUpgrades.Add(Upgrades.BLINKTECH);
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_NEXUS) > 2 && UnitCountService.Completed(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] < 2)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] = 2;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) > 0 && UnitCountService.Completed(UnitTypes.PROTOSS_FORGE) >= 2)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TEMPLARARCHIVE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TEMPLARARCHIVE] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_TEMPLARARCHIVE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_HIGHTEMPLAR] < 2)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_HIGHTEMPLAR] = 2;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON] < 3)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON] = 3;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_FORGE) > 0)
            {
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDWEAPONSLEVEL1] = true;
                ChronoData.ChronodUpgrades.Add(Upgrades.PROTOSSGROUNDWEAPONSLEVEL1);
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_FORGE) > 1)
            {
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDARMORSLEVEL1] = true;
                ChronoData.ChronodUpgrades.Add(Upgrades.PROTOSSGROUNDARMORSLEVEL1);
            }

            if (observation.Observation.GameLoop > SharkyOptions.FramesPerSecond * 15 * 60 || MacroData.FoodUsed > 125)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ORACLE] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ORACLE] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] = 1;
                }
            }

            if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_ROBOTICSBAY] < 1 && UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 1 && UnitCountService.Completed(UnitTypes.PROTOSS_NEXUS) > 2)
            {
                MacroData.DesiredTechCounts[UnitTypes.PROTOSS_ROBOTICSBAY] = 1;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSBAY) > 0)
            {
                if (UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) > 3)
                {
                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_DISRUPTOR] < 3)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_DISRUPTOR] = 3;
                    }
                }

                MacroData.DesiredUpgrades[Upgrades.GRAVITICDRIVE] = true;
                ChronoData.ChronodUpgrades.Add(Upgrades.GRAVITICDRIVE);
            }

            // TODO: ProductionBalancer
            //if (UnitCountService.Completed(UnitTypes.PROTOSS_NEXUS) > 3)
            //{
            //    ProductionBalancer.BalanceProductionCapacity();
            //    ProductionBalancer.BalanceUnitCounterProduction();
            //    ProductionBalancer.ProduceUntilCapped();
            //    ProductionBalancer.ResearchNeededUpgrades();
            //    ProductionBalancer.ExpandForever();
            //}

            //if (UnitCountService.Count(UnitTypes.PROTOSS_IMMORTAL) > 4)
            //{
            //    ProductionBalancer.ExpandForever();
            //}
        }
    }
}
