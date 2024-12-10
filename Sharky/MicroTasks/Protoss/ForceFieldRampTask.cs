namespace Sharky.MicroTasks
{
    public class ForceFieldRampTask : MicroTask
    {
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;

        WallService WallService;
        MapDataService MapDataService;
        MapData MapData;

        int LastForceFieldFrame;

        Point2D ForceFieldPoint;
        Point2D SentryPoint;

        public ForceFieldRampTask(TargetingData targetingData, ActiveUnitData activeUnitData, MapData mapData, WallService wallService, MapDataService mapDataService, bool enabled, float priority)
        {
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            MapData = mapData;
            WallService = wallService;
            MapDataService = mapDataService;
            Priority = priority;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
            LastForceFieldFrame = 0;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY)
                {
                    commander.Value.Claimed = true;
                    UnitCommanders.Add(commander.Value);
                    break;
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            SetForceFieldSpot(frame);

            var forceField = ActiveUnitData.NeutralUnits.FirstOrDefault(u => u.Value.Unit.UnitType == (uint)UnitTypes.NEUTRAL_FORCEFIELD && Vector2.DistanceSquared(new Vector2(ForceFieldPoint.X, ForceFieldPoint.Y), u.Value.Position) < 1).Value;

            foreach (var commander in UnitCommanders)
            {
                if (forceField != null || commander.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.EFFECT_FORCEFIELD))
                {
                    continue;
                }

                if (commander.UnitCalculation.Unit.Energy >= 50 && frame - LastForceFieldFrame > 20)
                {
                    var probeHeight = MapDataService.MapHeight(commander.UnitCalculation.Unit.Pos);
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen >= frame - 1 && e.UnitClassifications.HasFlag(UnitClassification.ArmyUnit) && !e.Attributes.Contains(SC2APIProtocol.Attribute.Massive) && !e.Unit.IsFlying && e.Unit.UnitType != (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT && probeHeight > MapDataService.MapHeight(e.Unit.Pos)))
                    {
                        LastForceFieldFrame = frame;
                        var action = commander.Order(frame, Abilities.EFFECT_FORCEFIELD, ForceFieldPoint);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                }

                if (SentryPoint != null && Vector2.DistanceSquared(commander.UnitCalculation.Position, SentryPoint.ToVector2()) > 2)
                {
                    var action = commander.Order(frame, Abilities.MOVE, SentryPoint);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }
 
            return commands;
        }

        private void SetForceFieldSpot(int frame)
        {
            if (ForceFieldPoint == null)
            {
                var baseLocation = WallService.GetBaseLocation();
                if (baseLocation == null) { return; }

                if (MapData != null && MapData.WallData != null)
                {
                    var data = MapData.WallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.X && d.BasePosition.Y == baseLocation.Y);
                    if (data != null && data.RampCenter != null)
                    {
                        ForceFieldPoint = data.RampCenter;
                        if (data.RampBottom != null)
                        {
                            ForceFieldPoint = data.RampBottom;
                        }
                        var direction = baseLocation.ToVector2() - ForceFieldPoint.ToVector2();
                        var normalizedDirection = Vector2.Normalize(direction);
                        var sentrySpot = ForceFieldPoint.ToVector2() + (normalizedDirection * 9);
                        SentryPoint = sentrySpot.ToPoint2D();
                        return;
                    }
                }

                var chokePoint = TargetingData.ChokePoints.Good.FirstOrDefault();
                if (chokePoint != null)
                {
                    ForceFieldPoint = new Point2D { X = chokePoint.Center.X, Y = chokePoint.Center.Y };
                    return;
                }

                ForceFieldPoint = TargetingData.ForwardDefensePoint;
            }

            if (SentryPoint == null)
            {
                var direction = TargetingData.SelfMainBasePoint.ToVector2() - ForceFieldPoint.ToVector2();
                var normalizedDirection = Vector2.Normalize(direction);
                var sentrySpot = ForceFieldPoint.ToVector2() + (normalizedDirection * 9);
                SentryPoint = sentrySpot.ToPoint2D();
            }
        }
    }
}
