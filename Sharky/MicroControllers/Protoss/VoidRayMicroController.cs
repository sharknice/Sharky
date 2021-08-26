using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class VoidRayMicroController : IndividualMicroController
    {
        public VoidRayMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder, MicroPriority microPriority, bool groupUpEnabled)
            :base(defaultSharkyBot, pathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool WeaponReady(UnitCommander commander, int frame)
        {
            return true;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Shield == commander.UnitCalculation.Unit.ShieldMax) { return false; }

            var dps = commander.UnitCalculation.Attackers.Sum(a => a.Dps);
            var hp = commander.UnitCalculation.Attackers.Sum(a => a.Unit.Health + a.Unit.Shield);
            if (dps <= 0 || hp <= 0) { return false; }

            var timeToLoseShield = commander.UnitCalculation.Unit.Shield / dps;
            var timeToKill = hp / commander.UnitCalculation.Dps;

            if (timeToLoseShield > timeToKill)
            {
                return false;
            }

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.AbilityOffCooldown(Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.ATTACK || o.AbilityId == (uint)Abilities.ATTACK_ATTACK))
                {
                    foreach (var tag in commander.UnitCalculation.Unit.Orders.Select(o => o.TargetUnitTag))
                    {
                        UnitCalculation unit;
                        if (ActiveUnitData.EnemyUnits.TryGetValue(tag, out unit))
                        {
                            if (unit.Attributes.Contains(Attribute.Armored))
                            {
                                if (commander.UnitCalculation.EnemiesInRange.Where(e => e.Attributes.Contains(Attribute.Armored)).Sum(e => e.Unit.Health) > 200)
                                {
                                    action = commander.Order(frame, Abilities.EFFECT_VOIDRAYPRISMATICALIGNMENT);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        protected override bool RechargeShieldsAtBattery(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (commander.UnitCalculation.Unit.ShieldMax > 0 && commander.UnitCalculation.Unit.Shield < 25)
            {
                var shieldBatttery = ActiveUnitData.SelfUnits.Where(a => a.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && a.Value.Unit.BuildProgress == 1 && a.Value.Unit.Energy > 5 && a.Value.Unit.Orders.Count() == 0).OrderBy(a => Vector2.DistanceSquared(commander.UnitCalculation.Position, a.Value.Position)).FirstOrDefault().Value;
                if (shieldBatttery != null)
                {
                    var distanceSquared = Vector2.DistanceSquared(commander.UnitCalculation.Position, shieldBatttery.Position);
                    if (distanceSquared > 35 && distanceSquared < 2500)
                    {
                        action = commander.Order(frame, Abilities.MOVE, new Point2D { X = shieldBatttery.Position.X, Y = shieldBatttery.Position.Y });
                        return true;
                    }
                }
            }
            return false;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.VOIDRAYSWARMDAMAGEBOOST))
            {
                var action = commander.Order(frame, Abilities.CANCEL);
                if (action != null)
                {
                    return action;
                }
            }
            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }
    }
}
