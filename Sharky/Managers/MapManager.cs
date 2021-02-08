using SC2APIProtocol;
using Sharky.Pathing;
using Sharky.S2ClientTypeEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers
{
    public class MapManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        MapData MapData;
        SharkyOptions SharkyOptions;
        SharkyUnitData SharkyUnitData;

        private int LastBuildingCount;
        private int LastVisibleEnemyUnitCount;

        private readonly int MillisecondsPerUpdate;
        private double MillisecondsUntilUpdate;

        public MapManager(MapData mapData, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, SharkyUnitData sharkyUnitData)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
            SharkyOptions = sharkyOptions;
            SharkyUnitData = sharkyUnitData;

            LastBuildingCount = 0;
            LastVisibleEnemyUnitCount = 0;
            MillisecondsPerUpdate = 500;
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
                    row[y] = new MapCell { X = x, Y = y, Walkable = walkable, TerrainHeight = height, Buildable = placeable, HasCreep = false, CurrentlyBuildable = placeable, EnemyAirDpsInRange = 0, EnemyGroundDpsInRange = 0, InEnemyVision = false, InSelfVision = false, InEnemyDetection = false, Visibility = 0, LastFrameVisibility = 0, NumberOfAllies = 0, NumberOfEnemies = 0, PoweredBySelfPylon = false, SelfAirDpsInRange = 0, SelfGroundDpsInRange = 0 };
                }
                MapData.Map[x] = row;
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            MillisecondsUntilUpdate -= (1 / SharkyOptions.FramesPerSecond) * 1000;
            if (MillisecondsUntilUpdate > 0) { return new List<SC2APIProtocol.Action>(); }
            MillisecondsUntilUpdate = MillisecondsPerUpdate;

            UpdateVisibility(observation.Observation.RawData.MapState.Visibility, (int)observation.Observation.GameLoop);
            UpdateCreep(observation.Observation.RawData.MapState.Creep);
            UpdateEnemyAirDpsInRange();
            UpdateInEnemyDetection();

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

        void UpdateEnemyAirDpsInRange()
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].EnemyAirDpsInRange = 0;
                }
            }

            foreach (var enemy in ActiveUnitData.EnemyUnits.Where(e => e.Value.DamageAir && e.Value.Unit.BuildProgress == 1))
            {
                var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Range + 2, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].EnemyAirDpsInRange += enemy.Value.Dps;
                }
            }
        }

        void UpdateInEnemyDetection()
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].InEnemyDetection = false;
                }
            }

            foreach (var enemy in ActiveUnitData.EnemyUnits.Where(e => e.Value.UnitClassifications.Contains(UnitClassification.Detector) && e.Value.Unit.BuildProgress == 1))
            {
                var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Unit.DetectRange + 1, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InEnemyDetection = true;
                }
            }

            foreach (var scan in SharkyUnitData.Effects.Where(e => e.EffectId == (uint)Effects.SCAN))
            {
                var nodes = GetNodesInRange(new Point { X = scan.Pos[0].X, Y = scan.Pos[0].Y, Z = 1 }, scan.Radius + 2, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InEnemyDetection = true;
                }
            }
        }

        private List<Vector2> GetNodesInRange(Point position, float range, int columns, int rows)
        {
            var nodes = new List<Vector2>();
            var xMin = (int)Math.Floor(position.X - range);
            var xMax = (int)Math.Ceiling(position.X + range);
            int yMin = (int)Math.Floor(position.Y - range);
            int yMax = (int)Math.Ceiling(position.Y + range);

            if (xMin < 0)
            {
                xMin = 0;
            }
            if (xMax >= columns)
            {
                xMax = columns - 1;
            }
            if (yMin < 0)
            {
                yMin = 0;
            }
            if (yMax >= rows)
            {
                yMax = rows - 1;
            }

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    nodes.Add(new Vector2(x, y));
                }
            }

            return nodes;
        }

        void UpdateCreep(ImageData creep)
        {
            for (var x = 0; x < creep.Size.X; x++)
            {
                for (var y = 0; y < creep.Size.Y; y++)
                {
                    MapData.Map[x][y].HasCreep = GetDataValueBit(creep, x, y);
                }
            }
        }

        void UpdateVisibility(ImageData visiblilityMap, int frame)
        {
            for (var x = 0; x < visiblilityMap.Size.X; x++)
            {
                for (var y = 0; y < visiblilityMap.Size.Y; y++)
                {
                    MapData.Map[x][y].InSelfVision = GetDataValueByte(visiblilityMap, x, y) == 2; // 2 is fully visible
                    MapData.Map[x][y].Visibility = GetDataValueByte(visiblilityMap, x, y);
                    if (GetDataValueByte(visiblilityMap, x, y) == 2)
                    {
                        MapData.Map[x][y].LastFrameVisibility = frame;
                    }
                }
            }
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
