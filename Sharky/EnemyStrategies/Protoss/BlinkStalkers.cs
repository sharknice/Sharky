namespace Sharky.EnemyStrategies
{
    public class BlinkStalkers : EnemyStrategy
    {
        public BlinkStalkers(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Protoss) { return false; }

            if (Detected) { return true; }

            if (ActiveUnitData.EnemyUnits.Values.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_STALKER && e.FrameLastSeen == frame && e.PreviousUnitCalculation != null && e.PreviousUnitCalculation.FrameLastSeen == frame - 1 && Vector2.DistanceSquared(e.Position, e.PreviousUnitCalculation.Position) > 9))
            {
                return true;
            }

            return false;
        }
    }
}
