using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class ObserverMicroController : IndividualMicroController
    {
        public ObserverMicroController(MapDataService mapDataService, SharkyUnitData unitDataManager, ActiveUnitData activeUnitData, DebugService debugService, IPathFinder sharkyPathFinder, BaseData baseData, SharkyOptions sharkyOptions, DamageService damageService, UnitDataService unitDataService, TargetingData targetingData, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, activeUnitData, debugService, sharkyPathFinder, baseData, sharkyOptions, damageService, unitDataService, targetingData, microPriority, groupUpEnabled)
        {
        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax)
            {
                var cloakedPosition = CloakedInvader(commander);
                if (cloakedPosition != null)
                {
                    action = commander.Order(frame, Abilities.EFFECT_ORACLEREVELATION, cloakedPosition);
                    return true;
                }
            }

            if (AvoidTargettedDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            if (AvoidDamage(commander, target, defensivePoint, frame, out action))
            {
                return true;
            }

            return false;
        }

        Point2D CloakedInvader(UnitCommander commander)
        {
            var pos = commander.UnitCalculation.Position;

            var hiddenUnits = ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.DisplayType == DisplayType.Hidden).OrderBy(e => Vector2.DistanceSquared(pos, e.Value.Position));
            if (hiddenUnits.Count() > 0)
            {
                return new Point2D { X = hiddenUnits.FirstOrDefault().Value.Unit.Pos.X, Y = hiddenUnits.FirstOrDefault().Value.Unit.Pos.Y };
            }

            return null;
        }

        protected override bool WeaponReady(UnitCommander commander)
        {
            return false;
        }
    }
}
