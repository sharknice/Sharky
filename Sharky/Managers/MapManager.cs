using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
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
        SharkyUnitData SharkyUnitData;
        DebugService DebugService;
        WallDataService WallDataService;

        private int LastUpdateFrame;
        private readonly int FramesPerUpdate;

        public MapManager(MapData mapData, ActiveUnitData activeUnitData, SharkyOptions sharkyOptions, SharkyUnitData sharkyUnitData, DebugService debugService, WallDataService wallDataService)
        {
            MapData = mapData;
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            DebugService = debugService;
            WallDataService = wallDataService;

            FramesPerUpdate = 5;
            LastUpdateFrame = -100;
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
                    row[y] = new MapCell { X = x, Y = y, Walkable = walkable, TerrainHeight = height, Buildable = placeable, HasCreep = false, CurrentlyBuildable = placeable, EnemyAirDpsInRange = 0, EnemyGroundDpsInRange = 0, InEnemyVision = false, InSelfVision = false, InEnemyDetection = false, InSelfDetection = false, Visibility = 0, LastFrameVisibility = 0, NumberOfAllies = 0, NumberOfEnemies = 0, PoweredBySelfPylon = false, SelfAirDpsInRange = 0, SelfGroundDpsInRange = 0, LastFrameAlliesTouched = 0, PathBlocked = false };
                }
                MapData.Map[x] = row;
            }
            MapData.WallData = new List<WallData>();

            Console.WriteLine("Calculating wall data");
            WallDataService.AddTerranWallData(gameInfo.MapName, MapData.WallData);
            WallDataService.AddPartialWallData(gameInfo.MapName, MapData.WallData);
            WallDataService.AddPartialWallData(gameInfo.MapName, MapData.WallData);
            WallDataService.AddBlockWallData(gameInfo.MapName, MapData.WallData);
            MapData.MapName = gameInfo.MapName;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            //DrawGrid(observation.Observation.RawData.Player.Camera);

            if (FramesPerUpdate > observation.Observation.GameLoop - LastUpdateFrame) { return null; }
            LastUpdateFrame = (int)observation.Observation.GameLoop;

            UpdateVisibility(observation.Observation.RawData.MapState.Visibility, (int)observation.Observation.GameLoop);
            UpdateCreep(observation.Observation.RawData.MapState.Creep);
            UpdateEnemyDpsInRange();
            UpdateInEnemyDetection();
            UpdateInSelfDetection();
            UpdateInEnemyVision();
            UpdateNumberOfAllies((int)observation.Observation.GameLoop);
            UpdatePathBlocked();

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



            return null;
        }

        private void DrawGrid(Point camera)
        {
            var height = 12;

            DebugService.DrawText($"Point: {camera.X},{camera.Y}");
            DebugService.DrawSphere(new Point { X = camera.X, Y = camera.Y, Z = height }, .25f);
            DebugService.DrawLine(new Point { X = camera.X, Y = camera.Y, Z = height }, new Point { X = camera.X, Y = camera.Y, Z = 0 }, new Color { R = 255, G = 255, B = 255 });

            for (int x = -5; x <= 5; x++)
            {
                for (int y = -5; y <= 5; y++)
                {
                    var point = new Point { X = (int)camera.X + x, Y = (int)camera.Y + y, Z = height + 1 };
                    var color = new Color { R = 255, G = 255, B = 255 };
                    if (point.X + 1 < MapData.MapWidth && point.Y + 1 < MapData.MapHeight && point.X > 0 && point.Y > 0)
                    {        
                        if (!MapData.Map[(int)point.X][(int)point.Y].CurrentlyBuildable)
                        {
                            color = new Color { R = 255, G = 0, B = 0 };
                        }
                        DebugService.DrawLine(point, new Point { X = point.X + 1, Y = point.Y, Z = height + 1 }, color);
                        DebugService.DrawLine(point, new Point { X = point.X, Y = point.Y + 1, Z = height + 1 }, color);
                        DebugService.DrawLine(point, new Point { X = point.X, Y = point.Y + 1, Z = 1 }, color);
                    }
                }
            }
        }

        void UpdateNumberOfAllies(int frame)
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].NumberOfAllies = 0;
                }
            }

            foreach (var selfUnit in ActiveUnitData.SelfUnits)
            {
                var nodes = GetNodesInRange(selfUnit.Value.Unit.Pos, selfUnit.Value.Unit.Radius, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].NumberOfAllies += 1;
                    MapData.Map[(int)node.X][(int)node.Y].LastFrameAlliesTouched = frame;
                }
            }
        }

        void UpdateEnemyDpsInRange()
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].EnemyAirDpsInRange = 0;
                    MapData.Map[x][y].EnemyGroundDpsInRange = 0;
                    MapData.Map[x][y].EnemyAirSplashDpsInRange = 0;
                    MapData.Map[x][y].EnemyGroundSplashDpsInRange = 0;
                }
            }

            foreach (var enemy in ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.BuildProgress == 1))
            {
                if (enemy.Value.DamageAir)
                {
                    var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Range + 2, MapData.MapWidth, MapData.MapHeight);
                    var splash = SharkyUnitData.AirSplashDamagers.Contains((UnitTypes)enemy.Value.Unit.UnitType);
                    foreach (var node in nodes)
                    {
                        MapData.Map[(int)node.X][(int)node.Y].EnemyAirDpsInRange += enemy.Value.Dps;
                        if (splash)
                        {
                            MapData.Map[(int)node.X][(int)node.Y].EnemyAirSplashDpsInRange += enemy.Value.Dps;
                        }
                    }
                }
                if (enemy.Value.DamageGround)
                {
                    var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Range + 2, MapData.MapWidth, MapData.MapHeight);
                    var splash = SharkyUnitData.GroundSplashDamagers.Contains((UnitTypes)enemy.Value.Unit.UnitType);
                    foreach (var node in nodes)
                    {
                        MapData.Map[(int)node.X][(int)node.Y].EnemyGroundDpsInRange += enemy.Value.Dps;
                        if (splash)
                        {
                            MapData.Map[(int)node.X][(int)node.Y].EnemyGroundSplashDpsInRange += enemy.Value.Dps;
                        }
                    }
                }
                if (enemy.Value.Unit.UnitType == (uint)UnitTypes.ZERG_INFESTOR || enemy.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR)
                {                
                    if (enemy.Value.Unit.Energy > 70)
                    {
                        var nodes = GetNodesInRange(enemy.Value.Unit.Pos, 12, MapData.MapWidth, MapData.MapHeight);
                        foreach (var node in nodes)
                        {
                            MapData.Map[(int)node.X][(int)node.Y].EnemyAirSplashDpsInRange += 50;
                            MapData.Map[(int)node.X][(int)node.Y].EnemyGroundSplashDpsInRange += 50;
                        }
                    }
                }
            }
        }

        void UpdatePathBlocked()
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].PathBlocked = false;
                }
            }

            foreach (var enemy in ActiveUnitData.EnemyUnits.Where(e => !e.Value.Unit.IsFlying && e.Value.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.Value.Unit.UnitType != (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED))
            {
                var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Unit.Radius, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].PathBlocked = true;
                }
            }

            foreach (var enemy in ActiveUnitData.SelfUnits.Where(e => !e.Value.Unit.IsFlying && e.Value.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.Value.Unit.UnitType != (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED))
            {
                var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Unit.Radius, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].PathBlocked = true;
                }
            }

            foreach (var enemy in ActiveUnitData.NeutralUnits.Where(e => !e.Value.Unit.IsFlying && e.Value.Attributes.Contains(SC2APIProtocol.Attribute.Structure) && e.Value.Unit.UnitType != (uint)UnitTypes.TERRAN_SUPPLYDEPOTLOWERED))
            {
                var nodes = GetNodesInRange(enemy.Value.Unit.Pos, enemy.Value.Unit.Radius, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].PathBlocked = true;
                }
            }
        }

        void UpdateInSelfDetection()
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].InSelfDetection = false;
                }
            }

            foreach (var unitCalculation in ActiveUnitData.SelfUnits.Where(e => e.Value.UnitClassifications.Contains(UnitClassification.Detector) && e.Value.Unit.BuildProgress == 1))
            {
                var nodes = GetNodesInRange(unitCalculation.Value.Unit.Pos, unitCalculation.Value.Unit.DetectRange + 1, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InSelfDetection = true;
                }
            }

            foreach (var scan in SharkyUnitData.Effects.Where(e => e.EffectId == (uint)Effects.SCAN && e.Alliance == Alliance.Self))
            {
                var nodes = GetNodesInRange(new Point { X = scan.Pos[0].X, Y = scan.Pos[0].Y, Z = 1 }, scan.Radius + 2, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InSelfDetection = true;
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

            foreach (var scan in SharkyUnitData.Effects.Where(e => e.EffectId == (uint)Effects.SCAN && e.Alliance == Alliance.Enemy))
            {
                var nodes = GetNodesInRange(new Point { X = scan.Pos[0].X, Y = scan.Pos[0].Y, Z = 1 }, scan.Radius + 2, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InEnemyDetection = true;
                }
            }
        }

        void UpdateInEnemyVision()
        {
            for (var x = 0; x < MapData.MapWidth; x++)
            {
                for (var y = 0; y < MapData.MapHeight; y++)
                {
                    MapData.Map[x][y].InEnemyVision = false;
                }
            }

            foreach (var enemy in ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.BuildProgress == 1))
            {
                var nodes = GetNodesInRange(enemy.Value.Unit.Pos, 12, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InEnemyVision = true;
                }
            }

            foreach (var scan in SharkyUnitData.Effects.Where(e => e.EffectId == (uint)Effects.SCAN && e.Alliance == Alliance.Enemy))
            {
                var nodes = GetNodesInRange(new Point { X = scan.Pos[0].X, Y = scan.Pos[0].Y, Z = 1 }, scan.Radius + 2, MapData.MapWidth, MapData.MapHeight);
                foreach (var node in nodes)
                {
                    MapData.Map[(int)node.X][(int)node.Y].InEnemyVision = true;
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
