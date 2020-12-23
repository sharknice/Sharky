using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using Sharky.MicroControllers;
using Sharky.MicroTasks;
using Sharky.Proxy;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharkyExampleBot.Builds
{
    public class ProxyVoidRay : ProtossSharkyBuild
    {
        SharkyOptions SharkyOptions;
        MicroManager MicroManager;
        UnitDataManager UnitDataManager;
        ProxyLocationService ProxyLocationService;

        bool OpeningAttackChatSent;
        bool CancelledProxyChatSent;

        ProxyTask ProxyTask;

        public ProxyVoidRay(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, IChatManager chatManager, ChronoData nexusManager, SharkyOptions sharkyOptions, MicroManager microManager, ICounterTransitioner counterTransitioner, UnitDataManager unitDataManager, ProxyLocationService proxyLocationService, DebugManager debugManager, UnitCountService unitCountService, IIndividualMicroController probeMicroController) 
            : base(buildOptions, macroData, activeUnitData, attackData, chatManager, nexusManager, counterTransitioner, unitCountService)
        {
            SharkyOptions = sharkyOptions;
            MicroManager = microManager;
            UnitDataManager = unitDataManager;
            ProxyLocationService = proxyLocationService;

            OpeningAttackChatSent = false;
            CancelledProxyChatSent = false;

            ProxyTask = new ProxyTask(UnitDataManager, false, 0.9f, MacroData, string.Empty, MicroManager, debugManager, probeMicroController);
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

            var desiredUnitsClaim = new DesiredUnitsClaim(UnitTypes.PROTOSS_STALKER, 1);

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

            MicroManager.MicroTasks["ProxyVoidRay"] = ProxyTask;   
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
                    ChatManager.SendChatType("ProxyVoidRay-FirstAttack");
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
            SetAttack();

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

                if (MacroData.Proxies[ProxyTask.ProxyName].DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] < 1)
                {
                    MacroData.Proxies[ProxyTask.ProxyName].DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] = 1;
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

                if (UnitDataManager.ResearchedUpgrades.Contains((uint)Upgrades.WARPGATERESEARCH))
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
            bool transition = false;

            if (UnitCountService.Count(UnitTypes.PROTOSS_STARGATE) < 1)
            {
                if (ActiveUnitData.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y), new Vector2(MacroData.Proxies[ProxyTask.ProxyName].Location.X, MacroData.Proxies[ProxyTask.ProxyName].Location.Y)) < 100))
                {
                    if (!CancelledProxyChatSent)
                    {
                        ChatManager.SendChatType("ProxyVoidRay-CancelledAttack");
                        CancelledProxyChatSent = true;
                    }
                    transition = true;
                }
            }

            if (MacroData.Frame > SharkyOptions.FramesPerSecond * 8 * 60)
            {
                transition = true;
            }

            if (transition)
            {
                ProxyTask.Disable();
                return true;
            }

            return false;
        }
    }
}
