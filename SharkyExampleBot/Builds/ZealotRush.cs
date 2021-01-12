using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using System.Collections.Generic;

namespace SharkyExampleBot.Builds
{
    public class ZealotRush : ProtossSharkyBuild
    {
        bool OpeningAttackChatSent;

        public ZealotRush(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, ICounterTransitioner counterTransitioner, UnitCountService unitCountService) : base(buildOptions, macroData, activeUnitData, attackData, chatService, chronoData, counterTransitioner, unitCountService)
        {
            OpeningAttackChatSent = false;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;

            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 100;

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ZEALOT
            };
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) >= 2)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 4)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 4;
                }
            }

            if (!OpeningAttackChatSent && MacroData.FoodArmy > 10)
            {
                ChatService.SendChatType("ZealotRush-FirstAttack");
                OpeningAttackChatSent = true;
            }
        }

        public override List<string> CounterTransition(int frame)
        {
            return new List<string>();
        }
    }
}
