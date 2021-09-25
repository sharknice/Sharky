using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Terran
{
    public class HellionMicroController : IndividualMicroController
    {
        CollisionCalculator CollisionCalculator;

        public HellionMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            // TODO: hellbat morph logic
            // action = commander.Order(frame, Abilities.MORPH_HELLBAT);
            // return true;

            return false;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float splashRadius = 0.3f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets)
            {
                float totalDamage = 0;
                var attackLine = GetAttackLine(commander, enemyAttack);
                foreach (var splashedEnemy in secondaryTargets)
                {
                    if (CollisionCalculator.Collides(splashedEnemy.Position, splashedEnemy.Unit.Radius + splashRadius, attackLine.Start, attackLine.End))
                    {
                        totalDamage += GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = totalDamage;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }

        private LineSegment GetAttackLine(UnitCommander commander, UnitCalculation enemyAttack)
        {
            // attack extends 1.65 past enemy

            var start = GetPositionFromRange(commander, enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius);
            var end = GetPositionFromRange(commander, enemyAttack.Unit.Pos, commander.UnitCalculation.Unit.Pos, commander.UnitCalculation.Range + 1.65f + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius);     

            return new LineSegment { Start = new Vector2(start.X, start.Y), End = new Vector2(end.X, end.Y) };
        }
    }
}
