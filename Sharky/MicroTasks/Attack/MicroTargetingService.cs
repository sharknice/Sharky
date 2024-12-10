namespace Sharky.MicroTasks.Attack
{
    public class MicroTargetingService : TargetingService
    {
        AreaService AreaService;
        public MicroTargetingService(DefaultSharkyBot defaultSharkyBot) : base(defaultSharkyBot.ActiveUnitData, defaultSharkyBot.MapDataService, defaultSharkyBot.BaseData, defaultSharkyBot.TargetingData)
        {
            AreaService = defaultSharkyBot.AreaService;
        }

        public Point2D GetMicroAttackPoint(Point2D attackPoint)
        {
            if (!ActiveUnitData.SelfUnits.Values.Any() || !ActiveUnitData.EnemyUnits.Values.Any())
            {
                return attackPoint;
            }

            var armyPoint = GetArmyPoint(ActiveUnitData.SelfUnits.Values);

            if (armyPoint != null)
            {
                var vector = armyPoint.ToVector2();
                var closestEnemy = ActiveUnitData.EnemyUnits.Values.OrderBy(e => Vector2.DistanceSquared(vector, e.Position)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    return closestEnemy.Position.ToPoint2D();
                }
            }

            return attackPoint;
        }

        public Point2D GetMicroDefensePoint(Point2D attackPoint, Point2D defensePoint)
        {
            if (!ActiveUnitData.SelfUnits.Values.Any() || !ActiveUnitData.EnemyUnits.Values.Any())
            {
                return defensePoint;
            }

            var armyPoint = GetArmyPoint(ActiveUnitData.SelfUnits.Values);

            if (armyPoint != null)
            {
                var vector = armyPoint.ToVector2();
                var closestEnemy = ActiveUnitData.EnemyUnits.Values.OrderBy(e => Vector2.DistanceSquared(vector, e.Position)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    var walkableArea = AreaService.GetAllArea(defensePoint).Where(p => MapDataService.PathWalkable(p));
                    var safestPosition = walkableArea.Where(p => IsCloserToFriendly(p, ActiveUnitData.SelfUnits.Values, ActiveUnitData.EnemyUnits.Values)).OrderByDescending(p => GetMinDistance(p.ToVector2(), ActiveUnitData.EnemyUnits.Values)).FirstOrDefault();
                    if (safestPosition != null)
                    {
                        return safestPosition;
                    }
                    safestPosition = walkableArea.OrderByDescending(p => GetMinDistance(p.ToVector2(), ActiveUnitData.EnemyUnits.Values)).FirstOrDefault();
                    if (safestPosition != null)
                    {
                        return safestPosition;
                    }
                }
            }

            return defensePoint;
        }

        private bool IsCloserToFriendly(Point2D position, Dictionary<ulong, UnitCalculation>.ValueCollection friendlyUnits, Dictionary<ulong, UnitCalculation>.ValueCollection enemyUnits)
        {
            var closestFriendlyDistance = GetMinDistance(position.ToVector2(), friendlyUnits);
            var closestEnemyDistance = GetMinDistance(position.ToVector2(), enemyUnits);
            return closestFriendlyDistance < closestEnemyDistance;
        }

        private float GetMinDistance(Vector2 position, Dictionary<ulong, UnitCalculation>.ValueCollection units)
        {
            return units.Min(unit => Vector2.Distance(position, unit.Position));
        }
    }
}
