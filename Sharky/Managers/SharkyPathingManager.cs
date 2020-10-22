using SC2APIProtocol;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharky.Managers
{
    public class SharkyPathingManager : SharkyManager
    {
        SharkyPathFinder SharkyPathFinder;

        private int LastBuildingCount;
        private int LastVisibleEnemyUnitCount;

        private readonly int MillisecondsPerUpdate;
        private double MillisecondsUntilUpdate;

        public SharkyPathingManager(SharkyPathFinder sharkyPathFinder)
        {
            SharkyPathFinder = sharkyPathFinder;
            LastBuildingCount = 0;
            LastVisibleEnemyUnitCount = 0;
            MillisecondsPerUpdate = 1000;
            MillisecondsUntilUpdate = 0;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            SharkyPathFinder.CreateMapGrid(gameInfo.StartRaw.PathingGrid);
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            MillisecondsUntilUpdate -= (1 / shark.FramesPerSecond) * 1000;
            if (MillisecondsUntilUpdate > 0) { return new List<SC2APIProtocol.Action>(); }
            MillisecondsUntilUpdate = MillisecondsPerUpdate;

            var buildings = shark.EnemyAttacks.Where(e => UnitTypes.BuildingTypes.Contains(e.Value.Unit.UnitType)).Select(e => e.Value).Concat(shark.AllyAttacks.Where(e => UnitTypes.BuildingTypes.Contains(e.Value.Unit.UnitType)).Select(e => e.Value));
            var currentBuildingCount = buildings.Count();
            if (LastBuildingCount != currentBuildingCount)
            {
                SharkyPathFinder.UpdateBuildingGrid(buildings, observation.Observation.RawData.Units.Where(u => UnitTypes.MineralFields.Contains(u.UnitType) || UnitTypes.GasGeysers.Contains(u.UnitType) || u.Alliance == Alliance.Neutral));
            }
            LastBuildingCount = currentBuildingCount;

            var currentVisibleEnemyUnitCount = observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Enemy).Count();
            if (LastVisibleEnemyUnitCount != currentVisibleEnemyUnitCount)
            {
                SharkyPathFinder.UpdateGroundDamageGrid(shark.EnemyAttacks.Where(e => e.Value.DamageGround).Select(e => e.Value));
                SharkyPathFinder.UpdateEnemyVisionGroundGrid(shark.EnemyAttacks.Values);
                SharkyPathFinder.UpdateEnemyVisionGrid(shark.EnemyAttacks.Values);
                SharkyPathFinder.UpdateAirDamageGrid(shark.EnemyAttacks.Where(e => e.Value.DamageAir).Select(e => e.Value));
            }
            LastVisibleEnemyUnitCount = currentVisibleEnemyUnitCount;

            return new List<SC2APIProtocol.Action>();
        }
    }
}
