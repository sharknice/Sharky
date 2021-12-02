using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class RavagerMicroController : IndividualMicroController
    {
        private int lastBileFrame = 0;

        public RavagerMicroController(DefaultSharkyBot defaultSharkyBot, IPathFinder sharkyPathFinder, MicroPriority microPriority, bool groupUpEnabled)
            : base(defaultSharkyBot, sharkyPathFinder, microPriority, groupUpEnabled)
        {
        }

        protected override bool OffensiveAbility(UnitCommander commander, Point2D target, Point2D defensivePoint, Point2D groupCenter, UnitCalculation bestTarget, int frame, out List<SC2APIProtocol.Action> action)
        {
            action = null;

            if (!commander.AbilityOffCooldown(Abilities.EFFECT_CORROSIVEBILE, frame, SharkyOptions.FramesPerSecond, SharkyUnitData))
            {
                return false;
            }

            if (lastBileFrame >= frame - 5)
            {
                return false;
            }

            var attacks = new List<UnitCalculation>();
            var center = commander.UnitCalculation.Position;

            foreach (var enemyAttack in commander.UnitCalculation.NearbyEnemies)
            {
                if (enemyAttack.Unit.UnitType != (uint)UnitTypes.ZERG_CHANGELING &&
                    InRange(enemyAttack.Position, commander.UnitCalculation.Position, 9 + enemyAttack.Unit.Radius + commander.UnitCalculation.Unit.Radius) && MapDataService.SelfVisible(enemyAttack.Unit.Pos))
                {
                    attacks.Add(enemyAttack);
                }
            }

            if (attacks.Count > 0)
            {
                var victims = attacks.OrderByDescending(u => u.Dps);
                if (victims.Count() > 0)
                {
                    var bestAttack = GetBestAttack(commander.UnitCalculation, victims, attacks);
                    if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinAir)
                    {
                        var airAttackers = victims.Where(u => u.DamageAir);
                        if (airAttackers.Count() > 0)
                        {
                            var air = GetBestAttack(commander.UnitCalculation, airAttackers, attacks);
                            if (air != null)
                            {
                                bestAttack = air;
                            }
                        }
                    }
                    else if (commander.UnitCalculation.TargetPriorityCalculation.TargetPriority == TargetPriority.WinGround)
                    {
                        var groundAttackers = victims.Where(u => u.DamageGround);
                        if (groundAttackers.Count() > 0)
                        {
                            var ground = GetBestAttack(commander.UnitCalculation, groundAttackers, attacks);
                            if (ground != null)
                            {
                                bestAttack = ground;
                            }
                        }
                    }
                    else
                    {
                        if (victims.Count() > 0)
                        {
                            var any = GetBestAttack(commander.UnitCalculation, victims, attacks);
                            if (any != null)
                            {
                                bestAttack = any;
                            }
                        }
                    }

                    if (bestAttack != null)
                    {
                        action = commander.Order(frame, Abilities.EFFECT_CORROSIVEBILE, bestAttack);
                        lastBileFrame = frame;
                        return true;
                    }
                }
            }

            return false;
        }

        private Point2D GetBestAttack(UnitCalculation unitCalculation, IEnumerable<UnitCalculation> enemies, IList<UnitCalculation> splashableEnemies)
        {
            float splashRadius = 0.5f;
            var killCounts = new Dictionary<Point, float>();
            foreach (var enemyAttack in enemies)
            {
                int killCount = 0;
                foreach (var splashedEnemy in splashableEnemies)
                {
                    if (Vector2.DistanceSquared(splashedEnemy.Position, enemyAttack.Position) < (splashedEnemy.Unit.Radius + splashRadius) * (splashedEnemy.Unit.Radius + splashRadius))
                    {
                        killCount++;
                    }
                }
                killCounts[enemyAttack.Unit.Pos] = killCount;
            }

            var best = killCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            return new Point2D { X = best.Key.X, Y = best.Key.Y };
        }

        public override List<SC2APIProtocol.Action> Retreat(UnitCommander commander, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            List<Action> actions = null;

            if (OffensiveAbility(commander, defensivePoint, defensivePoint, groupCenter, null, frame, out actions))
            {
                return actions;
            }

            return base.Retreat(commander, defensivePoint, groupCenter, frame);
        }
    }
}
