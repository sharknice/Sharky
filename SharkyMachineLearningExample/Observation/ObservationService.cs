using SC2APIProtocol;
using Sharky.Pathing;
using Sharky;

namespace SharkyMachineLearningExample.Observation
{
    public class ObservationService
    {
        public float[] GetFlattenedObservation(IEnumerable<UnitCommander> unitCommanders, IEnumerable<UnitCalculation> enemyUnits, MapData mapData)
        {
            var observationSpace = GetObservationSpace(unitCommanders, enemyUnits, mapData);
            return GetFlattenedObservation(observationSpace);
        }

        private ObservationSpace GetObservationSpace(IEnumerable<UnitCommander> unitCommanders, IEnumerable<UnitCalculation> enemyUnits, MapData mapData)
        {
            var friendlies = unitCommanders.Select(c => GetUnitStateFromUnit(c.UnitCalculation.Unit)).ToList();
            var enemies = enemyUnits.Select(u => GetUnitStateFromUnit(u.Unit)).ToList();
            var walkableArea = ExtractWalkableArea(mapData);

            return new ObservationSpace { EnemyUnits = enemies, FriendlyUnits = friendlies, MapHeight = mapData.MapHeight, MapWidth = mapData.MapWidth, WalkableArea = walkableArea };
        }

        private UnitState GetUnitStateFromUnit(Unit unit)
        {
            return new UnitState { Health = unit.Health, Shields = unit.Shield, Tag = (long)unit.Tag, WeaponCooldown = unit.WeaponCooldown, X = unit.Pos.X, Y = unit.Pos.Y };
        }

        private bool[,] ExtractWalkableArea(MapData mapData)
        {
            bool[,] walkableArea = new bool[mapData.MapWidth, mapData.MapHeight];

            for (int x = 0; x < mapData.MapWidth; x++)
            {
                for (int y = 0; y < mapData.MapHeight; y++)
                {
                    walkableArea[x, y] = mapData.Map[x, y].Walkable;
                }
            }

            return walkableArea;
        }

        private float[] GetFlattenedObservation(ObservationSpace space, int maxUnits = 5)
        {
            List<float> flattenedState = new List<float>();

            // Add map information
            flattenedState.Add(space.MapWidth);
            flattenedState.Add(space.MapHeight);

            // Add walkable area information
            for (int y = 0; y < space.WalkableArea.GetLength(1); y++)
            {
                for (int x = 0; x < space.WalkableArea.GetLength(0); x++)
                {
                    flattenedState.Add(space.WalkableArea[x, y] ? 1f : 0f);
                }
            }

            // Add friendly units
            AddUnitsToFlattenedState(flattenedState, space.FriendlyUnits, maxUnits, true);

            // Add enemy units
            AddUnitsToFlattenedState(flattenedState, space.EnemyUnits, maxUnits, false);

            return flattenedState.ToArray();
        }

        private void AddUnitsToFlattenedState(List<float> flattenedState, List<UnitState> units, int maxUnits, bool isFriendly)
        {
            for (int i = 0; i < maxUnits; i++)
            {
                if (i < units.Count)
                {
                    var unit = units[i];
                    flattenedState.AddRange(new[] { 1f, (float)unit.Tag, unit.X, unit.Y, unit.Health, unit.Shields });
                    if (isFriendly)
                    {
                        flattenedState.Add(unit.WeaponCooldown);
                    }
                }
                else
                {
                    // Add placeholder values for dead or non-existent units
                    flattenedState.AddRange(new[] { 0f, 0f, 0f, 0f, 0f, 0f });
                    if (isFriendly)
                    {
                        flattenedState.Add(0f);
                    }
                }
            }
        }
    }
}
