using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers
{
    public class MineralWalker
    {
        BaseData BaseData;
        SharkyUnitData SharkyUnitData;
        ActiveUnitData ActiveUnitData;

        public MineralWalker(BaseData baseData, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData)
        {
            BaseData = baseData;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
        }

        public bool MineralWalkHome(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var selfBase = BaseData.SelfBases.FirstOrDefault();
            if (selfBase != null)
            {
                var mineralPatch = selfBase.MineralFields.FirstOrDefault();
                if (mineralPatch != null && Vector2.DistanceSquared(new Vector2(mineralPatch.Pos.X, mineralPatch.Pos.Y), commander.UnitCalculation.Position) > 9)
                {
                    action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Tag);
                    return true;
                }
            }

            return false;
        }

        public bool MineralWalkTarget(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var mineralFields = ActiveUnitData.NeutralUnits.Values.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Unit.UnitType));
            var mineralPatch = mineralFields.OrderBy(m => Vector2.DistanceSquared(new Vector2(BaseData.EnemyBaseLocations.FirstOrDefault().Location.X, BaseData.EnemyBaseLocations.FirstOrDefault().Location.Y), m.Position)).FirstOrDefault();
            if (mineralPatch != null)
            {
                action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Unit.Tag);
                return true;
            }

            return false;
        }

        public bool MineralWalkNoWhere(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var selfLocation = BaseData.BaseLocations.FirstOrDefault().Location;
            var enemyLocation = BaseData.EnemyBaseLocations.FirstOrDefault().Location;
            var selfVector = new Vector2(selfLocation.X, selfLocation.Y);
            var enemyVector = new Vector2(enemyLocation.X, enemyLocation.Y);
            var selfBase = BaseData.BaseLocations.OrderByDescending(x => Vector2.Distance(selfVector, new Vector2(x.Location.X, x.Location.Y)) + Vector2.Distance(enemyVector, new Vector2(x.Location.X, x.Location.Y))).Where(x => Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(x.Location.X, x.Location.Y)) > 100).FirstOrDefault();
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
