namespace Sharky.MicroControllers.Terran
{
    public class BansheeGroupMicroController : IMicroController
    {
        MicroData MicroData;
        TargetingData TargetingData;
        MapDataService MapDataService;
        DebugService DebugService;

        BansheeInGroupMicroController BansheeInGroupMicroController;

        public int MinimumGroupSize { get; set; }

        public BansheeGroupMicroController(DefaultSharkyBot defaultSharkyBot, BansheeInGroupMicroController bansheeMicroController, int minimumGroupSize)
        {
            MicroData = defaultSharkyBot.MicroData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            DebugService = defaultSharkyBot.DebugService;
            BansheeInGroupMicroController = bansheeMicroController;
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


        // banshees in group range
        // leader picks a target and kite spot calcuated how far a banshees can fly away from a target and still have time to get back in time to shoot when weapon ready
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
            var allCloakable = commanders.All(c => c.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK) || c.UnitCalculation.Unit.Energy > 40);
            if (leader != null)
            {
                var point = new Point2D { X = leader.UnitCalculation.Position.X, Y = leader.UnitCalculation.Position.Y };

                if (harass)
                {
                    target = GetNextTargetPoint(leader, target);
                    if (leader.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.Worker)) && (!leader.UnitCalculation.TargetPriorityCalculation.Overwhelm || !leader.UnitCalculation.NearbyAllies.Any(e => e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) || e.UnitClassifications.HasFlag(UnitClassification.DefensiveStructure))))
                    {
                        leader.UnitCalculation.TargetPriorityCalculation.TargetPriority = TargetPriority.KillWorkers;
                    }
                    // if enemies threatening damage and no free kills run away
                    if (leader.UnitCalculation.EnemiesThreateningDamage.Any() && !CloakedAndUndetected(leader))
                    {
                        foreach(var commander in commanders)
                        {
                            actions.AddRange(BansheeInGroupMicroController.NavigateToPoint(commander, target, defensivePoint, groupCenter, frame));
                        }
                        return actions;
                    }
                }
                var leaderTarget = BansheeInGroupMicroController.GetBestTargetForGroup(leader, target, frame);
                if (leaderTarget != null)
                {
                    var kiteSpot = BansheeInGroupMicroController.GetKiteSpot(leader, leaderTarget, target, centerVector, frame);
                    DebugService.DrawLine(leaderTarget.Unit.Pos, new Point { X = kiteSpot.X, Y = kiteSpot.Y, Z = leader.UnitCalculation.Unit.Pos.Z }, new Color { R = 250, B = 200, G = 200 });
                    DebugService.DrawSphere(leaderTarget.Unit.Pos, .5f, new Color { R = 250, B = 200, G = 200 });
                    DebugService.DrawSphere(new Point { X = kiteSpot.X, Y = kiteSpot.Y, Z = leader.UnitCalculation.Unit.Pos.Z }, .5f, new Color { R = 200, B = 200, G = 250 });
                    foreach (var commander in commanders.Where(c => c.UnitRole == UnitRole.Attack || c.UnitRole == UnitRole.Leader))
                    {
                        actions.AddRange(BansheeInGroupMicroController.AttackDesignatedTarget(commander, target, defensivePoint, groupCenter, leaderTarget, kiteSpot, frame));
                    }
                }
                else
                {
                    foreach (var commander in commanders.Where(c => c.UnitRole == UnitRole.Attack || c.UnitRole == UnitRole.Leader))
                    {
                        var commanderAction = BansheeInGroupMicroController.Attack(commander, target, defensivePoint, groupCenter, frame);
                        if (commanderAction !=  null)
                        {
                            actions.AddRange(commanderAction);
                        }
                    }
                }

                foreach (var commander in commanders.Where(c => c.UnitRole != UnitRole.Attack && c.UnitRole != UnitRole.Leader))
                {
                    if (commander.UnitCalculation.Unit.WeaponCooldown == 0)
                    {
                        var individualAction = BansheeInGroupMicroController.AttackInRange(commander, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); continue; }
                    }

                    if (commander.UnitRole == UnitRole.Repair)
                    {
                        var individualAction = BansheeInGroupMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (commander.UnitRole == UnitRole.Hide)
                    {
                        var individualAction = BansheeInGroupMicroController.Retreat(commander, TargetingData.MainDefensePoint, groupCenter, frame); // TODO: calculate a better hiding spot for the army
                        if (individualAction != null) { actions.AddRange(individualAction); }
                    }
                    else if (commander.UnitRole == UnitRole.Regroup)
                    {
                        if (Vector2.DistanceSquared(commander.UnitCalculation.Position, leader.UnitCalculation.Position) > 100 && commander.UnitCalculation.EnemiesThreateningDamage.Any() && !CloakedAndUndetected(commander))
                        {
                            var individualAction = BansheeInGroupMicroController.NavigateToPoint(commander, point, defensivePoint, groupCenter, frame);
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
                if (commander.UnitCalculation.Unit.WeaponCooldown == 0)
                {
                    var action = BansheeInGroupMicroController.AttackInRange(commander, frame);
                    if (action != null) { actions.AddRange(action); continue; }
                }

                var individualAction = BansheeInGroupMicroController.Retreat(commander, defensivePoint, groupCenter, frame);
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

        private bool CloakedAndUndetected(UnitCommander commander)
        {
            return (commander.UnitCalculation.Unit.BuffIds.Contains((uint)Buffs.BANSHEECLOAK) || (commander.UnitCalculation.Unit.Energy > 50)) && !MapDataService.InEnemyDetection(commander.UnitCalculation.Unit.Pos);
        }

        public List<SC2Action> Contain(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Retreat(commanders, defensivePoint, groupCenter, frame);
        }

        public List<SC2Action> Defend(IEnumerable<UnitCommander> commanders, Point2D target, Point2D defensivePoint, Point2D groupCenter, int frame)
        {
            return Attack(commanders, target, defensivePoint, groupCenter, frame);
        }
    }
}
