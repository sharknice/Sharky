using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.DefaultBot;
using Sharky.MicroTasks;
using System.Collections.Generic;

namespace SharkyProtossExampleBot.Builds
{
    public class OneBaseCarriers : ProtossSharkyBuild
    {
        PermanentWallOffTask WallOffTask;
        DestroyWallOffTask DestroyWallOffTask;

        public OneBaseCarriers(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictWorkerCount = true;
            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_PROBE] = 23;

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
                UnitTypes.PROTOSS_ORACLE,
                UnitTypes.PROTOSS_CARRIER,
                UnitTypes.PROTOSS_MOTHERSHIP
            };

            ChronoData.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.PROTOSSAIRWEAPONSLEVEL1
            };

            if (!MicroTaskData.MicroTasks["OracleWorkerHarassTask"].Enabled)
            {
                MicroTaskData.MicroTasks["OracleWorkerHarassTask"].Enable();
            }

            WallOffTask = (PermanentWallOffTask)MicroTaskData.MicroTasks["PermanentWallOffTask"];
            DestroyWallOffTask = (DestroyWallOffTask)MicroTaskData.MicroTasks["DestroyWallOffTask"];
        }

        public override void OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;
            SendProbeForFirstGateway(frame);
            SendProbeForCyberneticsCore(frame);

            if (UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }

                if (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) > 0) // use EquivalentTypeCount instead of Count so Warp Gates are included
                {
                    BuildOptions.StrictGasCount = false;

                    if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
                    {
                        if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                        {
                            MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                        }
                    }
                }

                if (UnitCountService.Count(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
                {
                    if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] < 1)
                    {
                        MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] = 1;
                    }

                    if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
                    {
                        if (!WallOffTask.Enabled)
                        {
                            WallOffTask.Enable();
                        }

                        if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] < 1)
                        {
                            MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] = 1;
                        }

                        if (UnitCountService.Completed(UnitTypes.PROTOSS_MOTHERSHIP) > 0)
                        {
                            MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRWEAPONSLEVEL1] = true;
                        }
                    }
                }

                if (UnitCountService.Completed(UnitTypes.PROTOSS_STARGATE) > 0)
                {
                    if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FLEETBEACON] < 1)
                    {
                        MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FLEETBEACON] = 1;
                    }

                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ORACLE] < 1)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ORACLE] = 1;
                    }

                    if (UnitCountService.Completed(UnitTypes.PROTOSS_FLEETBEACON) > 0)
                    {
                        if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_CARRIER] < 12)
                        {
                            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_CARRIER] = 12;
                        }
                        if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_MOTHERSHIP] < 1)
                        {
                            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_MOTHERSHIP] = 1;
                        }
                    }
                }

                if (UnitCountService.Completed(UnitTypes.PROTOSS_FORGE) > 0)
                {
                    if (MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_PHOTONCANNON] < 3)
                    {
                        MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_PHOTONCANNON] = 3;
                    }
                }
            }
        }

        public override List<string> CounterTransition(int frame)
        {
            return new List<string>();
        }

        public override bool Transition(int frame)
        {
            if (UnitCountService.Count(UnitTypes.PROTOSS_CARRIER) > 10 || MacroData.Frame > SharkyOptions.FramesPerSecond * 10 * 60)
            {
                return true;
            }
            return false;
        }

        public override void EndBuild(int frame)
        {
            if (WallOffTask.Enabled)
            {
                DestroyWallOffTask.WallPoints = WallOffTask.PlacementPoints;
                DestroyWallOffTask.Enable();
                WallOffTask.Disable();
            }
        }
    }
}
