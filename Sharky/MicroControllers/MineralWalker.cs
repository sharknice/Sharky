using SC2APIProtocol;
using Sharky.Extensions;
using Sharky.Pathing;
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
        MapDataService MapDataService;

        BaseLocation DistractionBase;

        public MineralWalker(BaseData baseData, SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, MapDataService mapDataService)
        {
            BaseData = baseData;
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            MapDataService = mapDataService;
        }

        public bool MineralWalkHome(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action, bool spam = false, bool queue = false)
        {
            action = null;

            var selfBase = BaseData.SelfBases.FirstOrDefault();
            if (selfBase != null)
            {
                var mineralPatch = selfBase.MineralFields.FirstOrDefault();
                if (mineralPatch != null && Vector2.DistanceSquared(new Vector2(mineralPatch.Pos.X, mineralPatch.Pos.Y), commander.UnitCalculation.Position) > 9)
                {
                    action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Tag, allowSpam: spam, queue: queue);
                    return true;
                }
            }

            return false;
        }

        public UnitCalculation GetTargetMineralPatch(int skip = 4)
        {
            var mineralFields = ActiveUnitData.NeutralUnits.Values.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Unit.UnitType));
            var ordered = mineralFields.OrderBy(m => Vector2.DistanceSquared(new Vector2(BaseData.EnemyBaseLocations.FirstOrDefault().MiddleMineralLocation.X, BaseData.EnemyBaseLocations.FirstOrDefault().MiddleMineralLocation.Y), m.Position));
            var mineralPatch = ordered.Skip(skip).FirstOrDefault();
            if (mineralPatch != null)
            {
                return mineralPatch;
            }
            return ordered.FirstOrDefault();
        }

        public UnitCalculation GetEnemyNaturalMineralPatch(int skip = 4)
        {
            var mineralFields = ActiveUnitData.NeutralUnits.Values.Where(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Unit.UnitType));
            var ordered = mineralFields.OrderBy(m => Vector2.DistanceSquared(new Vector2(BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().MiddleMineralLocation.X, BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().MiddleMineralLocation.Y), m.Position));
            var mineralPatch = ordered.Skip(skip).FirstOrDefault();
            if (mineralPatch != null)
            {
                return mineralPatch;
            }
            return ordered.FirstOrDefault();
        }

        public bool MineralWalkPatch(UnitCommander commander, UnitCalculation mineralPatch, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (mineralPatch != null)
            {
                action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Unit.Tag, allowSpam: true);
                return true;
            }

            return false;
        }

        public bool MineralWalkTarget(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var mineralPatch = GetTargetMineralPatch();
            if (mineralPatch != null)
            {
                action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Unit.Tag, allowSpam: true);
                return true;
            }

            return false;
        }

        public bool MineralWalkNoWhere(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var distractionBase = GetNextDistractionBase();
            if (distractionBase != null)
            {              
                var mineralPatch = distractionBase.MineralFields.FirstOrDefault();
                if (mineralPatch != null)
                {
                    action = commander.Order(frame, Abilities.HARVEST_GATHER, null, mineralPatch.Tag);
                    return true;
                }
            }

            return false;
        }

        BaseLocation GetNextDistractionBase()
        {
            var selfBase = BaseData.BaseLocations.FirstOrDefault();
            var enemyBase = BaseData.EnemyBaseLocations.FirstOrDefault();

            if (DistractionBase == null)
            {
                var selfVector = selfBase.Location.ToVector2();
                var enemyVector = enemyBase.Location.ToVector2();
                DistractionBase = BaseData.BaseLocations.OrderByDescending(b => Vector2.Distance(selfVector, b.Location.ToVector2()) + Vector2.Distance(enemyVector, b.Location.ToVector2())).FirstOrDefault(b => !ActiveUnitData.NeutralUnits.Values.Any(u => u.UnitTypeData.Name.Contains("MineralField") && Vector2.DistanceSquared(u.Position, b.Location.ToVector2()) < 4));
            }
            var distractionBaseLastSeen = MapDataService.LastFrameVisibility(DistractionBase.Location);

            List<BaseLocation> alreadyUsed = new List<BaseLocation>
            {
                BaseData.BaseLocations.Skip(1).FirstOrDefault(),
                BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault()
            };
            alreadyUsed.AddRange(BaseData.BaseLocations.Where(b => ActiveUnitData.NeutralUnits.Values.Any(u => u.UnitTypeData.Name.Contains("MineralField") && Vector2.DistanceSquared(u.Position, b.Location.ToVector2()) < 4)));
            var currentBase = DistractionBase;
            for (int count = 0; count < 10; count++)
            {
                var currentBaseLastSeen = MapDataService.LastFrameVisibility(currentBase.Location);
                if (currentBaseLastSeen < 100)
                {
                    return currentBase;
                }
                alreadyUsed.Add(currentBase);
                var nextClosest = BaseData.BaseLocations.OrderBy(b => Vector2.Distance(currentBase.Location.ToVector2(), b.Location.ToVector2())).FirstOrDefault(b => b != currentBase && b != selfBase && b != enemyBase && !alreadyUsed.Contains(b));
                if (nextClosest == null)
                {
                    return DistractionBase;
                }
                var nextBaseLastSeen = MapDataService.LastFrameVisibility(nextClosest.Location);
                if (nextBaseLastSeen <= currentBaseLastSeen && nextBaseLastSeen <= distractionBaseLastSeen)
                {
                    return nextClosest;
                }
                alreadyUsed.Add(nextClosest);
                currentBase = nextClosest;
            }

            return DistractionBase;
        }
    }
}
