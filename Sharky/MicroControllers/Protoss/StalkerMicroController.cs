using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    public class StalkerMicroController : IndividualMicroController
    {
        // TODO: use get blink avoid point, 8 distance from current position opposite direction of enemy, make sure it's in vision
        public StalkerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        protected override bool AttackBestTargetInRange(UnitCommander commander, Point2D target, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            if (bestTarget != null)
            {
                if (commander.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == bestTarget.Unit.Tag) && bestTarget.Unit.DisplayType == DisplayType.Visible)
                {
                    action = commander.Order(frame, Abilities.ATTACK, null, bestTarget.Unit.Tag);
                    return true;
                }

                if (commander.UnitCalculation.TargetPriorityCalculation.Overwhelm)
                {
                    var blinkReady = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BLINKTECH) && commander.AbilityOffCooldown(Abilities.EFFECT_BLINK_STALKER, frame, SharkyOptions.FramesPerSecond, SharkyUnitData);
                    if (blinkReady && bestTarget.FrameLastSeen == frame)
                    {
                        // only blink if can see all around unit, don't blink when entire army is hidden behind first unit
                        var point = new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y };
                        if (MapDataService.SelfVisible(point) && 
                            MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X + 7, Y = bestTarget.Unit.Pos.Y }) && MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X - 7, Y = bestTarget.Unit.Pos.Y }) && 
                            MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y + 7 }) && MapDataService.SelfVisible(new Point2D { X = bestTarget.Unit.Pos.X, Y = bestTarget.Unit.Pos.Y - 7 }))
                        {
                            action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, point);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            var blinkReady = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BLINKTECH) && commander.AbilityOffCooldown(Abilities.EFFECT_BLINK_STALKER, frame, SharkyOptions.FramesPerSecond, SharkyUnitData);
            if (blinkReady)
            {
                if (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.LOCKON))
                {
                    var cyclone = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_CYCLONE).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (cyclone != null)
                    {
                        var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, cyclone.Unit.Pos, target, defensivePoint, 15f + cyclone.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                        action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                        return true;
                    }
                }

                var attack = commander.UnitCalculation.Attackers.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();
                if (attack != null)
                {
                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                    action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                    return true;
                }
                return false;
            }

            return base.AvoidTargettedDamage(commander, target, defensivePoint, frame, out action);
        }

        protected override bool AvoidDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action) // TODO: use unit speed to dynamically adjust AvoidDamageDistance
        {
            action = null;

            var blinkReady = SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BLINKTECH) && commander.AbilityOffCooldown(Abilities.EFFECT_BLINK_STALKER, frame, SharkyOptions.FramesPerSecond, SharkyUnitData);
            if (blinkReady && commander.UnitCalculation.Unit.Shield < 10)
            {
                var attacks = new List<UnitCalculation>();

                foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
                {
                    if (DamageService.CanDamage(enemyAttack, commander.UnitCalculation) && InRange(commander.UnitCalculation.Position, enemyAttack.Position, UnitDataService.GetRange(enemyAttack.Unit) + commander.UnitCalculation.Unit.Radius + enemyAttack.Unit.Radius + AvoidDamageDistance))
                    {
                        attacks.Add(enemyAttack);
                    }
                }

                if (attacks.Count > 0)
                {
                    var attack = attacks.OrderBy(e => (e.Range * e.Range) - Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position)).FirstOrDefault();  // enemies that are closest to being outranged
                    var range = UnitDataService.GetRange(attack.Unit);
                    if (attack.Range > range)
                    {
                        range = attack.Range;
                    }

                    var avoidPoint = GetGroundAvoidPoint(commander, commander.UnitCalculation.Unit.Pos, attack.Unit.Pos, target, defensivePoint, attack.Range + attack.Unit.Radius + commander.UnitCalculation.Unit.Radius + AvoidDamageDistance + 4);
                    action = commander.Order(frame, Abilities.EFFECT_BLINK_STALKER, avoidPoint);
                    return true;
                }

                if (MaintainRange(commander, frame, out action)) { return true; }

                return false;
            }

            return base.AvoidDamage(commander, target, defensivePoint, frame, out action);
        }
    }
}
