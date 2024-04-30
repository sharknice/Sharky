namespace Sharky.MicroControllers.Protoss
{
    public class PhoenixGroupMicroController : IMicroController
    {
        MicroData MicroData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        DebugService DebugService;

        PhoenixInGroupMicroController PhoenixMicroController;

        public int MinimumGroupSize { get; set; }

        // TDDO: support needs to stick close to support units, if only a couple units can shoot up attack them
        // TODO: need to make them retreat better, they seem to just go in a straigh tline running through enemy fire
        // they attacked void rays that were over a bunch of stalkers'
        // carrier micro is garbage, make support for carrier stay within 5 range, if leaves 5 range make it go back, if no shields make it stay with 3 range

        public PhoenixGroupMicroController(DefaultSharkyBot defaultSharkyBot, PhoenixInGroupMicroController phoenixMicroController, int minimumGroupSize)
        {
            MicroData = defaultSharkyBot.MicroData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            DebugService = defaultSharkyBot.DebugService;
            PhoenixMicroController = phoenixMicroController;
            MinimumGroupSize = minimumGroupSize;
        }

        public List<SC2APIProtocol.Action> PerformActions(MicroGroup microGroup, Point2D target, Point2D defensivePoint, int frame)
        {
            if (microGroup.GroupRole == GroupRole.Harass || microGroup.GroupRole == GroupRole.HarassMineralLines)
            {
                return Attack(microGroup.Commanders, target, defensivePoint, null, frame, true);
            }
            else
            {
                return Attack(microGroup.Commanders, target, defensivePoint, null, frame, false);
            }
        }


        // phoenix in group range
        // leader picks a target and kite spot calcuated how far a phoenix can fly away from a target and still have time to get back in time to shoot when weapon ready
        // when weapon is equal or less than half-cooldown attack the desginated target
        // when weapon is more than half cooldown move to designated kite spot

        public List<SC2APIProtocol.Action> Attack(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commanders, target, defensivePoint, groupCenter, frame, false);
        }

        public List<SC2APIProtocol.Action> Attack(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame, bool harass)
        {
            var actions = new List<SC2APIProtocol.Action>();
            var centerVector = new Vector2(groupCenter.X, groupCenter.Y);
            var leader = commanders.FirstOrDefault(c => c.UnitRole == UnitRole.Leader);
            if (leader != null)
            {
                var point = new Point2D { X = leader.UnitCalculation.Position.X, Y = leader.UnitCalculation.Position.Y };

                if (harass && !leader.UnitCalculation.TargetPriorityCalculation.Overwhelm)
                {
                    target = GetNextTargetPoint(leader, target);
                    if (leader.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)) && (!leader.UnitCalculation.TargetPriorityCalculation.Overwhelm || !leader.UnitCalculation.NearbyAllies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) || e.UnitClassifications.HasFlag(UnitClassification.DefensiveStructure))))
                    {
                        leader.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.KillWorkers;
                    }
                    // if enemies threatening damage and no free kills run away
                    if (leader.UnitCalculation.EnemiesThreateningDamage.Any() && !leader.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)))
                    {
                        foreach (var commander in commanders)
                        {
                            actions.AddRange(PhoenixMicroController.NavigateToPoint(commander, target, defensivePoint, groupCenter, frame));
                        }
                        return actions;
                    }
                }
                var leaderTarget = PhoenixMicroController.GetBestTargetForGroup(leader, target, frame);
                if (leaderTarget != null)
                {
                    var kiteSpot = PhoenixMicroController.GetKiteSpot(leader, leaderTarget, target, centerVector, frame);
                    DebugService.DrawLine(leaderTarget.Unit.Pos, new Point { X = kiteSpot.X, Y = kiteSpot.Y, Z = leader.UnitCalculation.Unit.Pos.Z }, new Color { R = 250, B = 200, G = 200 });
                    DebugService.DrawSphere(leaderTarget.Unit.Pos, .5f, new Color { R = 250, B = 200, G = 200 });
                    DebugService.DrawSphere(new Point { X = kiteSpot.X, Y = kiteSpot.Y, Z = leader.UnitCalculation.Unit.Pos.Z }, .5f, new Color { R = 200, B = 200, G = 250 });
                    foreach (var commander in commanders.Where(c => c.UnitRole == UnitRole.Attack || c.UnitRole == UnitRole.Leader))
                    {
                        actions.AddRange(PhoenixMicroController.AttackDesignatedTarget(commander, target, defensivePoint, groupCenter, leaderTarget, kiteSpot, frame));
                    }
                }
                else
                {
                    if (Vector2.DistanceSquared(leader.UnitCalculation.Position, target.ToVector2()) < 100)
                    {
                        target = TargetingData.EnemyMainBasePoint;
                    }
                    foreach (var commander in commanders.Where(c => c.UnitRole == UnitRole.Attack || c.UnitRole == UnitRole.Leader))
                    {
                        if (commander.UnitRole != UnitRole.Leader && !commander.UnitCalculation.NearbyEnemies.Any())
                        {
                            var action = commander.Order(frame, Abilities.MOVE, leader.UnitCalculation.Position.ToPoint2D());
                            if (action != null)
                            {
                                actions.AddRange(action);
                            }
                        }
                        else
                        {
                            actions.AddRange(PhoenixMicroController.Attack(commander, target, defensivePoint, groupCenter, frame));
                        }
                    }
                }

                foreach (var commander in commanders.Where(c => c.UnitRole != UnitRole.Attack && c.UnitRole != UnitRole.Leader))
                {
                    if (commander.UnitCalculation.Unit.WeaponCooldown == 0)
                    {
                        var individualAction = PhoenixMicroController.AttackInRange(commander, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); continue; }
                    }

                    if (commander.UnitRole == UnitRole.Regenerate)
                    {
                        var individualAction = PhoenixMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (commander.UnitRole == UnitRole.Hide)
                    {
                        var individualAction = PhoenixMicroController.Retreat(commander, TargetingData.MainDefensePoint, groupCenter, frame); // TODO: calculate a better hiding spot for the army
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (commander.UnitRole == UnitRole.Regroup)
                    {
                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, leader.UnitCalculation.Position) > 100 && commander.UnitCalculation.EnemiesThreateningDamage.Any())
                        {
                            var individualAction = PhoenixMicroController.NavigateToPoint(commander, point, defensivePoint, groupCenter, frame);
                            if (individualAction != null) { actions.AddRange(individualAction); }
                        }
                        else
                        {
                            var action = commander.Order(frame, Abilities.MOVE, point);
                            if (action != null) { actions.AddRange(action); }
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

        public List<SC2APIProtocol.Action> Retreat(IEnumerable<UnitCommander> commanders, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();
            foreach (var commander in commanders)
            {
                var individualAction = PhoenixMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                if (individualAction != null) { actions.AddRange(individualAction); }
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
            return Attack(commanders, target, defensivePoint, groupCenter, frame, false);
        }


        public List<SC2APIProtocol.Action> SupportRetreat(IEnumerable<UnitCommander> commanders, IEnumerable<UnitCommander> supportTargets, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Retreat(commanders, defensivePoint, groupCenter, frame);
        }

        Point2D GetNextTargetPoint(UnitCommander commander, Point2D target)
        {
            var left = target.X - 0;
            var right = MapDataService.MapData.MapWidth - target.X;
            var top = target.Y;
            var bottom = MapDataService.MapData.MapHeight - target.Y;

            Point2D midPoint;
            Point2D stagingPoint;

            if (left < right && left < top && left < bottom)
            {
                midPoint = new Point2D { X = 0, Y = TargetingData.ForwardDefensePoint.Y };
                stagingPoint = new Point2D { X = 0, Y = target.Y };
            }
            else if (right < left && right < top && right < bottom)
            {
                midPoint = new Point2D { X = MapDataService.MapData.MapWidth, Y = TargetingData.ForwardDefensePoint.Y };
                stagingPoint = new Point2D { X = MapDataService.MapData.MapWidth - 0, Y = target.Y };
            }
            else if (top < left && top < right && top < bottom)
            {
                midPoint = new Point2D { X = TargetingData.ForwardDefensePoint.X, Y = 0 };
                stagingPoint = new Point2D { X = target.X, Y = 0 };
            }
            else
            {
                midPoint = new Point2D { X = TargetingData.ForwardDefensePoint.X, Y = MapDataService.MapData.MapHeight };
                stagingPoint = new Point2D { X = target.X, Y = MapDataService.MapData.MapHeight - 0 };
            }

            var startDistance = Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), commander.UnitCalculation.Position);
            var midDistance = Vector2.DistanceSquared(new Vector2(midPoint.X, midPoint.Y), commander.UnitCalculation.Position);
            var stagingDistance = Vector2.DistanceSquared(new Vector2(stagingPoint.X, stagingPoint.Y), commander.UnitCalculation.Position);
            var targetDistance = Vector2.DistanceSquared(new Vector2(target.X, target.Y), commander.UnitCalculation.Position);
            if (targetDistance < 225)
            {
                return target;
            }

            if (Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), new Vector2(midPoint.X, midPoint.Y)) > startDistance + 10 &&
                (MapDataService.MapData.MapWidth - commander.UnitCalculation.Position.X > 4) && (commander.UnitCalculation.Position.X > 4) && (MapDataService.MapData.MapHeight - commander.UnitCalculation.Position.Y > 4) && (commander.UnitCalculation.Position.Y > 4))
            {
                return midPoint;
            }
            else if (Vector2.DistanceSquared(new Vector2(TargetingData.ForwardDefensePoint.X, TargetingData.ForwardDefensePoint.Y), new Vector2(midPoint.X, midPoint.Y)) > midDistance + 10 && stagingDistance > 25)
            {
                return stagingPoint;
            }
            else
            {
                return target;
            }
        }
    }
}
