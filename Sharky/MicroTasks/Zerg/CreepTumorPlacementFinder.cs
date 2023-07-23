using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Zerg
{
    /// <summary>
    /// Finds best placement spot for creep tumors
    /// </summary>
    public class CreepTumorPlacementFinder
    {
        TargetingData TargetingData;
        IBuildingPlacement ZergBuildingPlacement;
        ActiveUnitData ActiveUnitData;
        BuildOptions BuildOptions;
        BuildingService BuildingService;

        MapData MapData;

        /// <summary>
        /// Creep source density in given area. Higher numbers mean higher creep source density.
        /// Very high number of OutsideCreepRange means no suitable position for creep.
        /// Lowest value is best for spreading creep.
        /// </summary>
        int[,] creepSourceDensityIndex;

        /// <summary>
        /// Creep density index with this value means the position is out of range
        /// </summary>
        private const int OutsideCreepRange = 0x10000;
        private const int OutsideCreepRangeMask = 0xffff;

        private int lastFrameUpdate = -1;

        // Creep source units on the map that were already calculated for creep density map. We keep track of them so we could remove them when they are destroyed.
        private Dictionary<ulong, Vector2> creepSources = new();
        
        public CreepTumorPlacementFinder(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            ZergBuildingPlacement = defaultSharkyBot.ZergBuildingPlacement;
            MapData = defaultSharkyBot.MapData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            BuildingService = defaultSharkyBot.BuildingService;
        }

        /// <summary>
        /// Adds creep source to the creep density map
        /// </summary>
        private void AddCreepSource(UnitCalculation unitCalculation)
        {
            UpdateAreaCreepDensity(unitCalculation.Position.X, unitCalculation.Position.Y, 1);
            creepSources.Add(unitCalculation.Unit.Tag, unitCalculation.Position);
        }

        /// <summary>
        /// Removes creep source from the creep density map
        /// </summary>
        private void RemoveCreepSource(ulong unitTag)
        {
            if (creepSources.TryGetValue(unitTag, out var pos))
            {
                UpdateAreaCreepDensity(pos.X, pos.Y, -1);
                creepSources.Remove(unitTag);
            }
        }

        /// <summary>
        /// Looks for new units and adds them to the unit source map
        /// </summary>
        public void UpdateNewCreepSources(int frame)
        {
            // Init creep density map if null
            if (creepSourceDensityIndex is null)
            {
                creepSourceDensityIndex = new int[MapData.MapWidth, MapData.MapHeight];
                for (int x = 0; x < MapData.MapWidth; x++)
                    for (int y = 0; y < MapData.MapHeight; y++)
                    {
                        creepSourceDensityIndex[x, y] = OutsideCreepRange;
                    }
            }

            foreach (var commander in ActiveUnitData.Commanders.Values.Where(x=>IsCreepSource((UnitTypes)x.UnitCalculation.Unit.UnitType) && x.FrameFirstSeen > lastFrameUpdate))
            {
                AddCreepSource(commander.UnitCalculation);
            }

            lastFrameUpdate = frame;
        }

        /// <summary>
        /// Removes dead creep source units from the map
        /// </summary>
        public void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var deadUnit in deadUnits)
            {
                RemoveCreepSource(deadUnit);
            }
        }

        /// <summary>
        /// Returns true if given field is valid field for putting creep tumor there
        /// </summary>
        public bool IsValidCreepTumorPosition(int x, int y)
        {
            var mc = MapData.Map[x,y];
            return mc.HasCreep
                && mc.Walkable
                && mc.Buildable
                && mc.InSelfVision
                && mc.CurrentlyBuildable;
        }

        /// <summary>
        /// Updates creep source density in given area.
        /// </summary>
        /// <param name="posX">Position X</param>
        /// <param name="posY">Position Y</param>
        /// <param name="multiplier">positive (adding) or negative (removing) multiplier</param>
        private void UpdateAreaCreepDensity(float posX, float posY, int multiplier = 1)
        {
            for (int y = -10; y <= 10; y++)
            {
                int arrY = (int)(y + posY);
                if (arrY < 0 || arrY >= MapData.MapHeight) // Check array bounds
                    continue;

                for (int x = -10; x <= 10; x++)
                {
                    int arrX = (int)(x + posX);
                    if (arrX < 0 || arrX >= MapData.MapWidth) // Check array bounds
                        continue;

                    float dist = (float)Math.Sqrt(x*x + y*y);
                    if (dist > 10)
                        continue;

                    // highest score is far from point
                    int score = (10 - (int)dist) * multiplier;

                    // Initially 
                    creepSourceDensityIndex[arrX, arrY] = (creepSourceDensityIndex[arrX, arrY] & OutsideCreepRangeMask) + score;
                }
            }
        }

        /// <summary>
        /// Returns true if given unit type generates creep
        /// </summary>
        private bool IsCreepSource(UnitTypes unitType)
        {
            return unitType == UnitTypes.ZERG_CREEPTUMORBURROWED
                || unitType == UnitTypes.ZERG_CREEPTUMORQUEEN
                || unitType == UnitTypes.ZERG_CREEPTUMOR
                || unitType == UnitTypes.ZERG_HATCHERY
                || unitType == UnitTypes.ZERG_LAIR
                || unitType == UnitTypes.ZERG_HIVE;
        }

        public void AddQueenTarget(int x, int y)
        {
            UpdateAreaCreepDensity(x, y);
        }

        public void RemoveQueenTarget(int x, int y)
        {
            UpdateAreaCreepDensity(x, y, -1);
        }

        /// <summary>
        /// Calculates 
        /// </summary>
        public float CreepScore(int x, int y)
        {
            // Distance to main/enemy base
            var enemyMain = TargetingData.EnemyMainBasePoint.ToVector2();
            var selfMain = TargetingData.SelfMainBasePoint.ToVector2();
            
            // Distance from our main to enemy main
            float mainBasesDistance = Vector2.Distance(TargetingData.SelfMainBasePoint.ToVector2(), enemyMain);
            
            // How far is this point from enemy main. The closer to enemy base, smaller the number.
            float enemyBaseBonus = Vector2.Distance(new Vector2(x, y), enemyMain) / mainBasesDistance;
            //enemyBaseBonus = 1.0f - enemyBaseBonus * enemyBaseBonus;
            return creepSourceDensityIndex[x, y] + enemyBaseBonus * 2;
        }

        /// <summary>
        /// Finds position for placing tumor by queen
        /// </summary>
        public Point2D? FindTumorPlacement(int frame)
        {
            System.Drawing.Point? lowest = null;
            float lowestScore = OutsideCreepRange;

            // Find best tumor place
            for (int x = 0; x < MapData.MapWidth; x++)
                for (int y = 0; y < MapData.MapHeight; y++)
                {
                    if (!IsValidCreepTumorPosition(x, y))
                    {
                        continue;
                    }

                    var creepScore = CreepScore(x, y);

                    if (lowest is null || creepScore<lowestScore)
                    {
                        lowest = new System.Drawing.Point(x, y);
                        lowestScore = creepScore;
                    }
                }


            if (lowest is not null)
            {
                return new Point2D() { X = lowest.Value.X, Y = lowest.Value.Y };
            }
            else
                return null;
        }

        /// <summary>
        /// Finds position for tumor extension of other creep tumor
        /// </summary>
        public Point2D? FindTumorExtensionPlacement(int frame, Vector2 location)
        {
            System.Drawing.Point? lowest = null;
            float lowestScore = OutsideCreepRange;

            for (int x = -10; x < 10; x++)
                for (int y = -10; y < 10; y++)
                {
                    int arrX = (int)(x + location.X);
                    int arrY = (int)(y + location.Y);

                    if (arrX >= 0 && arrX < MapData.MapWidth && arrY >= 0 && arrY < MapData.MapHeight
                        && Vector2.Distance(new Vector2(x, y), Vector2.Zero) <= 10
                        && IsValidCreepTumorPosition(arrX, arrY)
                        && (lowest is null || CreepScore(arrX, arrY) < lowestScore))
                    {
                        lowest = new System.Drawing.Point(arrX, arrY);
                        lowestScore = CreepScore(arrX, arrY);
                    }
                }

            if (lowest is null)
                return null;

            if (!BuildingService.BlocksResourceCenter(lowest.Value.X, lowest.Value.Y, 4))
            {
                return new Point2D() { X = lowest.Value.X, Y = lowest.Value.Y };
            }

            return ZergBuildingPlacement.FindPlacement(new Point2D() { X = lowest.Value.X, Y = lowest.Value.Y }, UnitTypes.ZERG_CREEPTUMORQUEEN, 1, maxDistance: 9, ignoreResourceProximity: true, allowBlockBase: false, requireVision: true);
        }

        public void DebugCreepSpread(DebugService debugService)
        {
            for (int x = 0; x < MapData.MapWidth; x++)
                for (int y = 0; y < MapData.MapHeight; y++)
                {
                    if (creepSourceDensityIndex[x, y] < 65535)
                    {
                        debugService.DrawText($"{creepSourceDensityIndex[x, y]}:{CreepScore(x, y):F2}", new Point() { X= x, Y =y, Z = MapData.Map[x, y].TerrainHeight }, new Color() { R = 255, G = 31, B = 127 }, 8);
                    }
                }
        }
    }
}
