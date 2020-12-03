using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using System.Collections.Generic;

namespace SharkyExampleBot.Builds
{
    public class ZealotRush : ProtossSharkyBuild
    {
        bool OpeningAttackChatSent;

        public ZealotRush(BuildOptions buildOptions, MacroData macroData, IUnitManager unitManager, AttackData attackData, IChatManager chatManager, NexusManager nexusManager, ICounterTransitioner counterTransitioner) : base(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager, counterTransitioner)
        {
            OpeningAttackChatSent = false;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            AttackData.ArmyFoodAttack = 8;

            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 100;

            NexusManager.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ZEALOT
            };
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (UnitManager.Completed(UnitTypes.PROTOSS_PYLON) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }
            }
            if (UnitManager.Completed(UnitTypes.PROTOSS_PYLON) >= 2)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 4)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 4;
                }
            }

            if (!OpeningAttackChatSent && MacroData.FoodArmy > 10)
            {
                ChatManager.SendChatType("ZealotRush-FirstAttack");
                OpeningAttackChatSent = true;
            }
        }
    }
}
