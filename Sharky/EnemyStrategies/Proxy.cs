namespace Sharky.EnemyStrategies
{
    public class Proxy : EnemyStrategy
    {
        TargetingData TargetingData;

        public Proxy(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (frame < SharkyOptions.FramesPerSecond * 60 * 5)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && u.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE && u.Unit.UnitType != (uint)UnitTypes.PROTOSS_ORACLESTASISTRAP && u.Unit.UnitType != (uint)UnitTypes.TERRAN_AUTOTURRET && Vector2.DistanceSquared(new Vector2(TargetingData.EnemyMainBasePoint.X, TargetingData.EnemyMainBasePoint.Y), u.Position) > (75 * 75)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
