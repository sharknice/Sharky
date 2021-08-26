using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers
{
    public class MineralWalker
    {
        BaseData BaseData;

        public MineralWalker(BaseData baseData)
        {
            BaseData = baseData;
        }

        public bool MineralWalkHome(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var selfBase = BaseData.SelfBases.FirstOrDefault();
            if (selfBase != null)
            {
                var mineralPatch = selfBase.MineralFields.FirstOrDefault();
                if (mineralPatch != null)
                {
                    action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Tag);
                    return true;
                }
            }

            return false;
        }
    }
}
