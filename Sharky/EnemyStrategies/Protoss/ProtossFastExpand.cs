namespace Sharky.EnemyStrategies.Protoss
{
    public class ProtossFastExpand : EnemyStrategy
    {
        TargetingData TargetingData;

        public ProtossFastExpand(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != Race.Protoss) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 3 * 60)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && (e.Unit.Pos.X != TargetingData.EnemyMainBasePoint.X || e.Unit.Pos.Y != TargetingData.EnemyMainBasePoint.Y) 
                    && Vector2.Distance(e.Position, TargetingData.SelfMainBasePoint.ToVector2()) > 25))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
