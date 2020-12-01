using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using Sharky.MicroTasks;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharkyExampleBot.Builds
{
    public class ProxyVoidRay : ProtossSharkyBuild
    {
        SharkyOptions SharkyOptions;
        MicroManager MicroManager;
        EnemyRaceManager EnemyRaceManager;
        UnitDataManager UnitDataManager;

        bool OpeningAttackChatSent;
        bool CancelledProxyChatSent;

        public ProxyVoidRay(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager, NexusManager nexusManager, SharkyOptions sharkyOptions, MicroManager microManager, EnemyRaceManager enemyRaceManager, ICounterTransitioner counterTransitioner, UnitDataManager unitDataManager) : base(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, counterTransitioner)
        {
            SharkyOptions = sharkyOptions;
            MicroManager = microManager;
            EnemyRaceManager = enemyRaceManager;
            UnitDataManager = unitDataManager;

            OpeningAttackChatSent = false;
            CancelledProxyChatSent = false;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictSupplyCount = true;
            BuildOptions.StrictWorkerCount = true;

            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_PROBE] = 23;

            NexusManager.ChronodUnits = new HashSet<UnitTypes>
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
        }

        public override void OnFrame(ResponseObservation observation)
        {
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
            if (UnitManager.Count(UnitTypes.PROTOSS_GATEWAY) > 0)
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
                if (NexusManager.ChronodUnits.Contains(UnitTypes.PROTOSS_PROBE))
                {
                    NexusManager.ChronodUnits.Remove(UnitTypes.PROTOSS_PROBE);
                }
            }
            if (UnitManager.Completed(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
                // TODO: start the ProxyTask
            }
            if (UnitManager.Completed(UnitTypes.PROTOSS_CYBERNETICSCORE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 1;
                }

                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] < 1)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_STARGATE] = 1;
                }
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_STARGATE) > 0)
            {
                BuildOptions.StrictSupplyCount = false;
                MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;

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

                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] < 10 && MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] <= UnitManager.Count(UnitTypes.PROTOSS_ZEALOT) && MacroData.Minerals > 350)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT]++;
                    }
                }
            }
            if (UnitManager.Completed(UnitTypes.PROTOSS_STARGATE) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] < 10)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_VOIDRAY] = 10;
                }
            }

            if (MacroData.Frame > SharkyOptions.FramesPerSecond * 4 * 60)
            {
                // TODO: end proxy task
            }

            if (!OpeningAttackChatSent && MacroData.FoodArmy > 10)
            {
                ChatManager.SendChatType("ProxyVoidRay-FirstAttack");
                OpeningAttackChatSent = true;
            }
        }

        public override bool Transition(int frame)
        {
            bool transition = false;

            // TODO: proxy task location
            var proxyLocation = new Point2D { X = 200, Y = 200 };
            if (UnitManager.Count(UnitTypes.PROTOSS_STARGATE) < 1)
            {
                if (UnitManager.EnemyUnits.Any(e => Vector2.DistanceSquared(new Vector2(e.Value.Unit.Pos.X, e.Value.Unit.Pos.Y), new Vector2(proxyLocation.X, proxyLocation.Y)) < 100))
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
                // TODO: end proxy task
                return true;
            }

            return false;
        }
    }
}
