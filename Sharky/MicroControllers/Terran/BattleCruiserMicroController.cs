using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    class BattleCruiserMicroController : IndividualMicroController
    {
        public BattleCruiserMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled, 2)
        {

        }

        protected override bool PreOffenseOrder(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return true; }

            return false;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (Yamato(commander, frame, out action))
            {
                return true;
            }

            if (TacticalJump(commander, frame, out action))
            {
                return true;
            }

            return false;
        }

        bool Yamato(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_YAMATOGUN) || commander.LastAbility == Abilities.EFFECT_YAMATOGUN && commander.LastOrderFrame + 5 > frame) { return true; }

            if (!SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.BATTLECRUISERENABLESPECIALIZATIONS) || !commander.AbilityOffCooldown(Abilities.EFFECT_YAMATOGUN, frame, SharkyOptions.FramesPerSecond, SharkyUnitData)) { return false; }

            var enemiesInRange = commander.UnitCalculation.NearbyEnemies.Where(e => e.FrameLastSeen == frame && e.Unit.DisplayType == DisplayType.Visible && Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position) < 100 && e.SimulatedHitpoints > 50 && (!e.Attributes.Contains(Attribute.Structure) || (e.Damage > 0 && e.SimulatedHitpoints < 250)) && (e.Unit.HealthMax + e.Unit.ShieldMax > 200 || (e.Unit.IsFlying && e.Unit.HealthMax + e.Unit.ShieldMax > 150)));

            var target = enemiesInRange.FirstOrDefault();
            if (target != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_YAMATOGUN, null, target.Unit.Tag);
                return true;
            }

            return false;
        }

        bool TacticalJump(UnitCommander commander, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_TACTICALJUMP) || commander.LastAbility == Abilities.EFFECT_TACTICALJUMP && commander.LastOrderFrame + 5 > frame) { return true; }

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_TACTICALJUMP, frame, SharkyOptions.FramesPerSecond, SharkyUnitData)) { return false; }

            if (commander.UnitCalculation.Unit.Health > 150 || commander.UnitCalculation.EnemiesThreateningDamage.Sum(e => e.Dps) < (commander.UnitCalculation.Unit.Health / 4)) { return false; }

            var target = BaseData.SelfBases.Where(b => b.MineralMiningInfo != null && b.ResourceCenter != null && b.MineralMiningInfo.Any(m => m.Workers.Any()) && Vector2.DistanceSquared(b.MineralLineLocation.ToVector2(), commander.UnitCalculation.Position) > 625).OrderBy(b => b.ResourceCenter.Health).FirstOrDefault();
            if (target != null)
            {
                action = commander.Order(frame, Abilities.EFFECT_TACTICALJUMP, target.MineralLineLocation);
                return true;
            }

            return false;
        }
    }
}
