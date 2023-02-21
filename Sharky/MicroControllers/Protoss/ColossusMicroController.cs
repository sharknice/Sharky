using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Protoss
{
    // TODO: make hallucinations move farther forward than rest of army when there is splash damage, do not allow them to be close enough to friendly to take splash
    public class ColossusMicroController : IndividualMicroController
    {
        CollisionCalculator CollisionCalculator;

        public ColossusMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;
        }

        protected override bool DealWithSiegedTanks(UnitCommander commander, Point2D target, Point2D defensivePoint, int frame, out List<SC2APIProtocol.Action> action)
        {
            if (commander.UnitCalculation.Unit.IsHallucination)
            {
                return base.DealWithSiegedTanks(commander, target, defensivePoint, frame, out action);
            }
            action = null;
            return false;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float splashRadius = 0.3f;
            var dpsReductions = new Dictionary<ulong, float>();
            foreach (var enemyAttack in primaryTargets)
            {
                float totalDamage = 0;
                var attackLine = GetAttackLine(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos);
                foreach (var splashedEnemy in secondaryTargets)
                {
                    if (CollisionCalculator.Collides(splashedEnemy.Position, splashedEnemy.Unit.Radius + splashRadius, attackLine.Start, attackLine.End))
                    {
                        totalDamage +=  GetDamage(weapon, splashedEnemy.Unit, SharkyUnitData.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = totalDamage;
            }

            var best = dpsReductions.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            return primaryTargets.FirstOrDefault(t => t.Unit.Tag == best);
        }

        private LineSegment GetAttackLine(Point start, Point end)
        {
            var length = 1.4f;
            var dx = start.X - end.X;
            var dy = start.Y - end.Y;
            var dist = (float)Math.Sqrt((dx * dx) + (dy * dy));
            dx /= dist;
            dy /= dist;
            var attackStart = new Vector2(start.X + (length * dy), start.Y - (length * dx));
            var attackEnd = new Vector2(start.X - (length * dy), start.Y + (length * dx));
            return new LineSegment { Start = attackStart, End = attackEnd };
        }
    }
}
