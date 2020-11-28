using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Pathing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Sharky.MicroControllers.Protoss
{
    public class ColossusMicroController : IndividualMicroController
    {
        CollisionCalculator CollisionCalculator;

        public ColossusMicroController(MapDataService mapDataService, UnitDataManager unitDataManager, UnitManager unitManager, DebugManager debugManager, IPathFinder sharkyPathFinder, SharkyOptions sharkyOptions, MicroPriority microPriority, bool groupUpEnabled, CollisionCalculator collisionCalculator)
            : base(mapDataService, unitDataManager, unitManager, debugManager, sharkyPathFinder, sharkyOptions, microPriority, groupUpEnabled)
        {
            CollisionCalculator = collisionCalculator;
        }

        protected override UnitCalculation GetBestDpsReduction(UnitCommander commander, Weapon weapon, IEnumerable<UnitCalculation> primaryTargets, IEnumerable<UnitCalculation> secondaryTargets)
        {
            float splashRadius = 0.3f;
            var dpsReductions = new ConcurrentDictionary<ulong, float>();
            Parallel.ForEach(primaryTargets, (enemyAttack) =>
            {
                float dpsReduction = 0;
                var attackLine = GetAttackLine(commander.UnitCalculation.Unit.Pos, enemyAttack.Unit.Pos);
                foreach (var splashedEnemy in secondaryTargets)
                {
                    if (CollisionCalculator.Collides(new Vector2(splashedEnemy.Unit.Pos.X, splashedEnemy.Unit.Pos.Y), splashedEnemy.Unit.Radius + splashRadius, attackLine.Start, attackLine.End))
                    {
                        dpsReduction += splashedEnemy.Dps / TimeToKill(weapon, splashedEnemy.Unit, UnitDataManager.UnitData[(UnitTypes)splashedEnemy.Unit.UnitType]);
                    }
                }
                dpsReductions[enemyAttack.Unit.Tag] = dpsReduction;
            });

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

    public class LineSegment
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
    }
}
