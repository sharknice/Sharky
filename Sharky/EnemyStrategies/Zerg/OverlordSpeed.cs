namespace Sharky.EnemyStrategies.Zerg
{
    public class OverlordSpeed : EnemyStrategy
    {
        SharkyUnitData SharkyUnitData;

        public OverlordSpeed(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot) 
        {
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
        }

        protected override bool Detect(int frame)
        {
            if (EnemyData.EnemyRace != SC2APIProtocol.Race.Zerg) { return false; }
            if (Detected) { return true; }

            var overlord = ActiveUnitData.EnemyUnits.Values.FirstOrDefault(u => u.FrameLastSeen == frame && u.Velocity > 0 && (u.Unit.UnitType == (int)UnitTypes.ZERG_OVERLORD || u.Unit.UnitType == (int)UnitTypes.ZERG_OVERSEER || u.Unit.UnitType == (int)UnitTypes.ZERG_OVERLORDTRANSPORT));
            if (overlord != null)
            {
                if (overlord.Velocity * 16f > overlord.UnitTypeData.MovementSpeed + 1f)
                {
                    SharkyUnitData.UnitData[UnitTypes.ZERG_OVERLORD].MovementSpeed += SharkyUnitData.UnitData[UnitTypes.ZERG_OVERLORD].MovementSpeed * 1.9157f;
                    SharkyUnitData.UnitData[UnitTypes.ZERG_OVERLORDTRANSPORT].MovementSpeed += SharkyUnitData.UnitData[UnitTypes.ZERG_OVERLORDTRANSPORT].MovementSpeed * 1.9157f;
                    SharkyUnitData.UnitData[UnitTypes.ZERG_OVERSEER].MovementSpeed += SharkyUnitData.UnitData[UnitTypes.ZERG_OVERSEER].MovementSpeed * .8015f;
                    return true;
                }
            }

            return false;
        }
    }
}
