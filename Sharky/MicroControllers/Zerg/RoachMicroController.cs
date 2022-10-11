using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.MicroControllers.Zerg
{
    public class RoachMicroController : IndividualMicroController
    {
        private SharkyUnitData UnitData;

        public RoachMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled, SharkyUnitData unitData)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            UnitData = unitData;
        }

        protected override bool AvoidPointlessDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<Action> action)
        {
            action = null;

            if (commander.UnitCalculation.EnemiesThreateningDamage.All(e => e.Range > 1))
            {
                return false;
            }

            return base.AvoidPointlessDamage(commander, target, defensivePoint, frame, out action);
        }

        public override List<Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return ManageBurrow(commander, defensivePoint, frame) ?? base.Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        protected override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<Action> action)
        {
            action = ManageBurrow(commander, defensivePoint, frame);
            if (action is null) return base.Retreat(commander, target, defensivePoint, frame, out action);
            return true;
        }

        public override List<Action> Idle(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            return ManageBurrow(commander, defensivePoint, frame) ?? base.Idle(commander, defensivePoint, frame);
        }

        private List<Action> ManageBurrow(UnitCommander commander, Point2D defensivePoint, int frame)
        {
            if (commander.UnitCalculation.Unit.IsBurrowed)
            {
                if ((Detected(commander) && commander.UnitCalculation.NearbyAllies.Count < 5) || (commander.UnitCalculation.Unit.Health > commander.UnitCalculation.Unit.HealthMax * 0.9f) || (commander.UnitCalculation.EnemiesInRangeOf.Count == 0 && commander.UnitCalculation.Unit.Health > commander.UnitCalculation.Unit.HealthMax * 0.8f))
                {
                    return commander.Order(frame, Abilities.BURROWUP_ROACH);
                }
                else
                {
                    List<Action> actions;
                    base.Retreat(commander, defensivePoint, defensivePoint, frame, out actions);
                    return actions;
                }
            }
            else
            {
                if (UnitData.ResearchedUpgrades.Contains((uint)Upgrades.BURROW)
                    && commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax * 0.3f
                    && (!commander.UnitCalculation.NearbyEnemies.Any(x => x.UnitClassifications.Contains(UnitClassification.Detector)) || commander.UnitCalculation.EnemiesInRangeOf.Count <= 2 || commander.UnitCalculation.NearbyAllies.Count > 2))
                {
                    return commander.Order(frame, Abilities.BURROWDOWN_ROACH);
                }
            }

            return null;
        }

        protected override float GetMovementSpeed(UnitCommander commander)
        {
            var speed = commander.UnitCalculation.UnitTypeData.MovementSpeed * 1.4f;
            if (SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.GLIALRECONSTITUTION))
            {
                speed += 1.05f;
            }
            if (commander.UnitCalculation.IsOnCreep)
            {
                speed *= 1.3f;
            }
            return speed;
        }
    }
}
