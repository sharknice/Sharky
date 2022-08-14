using SC2APIProtocol;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroControllers.Zerg
{
    public class MutaliskGroupMicroController : IMicroController
    {
        MicroData MicroData;
        TargetingData TargetingData;
        MapDataService MapDataService;

        IIndividualMicroController MutaliskMicroController;
        float GroupTightness = 1f;

        public int MinimumGroupSize { get; set; }

        public MutaliskGroupMicroController(DefaultSharkyBot defaultSharkyBot, IIndividualMicroController mutaliskMicroController, int minimumGroupSize)
        {
            MicroData = defaultSharkyBot.MicroData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            MutaliskMicroController = mutaliskMicroController;
            MinimumGroupSize = minimumGroupSize;
        }

        public List<SC2APIProtocol.Action> PerformActions(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            return Attack(commanders, target, defensivePoint, null, frame);
        }

        public List<SC2APIProtocol.Action> Attack(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            var centerVector = new Vector2(groupCenter.X, groupCenter.Y);
            var leader = commanders.Where(c => c.UnitRole == UnitRole.Attack).OrderBy(c => c.UnitCalculation.Unit.Tag).FirstOrDefault();
            if (leader != null)
            {
                var point = new Point2D { X = leader.UnitCalculation.Position.X, Y = leader.UnitCalculation.Position.Y };

                var attackers = commanders.Where(c => c.UnitRole == UnitRole.Attack);
                var grouped = attackers.Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, leader.UnitCalculation.Position) <= GroupTightness);
                if (grouped.Count() > MinimumGroupSize)
                {
                    if (MapDataService.GetCells(leader.UnitCalculation.Unit.Pos.X, leader.UnitCalculation.Unit.Pos.Y, 5).Any(e => e.EnemyAirSplashDpsInRange > 0))
                    {
                        foreach (var commander in grouped)
                        {
                            var action = MutaliskMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                            if (action != null) { actions.AddRange(action); }
                        }
                    }
                    else
                    {
                        leader.UnitCalculation.Damage *= grouped.Count();
                        var action = MutaliskMicroController.Attack(leader, target, defensivePoint, groupCenter, frame);
                        actions.AddRange(DuplicateActionsForCommanders(grouped, action));
                    }
                }
                else
                {
                    if (MapDataService.GetCells(leader.UnitCalculation.Unit.Pos.X, leader.UnitCalculation.Unit.Pos.Y, 5).Any(e => e.EnemyAirSplashDpsInRange > 0))
                    {
                        foreach (var commander in attackers)
                        {
                            var action = MutaliskMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                            if (action != null) { actions.AddRange(action); }
                        }
                    }
                    else
                    {
                        var action = leader.Order(frame, Abilities.MOVE, point);
                        actions.AddRange(DuplicateActionsForCommanders(attackers, action));
                    }
                }

                foreach (var commander in commanders.Where(c => c.UnitRole != UnitRole.Attack))
                {
                    if (commander.UnitRole == UnitRole.Regenerate)
                    {
                        var individualAction = MutaliskMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (commander.UnitRole == UnitRole.Hide)
                    {
                        var individualAction = MutaliskMicroController.Retreat(commander, TargetingData.MainDefensePoint, groupCenter, frame); // TODO: calculate a better hiding spot for the army
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (commander.UnitRole == UnitRole.Regroup)
                    {
                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, leader.UnitCalculation.Position) > 100 && commander.UnitCalculation.EnemiesThreateningDamage.Any())
                        {
                            var individualAction = MutaliskMicroController.NavigateToPoint(commander, point, defensivePoint, groupCenter, frame);
                            if (individualAction != null) { actions.AddRange(individualAction); }
                        }
                        else
                        {
                            var action = commander.Order(frame, Abilities.MOVE, point);
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                    }
                }
            }
            else
            {

                return Retreat(commanders, defensivePoint, groupCenter, frame);
            }

            return actions;
        }

        List<Action> DuplicateActionsForCommanders(IEnumerable<UnitCommander> commanders, List<Action> action)
        {
            var actions = new List<Action>();

            if (action != null)
            {
                var tags = commanders.Select(c => c.UnitCalculation.Unit.Tag);
                foreach (var command in action)
                {
                    if (command?.ActionRaw?.UnitCommand?.UnitTags != null)
                    {
                        foreach (var tag in tags)
                        {
                            var unitAction = new Action(command);
                            unitAction.ActionRaw.UnitCommand.UnitTags.Clear();
                            unitAction.ActionRaw.UnitCommand.UnitTags.Add(tag);
                            actions.Add(unitAction);
                        }
                    }
                }
            }

            return actions;
        }

        public List<SC2APIProtocol.Action> Retreat(IEnumerable<UnitCommander> commanders, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var leader = commanders.FirstOrDefault();
            if (leader != null)
            {
                var clumped = commanders.Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, leader.UnitCalculation.Position) <= 1);

                var action = MutaliskMicroController.Retreat(leader, defensivePoint, groupCenter, frame);
                if (action != null)
                {
                    var tags = commanders.Select(c => c.UnitCalculation.Unit.Tag);
                    foreach (var command in action)
                    {
                        if (command?.ActionRaw?.UnitCommand?.UnitTags != null)
                        {
                            command.ActionRaw.UnitCommand.UnitTags.AddRange(tags);
                        }
                    }
                    actions.AddRange(action);
                }

                var point = new Point2D { X = leader.UnitCalculation.Position.X, Y = leader.UnitCalculation.Position.Y };
                foreach (var commander in commanders)
                {
                    if (commander.UnitRole == UnitRole.Regenerate)
                    {
                        var individualAction = MutaliskMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, leader.UnitCalculation.Position) > 100)
                    {
                        var individualAction = MutaliskMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, leader.UnitCalculation.Position) > 1)
                    {
                        var individualAction = commander.Order(frame, Abilities.MOVE, point, allowSpam: true);
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                }
            }

            return actions;
        }

        public List<SC2APIProtocol.Action> Idle(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var commander in commanders)
            {
                var action = MicroData.IndividualMicroController.Idle(commander, defensivePoint, frame);           
                if (action != null) { actions.AddRange(action); }
            }

            return actions;
        }


        public List<SC2APIProtocol.Action> Support(IEnumerable<UnitCommander> commanders, IEnumerable<UnitCommander> supportTargets, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commanders, target, defensivePoint, groupCenter, frame);
        }


        public List<SC2APIProtocol.Action> SupportRetreat(IEnumerable<UnitCommander> commanders, IEnumerable<UnitCommander> supportTargets, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Retreat(commanders, defensivePoint, groupCenter, frame);
        }
    }
}
