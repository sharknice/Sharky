using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Protoss
{
    public class PhoenixMicroController : IndividualMicroController
    {
        public PhoenixMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, IUnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, IBaseManager baseManager, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled)
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, baseManager, sharkyOptions, microPriority, groupUpEnabled)
        {
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            if (!commander.UnitCalculation.TargetPriorityCalculation.Overwhelm && MicroPriority != MicroPriority.AttackForward && commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.Retreat)
            {
                action = commander.Order(frame, Abilities.MOVE, defensivePoint); // no damaging targets in range, retreat towards the defense point
                return true;
            }

            if (bestTarget != null && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && MicroPriority != MicroPriority.NavigateToLocation)
            {
                action = commander.Order(frame, Abilities.MOVE, GetPositionFromRange(bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range));
                return true;
            }

            if (GroupUpEnabled && GroupUp(commander, target, groupCenter, true, frame, out action))
            {
                return true;
            }

            if (bestTarget != null && MicroPriority != MicroPriority.NavigateToLocation)
            {
                action = commander.Order(frame, Abilities.MOVE, GetPositionFromRange(bestTarget.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range));
                return true;
            }

            action = commander.Order(frame, Abilities.ATTACK, target); // no damaging targets in range, attack towards the main target
            return true;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out SC2APIProtocol.Action action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Energy < 50)
            {
                return false;
            }

            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM))) // only have one unit lifted at a time
            {
                return false;
            }

            var bestGravitonTarget = GetBestGravitonBeamTarget(commander, target);
            if (bestGravitonTarget != null)
            {
                commander.Order(frame, Abilities.EFFECT_GRAVITONBEAM, null, bestGravitonTarget.Unit.Tag);
                return true;
            }

            return false;
        }

        UnitCalculation GetBestGravitonBeamTarget(UnitCommander commander, Point2D target)
        {
            var existingOrder = commander.UnitCalculation.Unit.Orders.Where(o => o.AbilityId == (uint)Abilities.EFFECT_GRAVITONBEAM).FirstOrDefault();

            var range = 4;

            var attacks = new List<UnitCalculation>(commander.UnitCalculation.EnemiesInRange.Where(u => u.Unit.DisplayType != DisplayType.Hidden && !u.Unit.IsFlying && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM) && InRange(u.Unit.Pos, commander.UnitCalculation.Unit.Pos, range))); // units that are in range right now

            if (attacks.Count > 0)
            {
                var bestAttack = GetBestTargetFromList(commander, attacks, existingOrder);
                if (bestAttack != null)
                {
                    return bestAttack;
                }
            }

            attacks = new List<UnitCalculation>(commander.UnitCalculation.NearbyEnemies.Where(u => u.Unit.DisplayType != DisplayType.Hidden && !u.Unit.IsFlying && !u.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !u.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && !u.Unit.BuffIds.Contains((uint)Buffs.GRAVITONBEAM) && !InRange(u.Unit.Pos, commander.UnitCalculation.Unit.Pos, range))); // units that are not in range right now

            if (attacks.Count > 0)
            {
                var bestOutOfRangeAttack = GetBestTargetFromList(commander, attacks, existingOrder);
                if (bestOutOfRangeAttack != null)
                {
                    return bestOutOfRangeAttack;
                }
            }

            return null;
        }
    }
}
