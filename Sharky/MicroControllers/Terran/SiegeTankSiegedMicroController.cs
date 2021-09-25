using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class SiegeTankSiegedMicroController : IndividualMicroController
    {
        public SiegeTankSiegedMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {

        }

        public override List<SC2APIProtocol.Action> Attack(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            var formation = GetDesiredFormation(commander);
            var bestTarget = GetBestTarget(commander, target, frame);

            UpdateState(commander, target, defensivePoint, groupCenter, bestTarget, formation, frame);

            if (PreOffenseOrder(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (OffensiveAbility(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }

            if (WeaponReady(commander, frame))
            {
                if (AttackBestTarget(commander, target, defensivePoint, groupCenter, bestTarget, frame, out action)) { return action; }
            }

            return action;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (commander.UnitCalculation.EnemiesInRange.Count() == 0)
            {
                action = commander.Order(frame, Abilities.MORPH_UNSIEGE);
                return true;
            }

            return false;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float innerSplashRadius = 0.4687f;
            float middleSplashRadius = 0.7812f;
            float outerSplashRadius = 1.25f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets.Where(e => !e.Unit.IsFlying))
            {
                float dpsReduction = 0;
                foreach (var splashedEnemy in secondaryTargets.Where(e => !e.Unit.IsFlying))
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + innerSplashRadius) * (splashedEnemy.Unit.Radius + innerSplashRadius))
                    {
                        dpsReduction += splashedEnemy.Dps / GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                    else if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + middleSplashRadius) * (splashedEnemy.Unit.Radius + middleSplashRadius))
                    {
                        dpsReduction += splashedEnemy.Dps / (GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]) * .5f);
                    }
                    else if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + outerSplashRadius) * (splashedEnemy.Unit.Radius + outerSplashRadius))
                    {
                        dpsReduction += splashedEnemy.Dps / (GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]) * .25f);
                    }
                }
                foreach (var splashedFriendly in commander.UnitCalculation.NearbyAllies.Where(e => !e.Unit.IsFlying))
                {
                    if (Vector2.DistanceSquared(splashedFriendly.Position, enemyAttack.Position) < (splashedFriendly.Unit.Radius + innerSplashRadius) * (splashedFriendly.Unit.Radius + innerSplashRadius))
                    {
                        dpsReduction -= splashedFriendly.Dps / GetDamage(weapon, splashedFriendly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedFriendly.Unit.UnitType]);
                    }
                    else if (Vector2.DistanceSquared(splashedFriendly.Position, enemyAttack.Position) < (splashedFriendly.Unit.Radius + middleSplashRadius) * (splashedFriendly.Unit.Radius + middleSplashRadius))
                    {
                        dpsReduction -= splashedFriendly.Dps / (GetDamage(weapon, splashedFriendly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedFriendly.Unit.UnitType]) * .5f);
                    }
                    else if (Vector2.DistanceSquared(splashedFriendly.Position, enemyAttack.Position) < (splashedFriendly.Unit.Radius + outerSplashRadius) * (splashedFriendly.Unit.Radius + outerSplashRadius))
                    {
                        dpsReduction -= splashedFriendly.Dps / (GetDamage(weapon, splashedFriendly.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedFriendly.Unit.UnitType]) * .25f);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = dpsReduction;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }

        public override List<SC2APIProtocol.Action> Support(UnitCommander commander, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commander, target, defensivePoint, groupCenter, frame);
        }

        protected override bool AttackBestTarget(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (AttackBestTargetInRange(commander, target, bestTarget, frame, out action))
            {
                return true;
            }

            return false;
        }

        protected override void UpdateState(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame)
        {
            if (commander.CommanderState == CommanderState.Grouping)
            {
                commander.CommanderState = CommanderState.None;
            }
        }

        protected override bool SpecialCaseMove(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, Formation formation, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool AvoidTargettedOneHitKills(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool AvoidTargettedDamage(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        protected override bool Retreat(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;
            return false;
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<SC2APIProtocol.Action> action = null;

            if (OffensiveAbility(commander, null, defensivePoint, groupCenter, null, frame, out action)) { return action; }

            return action;
        }
    }
}
