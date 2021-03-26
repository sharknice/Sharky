using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky
{
    public class DamageService
    {
        public bool CanDamage(UnitCalculation attacker, UnitCalculation victim)
        {
            if (attacker.Damage == 0)
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
