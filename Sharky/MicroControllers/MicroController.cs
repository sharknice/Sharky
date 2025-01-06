﻿namespace Sharky.MicroControllers
{
    public class MicroController : IMicroController
    {
        MicroData MicroData;

        public MicroController(MicroData microData)
        {
            MicroData = microData;
        }

        public List<SC2Action> Attack(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2Action>();
            var stopwatch = new Stopwatch();

            foreach (var commander in commanders)
            {
                if (commander.SkipFrame)
                {
                    commander.SkipFrame = false;
                    continue;
                }
                stopwatch.Restart();
                List<SC2Action> action;

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                }

                if (action != null)
                {
                    actions.AddRange(action);
                }
                var timeTaken = stopwatch.ElapsedMilliseconds;
                if (timeTaken > 1)
                {
                    commander.SkipFrame = true;
                }
            }
            return actions;
        }

        public List<SC2Action> Retreat(IEnumerable<UnitCommander> commanders, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2Action>();
            var stopwatch = new Stopwatch();

            foreach (var commander in commanders)
            {
                if (commander.SkipFrame)
                {
                    commander.SkipFrame = false;
                    continue;
                }
                stopwatch.Restart();
                List<SC2Action> action;

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                }

                if (action != null)
                {
                    actions.AddRange(action);
                }
                var timeTaken = stopwatch.ElapsedMilliseconds;
                if (timeTaken > 1)
                {
                    commander.SkipFrame = true;
                }
            }
            return actions;
        }

        public List<SC2Action> Idle(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            var actions = new List<SC2Action>();
            var stopwatch = new Stopwatch();

            foreach (var commander in commanders)
            {
                if (commander.SkipFrame)
                {
                    commander.SkipFrame = false;
                    continue;
                }
                stopwatch.Restart();
                List<SC2Action> action;

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Idle(commander, defensivePoint, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Idle(commander, defensivePoint, frame);
                }

                if (action != null)
                {
                    actions.AddRange(action);
                }
                var timeTaken = stopwatch.ElapsedMilliseconds;
                if (timeTaken > 1)
                {
                    commander.SkipFrame = true;
                }
            }
            return actions;
        }

        public List<SC2Action> Support(IEnumerable<UnitCommander> commanders, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2Action>();
            var stopwatch = new Stopwatch();

            var targetVector = new Vector2(target.X, target.Y);
            var friendlies = supportTargets.OrderByDescending(c => c.UnitCalculation.EnemiesInRangeOf.Count()).ThenByDescending(c => c.UnitCalculation.EnemiesInRange.Count()).ThenByDescending(c => c.UnitCalculation.NearbyEnemies.Count()).ThenBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetVector));

            foreach (var commander in commanders)
            {
                if (commander.SkipFrame)
                {
                    commander.SkipFrame = false;
                    continue;
                }
                stopwatch.Restart();
                List<SC2Action> action;

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Support(commander, friendlies, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Support(commander, friendlies, target, defensivePoint, groupCenter, frame);
                }

                if (action != null)
                {
                    actions.AddRange(action);
                }
                var timeTaken = stopwatch.ElapsedMilliseconds;
                if (timeTaken > 1)
                {
                    commander.SkipFrame = true;
                }
            }
            return actions;
        }

        public List<SC2APIProtocol.Action> Contain(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Retreat(commanders, defensivePoint, groupCenter, frame);
        }

        public List<SC2Action> Defend(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2Action>();
            var stopwatch = new Stopwatch();

            foreach (var commander in commanders)
            {
                if (commander.SkipFrame)
                {
                    commander.SkipFrame = false;
                    continue;
                }
                stopwatch.Restart();
                List<SC2Action> action;

                if (MicroData.IndividualMicroControllers.TryGetValue((UnitTypes)commander.UnitCalculation.Unit.UnitType, out var individualMicroController))
                {
                    action = individualMicroController.Defend(commander, target, defensivePoint, groupCenter, frame);
                }
                else
                {
                    action = MicroData.IndividualMicroController.Defend(commander, target, defensivePoint, groupCenter, frame);
                }

                if (action != null)
                {
                    actions.AddRange(action);
                }
                var timeTaken = stopwatch.ElapsedMilliseconds;
                if (timeTaken > 1)
                {
                    commander.SkipFrame = true;
                }
            }
            return actions;
        }
    }
}
