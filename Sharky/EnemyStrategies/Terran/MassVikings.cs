using Sharky.Chat;
using Sharky.Managers;

namespace Sharky.EnemyStrategies.Terran
{
    public class MassVikings : EnemyStrategy
    {
        public MassVikings(EnemyStrategyHistory enemyStrategyHistory, ChatService chatService, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, DebugService debugService, UnitCountService unitCountService, FrameToTimeConverter frameToTimeConverter)
        {
            EnemyStrategyHistory = enemyStrategyHistory;
            ChatService = chatService;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
            UnitCountService = unitCountService;
            FrameToTimeConverter = frameToTimeConverter;
        }

        protected override bool Detect(int frame)
        {
            if (UnitCountService.EnemyCount(UnitTypes.TERRAN_VIKINGASSAULT) + UnitCountService.EnemyCount(UnitTypes.TERRAN_VIKINGFIGHTER) >= 8)
            {
                return true;
            }

            return false;
        }
    }
}
