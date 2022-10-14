using Sharky.DefaultBot;
using System.Linq;

namespace Sharky.EnemyStrategies.Protoss
{
    public class AdeptRush : EnemyStrategy
    {
        bool Expired = false;

        public AdeptRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (Expired) { return false; }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_ADEPT))
            {
                Expired = true;
                return false;
            }

            if (UnitCountService.EnemyCount(UnitTypes.PROTOSS_ADEPT) >= 4 && frame < SharkyOptions.FramesPerSecond * 5 * 60)
            {
                return true;
            }

            return false;
        }
    }
}
