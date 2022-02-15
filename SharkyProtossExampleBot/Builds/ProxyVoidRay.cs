using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using Sharky.MicroTasks;
using Sharky.Proxy;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharkyProtossExampleBot.Builds
{
    public class ProxyVoidRay : ProtossSharkyBuild
    {
        SharkyOptions SharkyOptions;
        SharkyUnitData SharkyUnitData;
        ProxyLocationService ProxyLocationService;

        bool OpeningAttackChatSent;
        bool CancelledProxyChatSent;

        ProxyTask ProxyTask;

        public ProxyVoidRay(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner, IIndividualMicroController probeMicroController)
            : base(defaultSharkyBot, counterTransitioner)
        {
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            ProxyLocationService = defaultSharkyBot.ProxyLocationService;

            OpeningAttackChatSent = false;
            CancelledProxyChatSent = false;

            ProxyTask = new ProxyTask(SharkyUnitData, false, 0.9f, MacroData, string.Empty, MicroTaskData, defaultSharkyBot.DebugService, defaultSharkyBot.ActiveUnitData, probeMicroController);
            ProxyTask.ProxyName = GetType().Name;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictSupplyCount = true;
            BuildOptions.StrictWorkerCount = true;

            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_PROBE] = 23;

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
                UnitTypes.PROTOSS_VOIDRAY,
            };

            var defenseSquadTask = (DefenseSquadTask)MicroTaskData.MicroTasks["DefenseSquadTask"];
            defenseSquadTask.DesiredUnitsClaims = new List<DesiredUnitsClaim> { new DesiredUnitsClaim(UnitTypes.PROTOSS_STALKER, 1) };
            defenseSquadTask.Enable();

            MicroTaskData.MicroTasks[ProxyTask.ProxyName] = ProxyTask;
            var proxyLocation = ProxyLocationService.GetCliffProxyLocation();
            MacroData.Proxies[ProxyTask.ProxyName] = new ProxyData(proxyLocation, MacroData);

            AttackData.UseAttackDataManager = false;
            AttackData.CustomAttackFunction = true;
        }

        void SetAttack()
        {
            if (UnitCountService.Completed(UnitTypes.PROTOSS_VOIDRAY) > 1)
            {
                AttackData.Attacking = true;
                if (!OpeningAttackChatSent)
                {
                    ChatService.SendChatType("ProxyVoidRay-FirstAttack");
                    OpeningAttackChatSent = true;
                }
            }
            else if (UnitCountService.Completed(UnitTypes.PROTOSS_VOIDRAY) == 0)
            {
                AttackData.Attacking = false;
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            var frame = (int)observation.Observation.GameLoop;

            SetAttack();

            SendProbeForFirstPylon(frame);
            SendProbeForFirstGateway(frame);
            SendProbeForCyberneticsCore(frame);

            if (MacroData.FoodUsed >= 14)
            {
                if (MacroData.DesiredPylons < 1)
                {
                    MacroData.DesiredPylons = 1;
                }
            }
            if (MacroData.FoodUsed >= 16)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
                }
            }
            if (UnitCountService.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredGases < 2)
                {
                    MacroData.DesiredGases = 2;
                }
            }
            if (MacroData.FoodUsed >= 19)
            {
                if (MacroData.DesiredPylons < 2)
                {
                    MacroData.DesiredPylons = 2;
                }
                if (ChronoData.ChronodUnits.Contains(UnitTypes.PROTOSS_PROBE))
                {
                    ChronoData.ChronodUnits.Remove(UnitTypes.PROTOSS_PROBE);
                }
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                ProxyTask.Enable();
                MacroData.Proxies[ProxyTask.ProxyName].DesiredPylons = 1;
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 1;
                }

                if (MacroData.Proxies[ProxyTask.ProxyName].DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] < 2)
                {
                    MacroData.Proxies[ProxyTask.ProxyName].DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] = 2;
                }
            }

            if (UnitCountService.Count(UnitTypes.PROTOSS_STARGATE) > 0)
            {
                BuildOptions.StrictSupplyCount = false;
                MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;

                MacroData.Proxies[ProxyTask.ProxyName].DesiredPylons = 2;
                if (MacroData.Proxies[ProxyTask.ProxyName].DesiredDefensiveBuildingsCounts[UnitTypes.PROTOSS_SHIELDBATTERY] < 2)
                {
                    MacroData.Proxies[ProxyTask.ProxyName].DesiredDefensiveBuildingsCounts[UnitTypes.PROTOSS_SHIELDBATTERY] = 2;
                }

                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }

                if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.WARPGATERESEARCH))
                {
                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 3)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 3;
                    }

                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 10 && MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] <= UnitCountService.Count(UnitTypes.PROTOSS_ZEALOT) && MacroData.Minerals > 350)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT]++;
                    }
                }
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_STARGATE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] < 10)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] = 10;
                }
            }

            if (MacroData.Frame > SharkyOptions.FramesPerSecond * 4 * 60)
            {
                ProxyTask.Disable();
            }
        }

        public override bool Transition(int frame)
        {
            if (UnitCountService.Count(UnitTypes.PROTOSS_STARGATE) < 1)
            {
                if (ActiveUnitData.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y), new Vector2(MacroData.Proxies[ProxyTask.ProxyName].Location.X, MacroData.Proxies[ProxyTask.ProxyName].Location.Y)) < 100))
                {
                    return true;
                }
            }

            if (MacroData.Frame > SharkyOptions.FramesPerSecond * 8 * 60)
            {
                return true;
            }

            return false;
        }

        public override void EndBuild(int frame)
        {
            if (!CancelledProxyChatSent)
            {
                ChatService.SendChatType("ProxyVoidRay-CancelledAttack");
                CancelledProxyChatSent = true;
            }
            ProxyTask.Disable();
        }
    }
}
