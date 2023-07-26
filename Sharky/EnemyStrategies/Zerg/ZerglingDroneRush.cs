namespace Sharky.EnemyStrategies.Zerg
{
    public class ZerglingDroneRush : EnemyStrategy
    {
        public ZerglingDroneRush(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) { }

        private bool IsUnitAggressive(Unit unit)
        {
            // if ground distance is more than 30 units from nearest enemy base
            return EnemyData.EnemyAggressivityData.DistanceGrid.GetDist(unit.Pos.X, unit.Pos.Y, false, true) >= 20;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }

            if (frame <= SharkyOptions.FramesPerSecond * 60 * 2.5f
                && EnemyData.EnemyStrategies[nameof(ZerglingRush)].Detected
                && ActiveUnitData.EnemyUnits.Values.Count(u => u.Unit.UnitType == (int)UnitTypes.ZERG_DRONE && IsUnitAggressive(u.Unit)) >= 3)
            {
                return true;
            }

            return false;
        }
    }
}
