namespace Sharky.EnemyStrategies
{
    public class CompletedProxy : EnemyStrategy
    {
        TargetingData TargetingData;

        public CompletedProxy(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (TargetingData.EnemyMainBasePoint == null) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && u.Unit.BuildProgress == 1 && u.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), u.Position) > (75 * 75)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
