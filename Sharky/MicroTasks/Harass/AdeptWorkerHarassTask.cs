using SC2APIProtocol;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class AdeptWorkerHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        IIndividualMicroController AdeptMicroController;
        IIndividualMicroController AdeptShadeMicroController;

        Point2D EnemyMain { get; set; }
        Point2D EnemyExpansion { get; set; }

        public int MaxAdeptCount { get; set; }

        public AdeptWorkerHarassTask(BaseData baseData, TargetingData targetingData, IIndividualMicroController adeptMicroController, IIndividualMicroController adeptShadeMicroController, bool enabled = false, float priority = -1f)
        {
            BaseData = baseData;
            TargetingData = targetingData;
            AdeptMicroController = adeptMicroController;
            AdeptShadeMicroController = adeptShadeMicroController;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed && (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT || commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT))
                {
                    if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT && UnitCommanders.Count(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT) < MaxAdeptCount)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                    else if (commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPTPHASESHIFT)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();
            SetBases();

            foreach (var commander in UnitCommanders)
            {
                if (commander.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_ADEPT)
                {
                    if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(EnemyMain.X, EnemyMain.Y)) < 100)
                    {
                        if (commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
                        {
                            var action = AdeptMicroController.HarassWorkers(commander, EnemyMain, EnemyExpansion, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                        else
                        {
                            var action = AdeptMicroController.NavigateToPoint(commander, EnemyExpansion, TargetingData.ForwardDefensePoint, null, frame);
                            if (action != null)
                            {
                                commands.AddRange(action);
                            }
                        }
                    }
                    else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(EnemyExpansion.X, EnemyExpansion.Y)) < 100 && commander.UnitCalculation.NearbyEnemies.Take(25).Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)))
                    {
                        var action = AdeptMicroController.HarassWorkers(commander, EnemyExpansion, EnemyMain, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        var action = AdeptMicroController.NavigateToPoint(commander, EnemyMain, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                }
                else
                {
                    var target = EnemyMain;
                    if (commander.ParentUnitCalculation != null)
                    {
                        if (Vector2.DistanceSquared(commander.ParentUnitCalculation.Position, new Vector2(EnemyMain.X, EnemyMain.Y)) < 100)
                        {
                            target = EnemyExpansion;
                        }
                    }

                    var action = AdeptShadeMicroController.NavigateToPoint(commander, target, target, target, frame);
                    if (action != null)
                    {
                        commands.AddRange(action);
                    }
                }
            }

            return commands;
        }

        private void SetBases()
        {
            if (EnemyMain == null)
            {
                var mainBase = BaseData.EnemyBaseLocations.FirstOrDefault();
                if (mainBase != null)
                {
                    EnemyMain = mainBase.MineralLineBuildingLocation;
                }
                var expansionBase = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
                if (expansionBase != null)
                {
                    EnemyExpansion = expansionBase.MineralLineBuildingLocation;
                }
            }
        }
    }
}
