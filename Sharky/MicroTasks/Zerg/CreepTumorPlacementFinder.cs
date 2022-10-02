using SC2APIProtocol;
using Sharky.Builds;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.Extensions;
using Sharky.Pathing;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks.Zerg
{
    public class CreepTumorPlacementFinder
    {
        TargetingData TargetingData;
        IBuildingPlacement ZergBuildingPlacement;
        ActiveUnitData ActiveUnitData;
        BuildOptions BuildOptions;
        UnitCountService UnitCountService;
        BuildingService BuildingService;

        MapData MapData;

        bool needsUpdate = true;

        float[,] CreepTumorPlacementMap;

        public CreepTumorPlacementFinder(DefaultSharkyBot defaultSharkyBot, IPathFinder pathFinder)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            ZergBuildingPlacement = defaultSharkyBot.ZergBuildingPlacement;
            MapData = defaultSharkyBot.MapData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitCountService = defaultSharkyBot.UnitCountService;
            BuildingService = defaultSharkyBot.BuildingService;
        }

        private void ScoreArea(float posX, float posY, float multiplier)
        {
            for (int x = -10; x <= 10; x++)
                for (int y = -10; y <= 10; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), Vector2.Zero);
                    if (distance > 10.0f)
                        continue;

                    int arrX = (int)(x + posX);
                    int arrY = (int)(y + posY);

                    if (arrX >= 0 && arrX < MapData.MapWidth && arrY >= 0 && arrY < MapData.MapHeight)
                    {
                        if (MapData.Map[arrX][arrY].HasCreep && MapData.Map[arrX][arrY].Walkable && MapData.Map[arrX][arrY].CurrentlyBuildable)
                        {
                            CreepTumorPlacementMap[arrX, arrY] = CreepTumorPlacementMap[arrX, arrY] == 0.0f ? 100.0f : CreepTumorPlacementMap[arrX, arrY];
                            CreepTumorPlacementMap[arrX, arrY] -= (1 - (distance / 10.0f)) * multiplier;
                        }
                    }
                }
        }

        private void UpdateMap(IEnumerable<UnitCommander> queens)
        {
            for (int x = 0; x < MapData.MapWidth; x++)
                for (int y = 0; y < MapData.MapHeight; y++)
                {
                    bool valid = MapData.Map[x][y].HasCreep 
                        && MapData.Map[x][y].CurrentlyBuildable 
                        && !BuildingService.BlocksResourceCenter(x, y, 1)
                        && MapData.Map[x][y].InSelfVision;
                    CreepTumorPlacementMap[x, y] = valid ? 0.0f : -1000.0f;
                }

            // Score area around creep spread structures
            foreach (var creepSpreader in ActiveUnitData.Commanders.Values.Where(
                x => x.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_CREEPTUMORBURROWED
                || x.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_CREEPTUMORQUEEN
                || x.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_CREEPTUMOR
                || x.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_HATCHERY
                || x.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_LAIR
                || x.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_HIVE
                ))
            {
                ScoreArea(creepSpreader.UnitCalculation.Position.X, creepSpreader.UnitCalculation.Position.Y, 2.0f);

                // Creep tumor targets
                if (creepSpreader.UnitCalculation.Unit.UnitType == (int)UnitTypes.ZERG_CREEPTUMORBURROWED && creepSpreader.UnitCalculation.Unit.Orders.Any() && creepSpreader.UnitCalculation.Unit.Orders.First().TargetWorldSpacePos != null)
                {
                    ScoreArea(creepSpreader.UnitCalculation.Unit.Orders.First().TargetWorldSpacePos.X, creepSpreader.UnitCalculation.Unit.Orders.First().TargetWorldSpacePos.X, 1.0f);
                }
            }

            // Queen targets
            foreach (var queen in queens)
            {
                ScoreArea(queen.UnitCalculation.Unit.Orders.First().TargetWorldSpacePos.X, queen.UnitCalculation.Unit.Orders.First().TargetWorldSpacePos.Y, 1.5f);
            }

            bool earlyTumors = UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_CREEPTUMOR) < BuildOptions.ZergBuildOptions.TumorsPreferForward;

            // Distance to main/enemy base
            var enemyMain = TargetingData.EnemyMainBasePoint.ToVector2();
            var selfMain = TargetingData.SelfMainBasePoint.ToVector2();
            float mainBasesDistance = Vector2.Distance(TargetingData.SelfMainBasePoint.ToVector2(), enemyMain);
            for (int x = 0; x < MapData.MapWidth; x++)
                for (int y = 0; y < MapData.MapHeight; y++)
                {
                    float enemyBaseBonus = Vector2.Distance(new Vector2(x, y), enemyMain) / mainBasesDistance;
                    enemyBaseBonus = 1.0f - enemyBaseBonus * enemyBaseBonus;
                    CreepTumorPlacementMap[x, y] += (earlyTumors ? 50 : 3f) * enemyBaseBonus;
                }
        }

        private void ConsiderUpdatingMap(int frame, IEnumerable<UnitCommander> queens, bool forceUpdate = false)
        {
            if (CreepTumorPlacementMap == null)
            {
                CreepTumorPlacementMap = new float[MapData.MapWidth, MapData.MapHeight];
            }

            {
                UpdateMap(queens.Where(x => x.UnitRole == UnitRole.SpreadCreep && x.UnitCalculation.Unit.Orders.Any() && x.UnitCalculation.Unit.Orders.First().AbilityId == (uint)Abilities.BUILD_CREEPTUMOR_QUEEN));
                needsUpdate = false;
            }
        }

        public Point2D FindTumorPlacement(int frame, IEnumerable<UnitCommander> queens, bool canFindSuboptimal = false, bool forceUpdate = false)
        {
            ConsiderUpdatingMap(frame, queens, forceUpdate);

            if (needsUpdate && !canFindSuboptimal)
                return null;

            Point2D highest = null;

            // Find best tumor place
            for (int x = 0; x < MapData.MapWidth; x++)
                for (int y = 0; y < MapData.MapHeight; y++)
                {
                    if (highest == null || CreepTumorPlacementMap[x, y] > CreepTumorPlacementMap[(int)highest.X, (int)highest.Y])
                        highest = new Point2D().Create(x, y);
                }

            if (highest != null)
            {
                needsUpdate = true;
                return highest;
            }
            else
                return null;
        }

        public Point2D FindTumorExtensionPlacement(int frame, IEnumerable<UnitCommander> queens, Vector2 location, bool canFindSuboptimal = false, bool forceUpdate = false)
        {
            ConsiderUpdatingMap(frame, queens, forceUpdate);

            if (needsUpdate && !canFindSuboptimal)
                return null;

            Point2D highest = null;
            float highestValue = float.MinValue;

            for (int x = -10; x < 10; x ++)
                for (int y = -10; y < 10; y ++)
                {
                    int arrX = (int)(x + location.X);
                    int arrY = (int)(y + location.Y);

                    if (arrX >= 0 && arrX < MapData.MapWidth && arrY >= 0 && arrY < MapData.MapHeight
                        && Vector2.Distance(new Vector2(x, y), Vector2.Zero) <= 10
                        && MapData.Map[arrX][arrY].InSelfVision
                        && (highest == null || CreepTumorPlacementMap[arrX, arrY] > highestValue))
                    {
                        highest = new Point2D().Create(arrX, arrY);
                        highestValue = CreepTumorPlacementMap[arrX, arrY];
                    }
                }

            if (highest == null)
                return null;

            return ZergBuildingPlacement.FindPlacement(highest, UnitTypes.ZERG_CREEPTUMORQUEEN, 3, maxDistance: 10, ignoreResourceProximity: true, allowBlockBase: false, requireVision: true);
        }
    }
}
