using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Chat;
using Sharky.DefaultBot;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class FullPylonWallOffTask : WallOffTask
    {
        bool Complete;

        List<Point2D> BuildingPoints;

        public FullPylonWallOffTask(DefaultSharkyBot defaultSharkyBot, bool enabled, float priority)
            : base(defaultSharkyBot.SharkyUnitData, defaultSharkyBot.ActiveUnitData, defaultSharkyBot.MacroData, defaultSharkyBot.MapData, defaultSharkyBot.WallService, defaultSharkyBot.ChatService, enabled, priority)
        {
            Complete = false;
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < 1 && ProbeSpot != null && !Complete)
            {
                foreach (var commander in commanders.OrderBy(c => c.Value.Claimed).ThenBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()).ThenBy(c => DistanceToResourceCenter(c)))
                {
                    if (commander.Value.UnitRole != UnitRole.Gas && (!commander.Value.Claimed || commander.Value.UnitRole == UnitRole.Minerals) && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker) && !commander.Value.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)) && commander.Value.UnitRole != UnitRole.Build)
                    {
                        if (Vector2.DistanceSquared(commander.Value.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) < 400)
                        {
                            commander.Value.UnitRole = UnitRole.Door;
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            GetWallData();

            var commands = new List<SC2APIProtocol.Action>();
            if (BuildingPoints == null)
            {
                return commands;
            }

            foreach (var spot in BuildingPoints)
            {
                var probe = UnitCommanders.FirstOrDefault();
                if (probe == null) { return commands; }
                if (probe.UnitRole != UnitRole.Wall) { probe.UnitRole = UnitRole.Wall; }

                var blockBuilding = ActiveUnitData.Commanders.FirstOrDefault(u => u.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.UnitCalculation.Unit.Pos.X == spot.X && u.Value.UnitCalculation.Unit.Pos.Y == spot.Y).Value;

                if (blockBuilding == null)
                {
                    Complete = false;

                    if (MacroData.Minerals >= 100)
                    {
                        var probeCommand = probe.Order(frame, Abilities.BUILD_PYLON, spot);
                        if (probeCommand != null)
                        {
                            commands.AddRange(probeCommand);
                        }

                        return commands;
                    }
                    else
                    {
                        if (!probe.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.BUILD_PYLON) && Vector2.DistanceSquared(probe.UnitCalculation.Position, new Vector2(ProbeSpot.X, ProbeSpot.Y)) > .1f)
                        {
                            var probeCommand = probe.Order(frame, Abilities.MOVE, ProbeSpot);
                            if (probeCommand != null)
                            {
                                commands.AddRange(probeCommand);
                            }

                            return commands;
                        }
                        return commands;
                    }
                }
            }

            Complete = true;

            if (!BlockedChatSent)
            {
                ChatService.SendChatType("FullPylonWallOffTask-TaskCompleted");
                BlockedChatSent = true;
            }

            return commands;
        }

        protected override void GetWallData()
        {
            if (!GotWallData)
            {
                GotWallData = true;

                var baseLocation = WallService.GetBaseLocation();
                if (baseLocation == null) { return; }

                if (MapData != null && MapData.WallData != null)
                {
                    var data = MapData.WallData.FirstOrDefault(d => d.BasePosition.X == baseLocation.X && d.BasePosition.Y == baseLocation.Y);
                    if (data != null && data.FullDepotWall != null)
                    {
                        BuildingPoints = data.FullDepotWall;
                        PlacementPoints = new List<Point2D> { data.FullDepotWall.FirstOrDefault() };
                        var spot = data.FullDepotWall.FirstOrDefault();
                        var angle = System.Math.Atan2(spot.Y - baseLocation.Y, baseLocation.X - spot.X);
                        var x = System.Math.Cos(angle) * 3.5f;
                        var y = System.Math.Sin(angle) * 3.5f;
                        ProbeSpot = new Point2D { X = spot.X + (float)x, Y = spot.Y - (float)y };
                    }
                }
            }
        }
    }
}
