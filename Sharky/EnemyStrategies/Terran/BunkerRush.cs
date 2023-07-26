namespace Sharky.EnemyStrategies.Terran
{
    public class BunkerRush : EnemyStrategy
    {
        TargetingData TargetingData;

        public BunkerRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot)
        {
            TargetingData = defaultSharkyBot.TargetingData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Terran) { return false; }

            if (frame < SharkyOptions.FramesPerSecond * 60 * 3)
            {
                if (ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER && Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), u.Position) < 900))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
