using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.Terran;
using Sharky.Chat;
using Sharky.MicroControllers;
using Sharky.MicroTasks;
using Sharky.Proxy;
using System.Collections.Generic;

namespace SharkyExampleBot.Builds
{
    public class ReaperCheese : TerranSharkyBuild
    {
        MicroTaskData MicroTaskData;
        ProxyLocationService ProxyLocationService;

        bool OpeningAttackChatSent;
        ProxyTask ProxyTask;

        public ReaperCheese(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, MicroTaskData microTaskData, UnitCountService unitCountService, SharkyUnitData sharkyUnitData, ProxyLocationService proxyLocationService, DebugService debugService, IIndividualMicroController scvMicroController) : base(buildOptions, macroData, activeUnitData, attackData, chatService, unitCountService)
        {
            MicroTaskData = microTaskData;
            ProxyLocationService = proxyLocationService;

            OpeningAttackChatSent = false;
            ProxyTask = new ProxyTask(sharkyUnitData, false, 0.9f, MacroData, string.Empty, MicroTaskData, debugService, scvMicroController);
            ProxyTask.ProxyName = GetType().Name;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            BuildOptions.StrictSupplyCount = true;
            BuildOptions.StrictWorkerCount = true;

            MacroData.DesiredUnitCounts[UnitTypes.TERRAN_SCV] = 15;

            var desiredUnitsClaim = new DesiredUnitsClaim(UnitTypes.TERRAN_REAPER, 1);
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

            MicroTaskData.MicroTasks["ReaperCheese"] = ProxyTask;
            var proxyLocation = ProxyLocationService.GetCliffProxyLocation();
            MacroData.Proxies[ProxyTask.ProxyName] = new ProxyData(proxyLocation, MacroData);
            ProxyTask.Enable();

            AttackData.CustomAttackFunction = true;
            AttackData.UseAttackDataManager = false;
        }

        void SetAttack()
        {
            if (UnitCountService.Completed(UnitTypes.TERRAN_REAPER) > 5)
            {
                AttackData.Attacking = true;
                if (!OpeningAttackChatSent)
                {
                    ChatService.SendChatType("ReaperCheese-FirstAttack");
                    OpeningAttackChatSent = true;
                }
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            SetAttack();

            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredSupplyDepots < 1)
                {
                    MacroData.DesiredSupplyDepots = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_SUPPLYDEPOT) > 0)
            {
                if (MacroData.Proxies[ProxyTask.ProxyName].DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] < 1)
                {
                    MacroData.Proxies[ProxyTask.ProxyName].DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] = 1;
                }
            }

            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_SUPPLYDEPOT) > 0 && UnitCountService.Count(UnitTypes.TERRAN_BARRACKS) > 0)
            {
                if (MacroData.DesiredGases < 1)
                {
                    MacroData.DesiredGases = 1;
                }

                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] < 3)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] = 3;
                }
            }

            if (UnitCountService.Completed(UnitTypes.TERRAN_BARRACKS) > 0)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.TERRAN_REAPER] < 10)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.TERRAN_REAPER] = 10;
                }

                if (MacroData.DesiredSupplyDepots < 2)
                {
                    MacroData.DesiredSupplyDepots = 2;
                }
            }

            if (UnitCountService.Completed(UnitTypes.TERRAN_BARRACKS) > 0 && UnitCountService.Count(UnitTypes.TERRAN_COMMANDCENTER) > 0 && UnitCountService.EquivalentTypeCompleted(UnitTypes.TERRAN_SUPPLYDEPOT) >= 2)
            {
                if (MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] < 1)
                {
                    MacroData.DesiredMorphCounts[UnitTypes.TERRAN_ORBITALCOMMAND] = 1;
                }

                BuildOptions.StrictSupplyCount = false;
            }

            if (UnitCountService.Count(UnitTypes.TERRAN_ORBITALCOMMAND) > 0)
            {
                BuildOptions.StrictWorkerCount = false;
                BuildOptions.StrictGasCount = false;
            }

            if (MacroData.Minerals > 500)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] <= UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER))
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER]++;
                }
            }
        }

        public override bool Transition(int frame)
        {
            if (UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) >= 2)
            {
                AttackData.UseAttackDataManager = true;
                ProxyTask.Disable();
                return true;
            }

            return false;
        }
    }
}
