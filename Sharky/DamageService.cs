namespace Sharky
{
    public class DamageService
    {
        public bool CanDamage(UnitCalculation attacker, UnitCalculation victim)
        {
            if (victim.Unit.UnitType == (uint)UnitTypes.TERRAN_KD8CHARGE) { return false; }

            if (attacker.Damage == 0 || attacker.Unit.BuildProgress < 1 || attacker.Unit.BuffIds.Contains((uint)Buffs.ORACLESTASISTRAPTARGET) || victim.Unit.BuffIds.Contains((uint)Buffs.ORACLESTASISTRAPTARGET) || attacker.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM))
            {
                return false;
            }
            if (attacker.DamageAir && (victim.Unit.IsFlying || victim.Unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || victim.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)))
            {
                return true;
            }
            if (attacker.DamageGround && !victim.Unit.IsFlying)
            {
                return true;
            }
            return false;
        }
    }
}
