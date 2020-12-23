using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Sharky
{
    public class DamageService
    {
        public bool CanDamage(IEnumerable<Weapon> weapons, Unit unit)
        {
            if (weapons.Count() == 0 || weapons.All(w => w.Damage == 0))
            {
                return false;
            }
            if ((unit.IsFlying || unit.UnitType == (uint)UnitTypes.PROTOSS_COLOSSUS || unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM)) && weapons.Any(w => w.Type == Weapon.Types.TargetType.Air || w.Type == Weapon.Types.TargetType.Any))
            {
                return true;
            }
            if (!unit.IsFlying && weapons.Any(w => w.Type == Weapon.Types.TargetType.Ground || w.Type == Weapon.Types.TargetType.Any))
            {
                return true;
            }
            return false;
        }
    }
}
