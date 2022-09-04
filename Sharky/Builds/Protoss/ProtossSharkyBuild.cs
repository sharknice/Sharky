using SC2APIProtocol;
using Sharky.Builds.BuildChoosing;
using Sharky.DefaultBot;
using Sharky.MicroTasks;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Builds
{
    public abstract class ProtossSharkyBuild : SharkyBuild
    {
        protected ChronoData ChronoData;
        protected ICounterTransitioner CounterTransitioner;

        protected TargetingData TargetingData;
        protected MapDataService MapDataService;
        protected EnemyData EnemyData;

        public ProtossSharkyBuild(DefaultSharkyBot defaultSharkyBot, ICounterTransitioner counterTransitioner)
            : base(defaultSharkyBot)
        {
            ChronoData = defaultSharkyBot.ChronoData;
            TargetingData = defaultSharkyBot.TargetingData;
            MapDataService = defaultSharkyBot.MapDataService;
            EnemyData = defaultSharkyBot.EnemyData;
            CounterTransitioner = counterTransitioner;
        }

        public override List<string> CounterTransition(int frame)
        {
            return CounterTransitioner.DefaultCounterTransition(frame);
        }

        public override void StartBuild(int frame)
        {
            MacroData.DesiredPylonsAtDefensivePoint = 0;
            MacroData.DesiredDefensiveBuildingsAtDefensivePoint[UnitTypes.PROTOSS_SHIELDBATTERY] = 0;

            base.StartBuild(frame);

            if (EnemyData.EnemyRace == Race.Terran)
            {
                BuildOptions.WallOffType = BuildingPlacement.WallOffType.None;
            }
            else
            {
                BuildOptions.WallOffType = BuildingPlacement.WallOffType.Partial;
                if (EnemyData.EnemyRace == Race.Zerg || EnemyData.EnemyRace == Race.Random && !MicroTaskData[typeof(ProtossDoorTask).Name].Enabled)
                {
                    MicroTaskData[typeof(ProtossDoorTask).Name].Enable();
                }
            }
        }

        protected void SendProbeForFirstPylon(int frame)
        {
            if (MacroData.FoodUsed == 13 && MacroData.Minerals > 94 && UnitCountService.Count(UnitTypes.PROTOSS_PYLON) == 0)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.ForwardDefensePoint, frame);
            }
        }

        protected void SendProbeForFirstGateway(int frame)
        {
            if (MacroData.FoodUsed >= 14 && UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) == 0 && ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.Unit.BuildProgress > .75f))
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.ForwardDefensePoint, frame);
            }
        }

        protected void SendProbeForSecondGateway(int frame)
        {
            if (UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_GATEWAY) == 1 && MacroData.Minerals > 90)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.ForwardDefensePoint, frame);
            }
        }

        protected void SendProbeForCyberneticsCore(int frame)
        {
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_CYBERNETICSCORE) == 0 && UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) == 0 && ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY && u.Value.Unit.BuildProgress > .90f))
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.ForwardDefensePoint, frame);
            }
        }

        protected void SendProbeToNaturalForFirstPylon(int frame)
        {
            if (MacroData.FoodUsed == 13 && MacroData.Minerals > 15 && UnitCountService.Count(UnitTypes.PROTOSS_PYLON) == 0)
            {
                if (MapDataService != null && MapDataService.MapData.WallData != null)
                {
                    var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                    if (wallData != null)
                    {
                        var point = wallData.Pylons.FirstOrDefault();
                        if (point != null)
                        {
                            PrePositionBuilderTask.SendBuilder(point, frame);
                            return;
                        }
                    }
                }
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }

        protected void SendProbeToNaturalForFirstGateway(int frame)
        {
            if (MacroData.FoodUsed >= 14 && UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) == 0 && ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_PYLON && u.Value.Unit.BuildProgress > .5f))
            {
                if (MapDataService != null && MapDataService.MapData.WallData != null)
                {
                    var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                    if (wallData != null)
                    {
                        var point = wallData.WallSegments.FirstOrDefault();
                        if (point != null)
                        {
                            PrePositionBuilderTask.SendBuilder(point.Position, frame);
                            return;
                        }
                    }
                }
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }

        protected void SendProbeToNaturalForCyberneticsCore(int frame)
        {
            if (UnitCountService.EquivalentTypeCompleted(UnitTypes.PROTOSS_GATEWAY) == 0 && ActiveUnitData.SelfUnits.Any(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY && u.Value.Unit.BuildProgress > .7f))
            {
                if (MapDataService != null && MapDataService.MapData.WallData != null)
                {
                    var wallData = MapDataService.MapData.WallData.FirstOrDefault(b => b.BasePosition.X == TargetingData.NaturalBasePoint.X && b.BasePosition.Y == TargetingData.NaturalBasePoint.Y);
                    if (wallData != null)
                    {
                        var point = wallData.WallSegments.FirstOrDefault();
                        if (point != null)
                        {
                            PrePositionBuilderTask.SendBuilder(point.Position, frame);
                            return;
                        }
                    }
                }
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }

        protected virtual void SendProbeForNexus(int frame)
        {
            if (UnitCountService.Count(UnitTypes.PROTOSS_NEXUS) == 1 && MacroData.Minerals > 275)
            {
                PrePositionBuilderTask.SendBuilder(TargetingData.NaturalBasePoint, frame);
            }
        }
    }
}
