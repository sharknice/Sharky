using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.DefaultBot;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class EveryProtossUnit : ProtossSharkyBuild
    {
        public EveryProtossUnit(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot, counterTransitioner)
        {
        }

        public EveryProtossUnit(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, ICounterTransitioner counterTransitioner, UnitCountService unitCountService, MicroTaskData microTaskData, FrameToTimeConverter frameToTimeConverter) 
            : base(buildOptions, macroData, activeUnitData, attackData, chatService, chronoData, counterTransitioner, unitCountService, microTaskData, frameToTimeConverter)
        {
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
                UnitTypes.PROTOSS_PROBE,
            };
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] < 2)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FORGE] = 2;
                }

                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_FORGE) > 0)
            {
                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.PROTOSS_PHOTONCANNON] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.PROTOSS_PHOTONCANNON] = 1;
                }
                if (MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.PROTOSS_SHIELDBATTERY] < 1)
                {
                    MacroData.DesiredDefensiveBuildingsCounts[UnitTypes.PROTOSS_SHIELDBATTERY] = 1;
                }

                MacroData.DesiredPylonsAtDefensivePoint = 1;
                MacroData.DesiredPylonsAtEveryBase = 1;
                MacroData.DesiredPylonsAtNextBase = 1;
                MacroData.DesiredPylonsAtEveryMineralLine = 1;

                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_PHOTONCANNON] = 1;
                MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 1;

                MacroData.DesiredDefensiveBuildingsAtEveryBase[UnitTypes.PROTOSS_PHOTONCANNON] = 1;
                MacroData.DesiredDefensiveBuildingsAtEveryMineralLine[UnitTypes.PROTOSS_PHOTONCANNON] = 1;

                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDWEAPONSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDARMORSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDARMORSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSGROUNDARMORSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSSHIELDSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSSHIELDSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSSHIELDSLEVEL3] = true;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_SENTRY] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ADEPT] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ADEPT] = 1;
                }

                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] = 1;
                }
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] = 1;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TWILIGHTCOUNCIL] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRARMORSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRARMORSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRARMORSLEVEL3] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRWEAPONSLEVEL1] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRWEAPONSLEVEL2] = true;
                MacroData.DesiredUpgrades[Upgrades.PROTOSSAIRWEAPONSLEVEL3] = true;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_WARPPRISM] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_WARPPRISM] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] = 1;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_ROBOTICSBAY] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_ROBOTICSBAY] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_ROBOTICSBAY) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_COLOSSUS] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_COLOSSUS] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_DISRUPTOR] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_DISRUPTOR] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.OBSERVERGRAVITICBOOSTER] = true;
                MacroData.DesiredUpgrades[Upgrades.GRAVITICDRIVE] = true;
                MacroData.DesiredUpgrades[Upgrades.EXTENDEDTHERMALLANCE] = true;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_STARGATE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_PHOENIX] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_PHOENIX] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ORACLE] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ORACLE] = 1;
                }

                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FLEETBEACON] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_FLEETBEACON] = 1;
                }
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_FLEETBEACON) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_CARRIER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_CARRIER] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_MOTHERSHIP] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_MOTHERSHIP] = 1;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_TEMPEST] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_TEMPEST] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.PHOENIXRANGEUPGRADE] = true;
                MacroData.DesiredUpgrades[Upgrades.VOIDRAYSPEEDUPGRADE] = true;
                MacroData.DesiredUpgrades[Upgrades.TECTONICDESTABILIZERS] = true;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_TWILIGHTCOUNCIL) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TEMPLARARCHIVE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_TEMPLARARCHIVE] = 1;
                }
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_DARKSHRINE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_DARKSHRINE] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.BLINKTECH] = true;
                MacroData.DesiredUpgrades[Upgrades.CHARGE] = true;
                MacroData.DesiredUpgrades[Upgrades.ADEPTPIERCINGATTACK] = true;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_TEMPLARARCHIVE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_HIGHTEMPLAR] < 2)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_HIGHTEMPLAR] = 2;
                }
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.PSISTORMTECH] = true;
            }

            if (UnitCountService.Completed(UnitTypes.PROTOSS_DARKSHRINE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_DARKTEMPLAR] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_DARKTEMPLAR] = 1;
                }

                MacroData.DesiredUpgrades[Upgrades.DARKTEMPLARBLINKUPGRADE] = true;
            }

            if (MacroData.Minerals > 500)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] <= UnitCountService.Count(UnitTypes.PROTOSS_NEXUS))
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS]++;
                }
            }
        }
    }
}
