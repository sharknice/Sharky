using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using static SC2APIProtocol.AbilityData.Types;

namespace Sharky.MicroTasks
{
    public class ClearFutureExpansionTask : MicroTask
    {
        TargetingData TargetingData;
        BaseData BaseData;
        BuildingService BuildingService;

        IMicroController MicroController;

        Point2D NextBaseLocation;
        int BaseCountDuringLocation;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }

        public ClearFutureExpansionTask(DefaultSharkyBot defaultSharkyBot,
            List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            BuildingService = defaultSharkyBot.BuildingService;

            MicroController = defaultSharkyBot.MicroController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            Enabled = true;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.Value.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            if (UpdateBaseLocation())
            {
                var detectors = UnitCommanders.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Detector) || c.UnitCalculation.UnitClassifications.Contains(UnitClassification.DetectionCaster));
                var nonDetectors = UnitCommanders.Where(c => !c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Detector) && !c.UnitCalculation.UnitClassifications.Contains(UnitClassification.DetectionCaster));

                var vector = new Vector2(NextBaseLocation.X, NextBaseLocation.Y);

                foreach (var nonDetector in nonDetectors)
                {
                    if (nonDetector.UnitCalculation.EnemiesInRangeOf.Any() || Vector2.DistanceSquared(nonDetector.UnitCalculation.Position, vector) < 25)
                    {
                        actions.AddRange(MicroController.Attack(new List<UnitCommander> { nonDetector }, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                    }
                    else
                    {
                        actions.AddRange(nonDetector.Order(frame, Abilities.MOVE, NextBaseLocation));
                    }
                }

                foreach (var detector in detectors)
                {
                    if (detector.UnitCalculation.EnemiesInRangeOf.Any() || Vector2.DistanceSquared(detector.UnitCalculation.Position, vector) < 25)
                    {
                        actions.AddRange(MicroController.Support(new List<UnitCommander> { detector }, nonDetectors, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                    }
                    else
                    {
                        actions.AddRange(detector.Order(frame, Abilities.MOVE, NextBaseLocation));
                    }
                }
            }
            else
            {
                actions.AddRange(MicroController.Attack(UnitCommanders, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, null, frame));
            }

            return actions;
        }

        private bool UpdateBaseLocation()
        {
            var baseCount = BaseData.SelfBases.Count();
            if (NextBaseLocation == null || BaseCountDuringLocation != baseCount)
            {
                var nextBase = BuildingService.GetNextBaseLocation();
                if (nextBase != null)
                {
                    NextBaseLocation = nextBase.Location;
                    BaseCountDuringLocation = baseCount;
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}
