using SC2APIProtocol;
using Sharky.Pathing;
using System.Collections.Generic;

namespace Sharky.Managers
{
    public class MapManager : SharkyManager
    {
        MapData MapData;

        private int LastBuildingCount;
        private int LastVisibleEnemyUnitCount;

        private readonly int MillisecondsPerUpdate;
        private double MillisecondsUntilUpdate;

        public MapManager(MapData mapData)
        {
            MapData = mapData;

            LastBuildingCount = 0;
            LastVisibleEnemyUnitCount = 0;
            MillisecondsPerUpdate = 1000;
            MillisecondsUntilUpdate = 0;
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            var placementGrid = gameInfo.StartRaw.PlacementGrid;
            var heightGrid = gameInfo.StartRaw.TerrainHeight;
            var pathingGrid = gameInfo.StartRaw.PathingGrid;
            MapData.MapWidth = pathingGrid.Size.X;
            MapData.MapHeight = pathingGrid.Size.Y;
            MapData.Map = new Dictionary<int, Dictionary<int, MapCell>>();
            for (var x = 0; x < pathingGrid.Size.X; x++)
            {
                var row = new Dictionary<int, MapCell>();
                for (var y = 0; y < pathingGrid.Size.Y; y++)
                {
                    var walkable = GetDataValueBit(pathingGrid, x, y);
                    var height = GetDataValueByte(heightGrid, x, y);
                    var placeable = GetDataValueBit(placementGrid, x, y);
                    row[y] = new MapCell { X = x, Y = y, Walkable = walkable, TerrainHeight = height, Buildable = placeable, CurrentlyBuildable = placeable, EnemyAirDpsInRange = 0, EnemyGroundDpsInRange = 0, InEnemyVision = false, InSelfVision = false, NumberOfAllies = 0, NumberOfEnemies = 0, PoweredBySelfPylon = false, SelfAirDpsInRange = 0, SelfGroundDpsInRange = 0 };
                }
                MapData.Map[x] = row;
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var visiblilityMap = observation.Observation.RawData.MapState.Visibility;
            for (var x = 0; x < visiblilityMap.Size.X; x++)
            {
                for (var y = 0; y < visiblilityMap.Size.Y; y++)
                {
                    MapData.Map[x][y].InSelfVision = GetDataValueByte(visiblilityMap, x, y) == 2; // 2 is fully visible
                }
            }

            //MillisecondsUntilUpdate -= (1 / shark.FramesPerSecond) * 1000;
            //if (MillisecondsUntilUpdate > 0) { return new List<SC2APIProtocol.Action>(); }
            //MillisecondsUntilUpdate = MillisecondsPerUpdate;

            //var buildings = shark.EnemyAttacks.Where(e => UnitTypes.BuildingTypes.Contains(e.Value.Unit.UnitType)).Select(e => e.Value).Concat(shark.AllyAttacks.Where(e => UnitTypes.BuildingTypes.Contains(e.Value.Unit.UnitType)).Select(e => e.Value));
            //var currentBuildingCount = buildings.Count();
            //if (LastBuildingCount != currentBuildingCount)
            //{
            //    MapData.UpdateBuildingGrid(buildings, observation.Observation.RawData.Units.Where(u => UnitTypes.MineralFields.Contains(u.UnitType) || UnitTypes.GasGeysers.Contains(u.UnitType) || u.Alliance == Alliance.Neutral));
            //}
            //LastBuildingCount = currentBuildingCount;

            //var currentVisibleEnemyUnitCount = observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Enemy).Count();
            //if (LastVisibleEnemyUnitCount != currentVisibleEnemyUnitCount)
            //{
            //    MapData.UpdateGroundDamageGrid(shark.EnemyAttacks.Where(e => e.Value.DamageGround).Select(e => e.Value));
            //    MapData.UpdateEnemyVisionGroundGrid(shark.EnemyAttacks.Values);
            //    MapData.UpdateEnemyVisionGrid(shark.EnemyAttacks.Values);
            //    MapData.UpdateAirDamageGrid(shark.EnemyAttacks.Where(e => e.Value.DamageAir).Select(e => e.Value));
            //}
            //LastVisibleEnemyUnitCount = currentVisibleEnemyUnitCount;

            return new List<SC2APIProtocol.Action>();
        }

        bool GetDataValueBit(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;
            return ((data.Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? false : true;
        }
        int GetDataValueByte(ImageData data, int x, int y)
        {
            int pixelID = x + y * data.Size.X;
            return data.Data[pixelID];
        }
    }
}
