namespace Sharky.Concaves
{
    public class ConcaveService
    {
        TargetingService TargetingService;
        MapDataService MapDataService;
        ChokePointsService ChokePointsService;

        public ConcaveService(DefaultSharkyBot defaultSharkyBot)
        {
            TargetingService = defaultSharkyBot.TargetingService;
            MapDataService = defaultSharkyBot.MapDataService;
            ChokePointsService = defaultSharkyBot?.ChokePointsService;
        }

        List<UnitCalculation> GetUnitLine(IEnumerable<UnitCommander> commanders)
        {
            var average = TargetingService.GetArmyPoint(commanders).ToVector2();
            var furthest = commanders.OrderByDescending(c => Vector2.DistanceSquared(average, c.UnitCalculation.Position)).FirstOrDefault();
            var remaining = commanders.Where(c => c.UnitCalculation.Unit.Tag != furthest.UnitCalculation.Unit.Tag).Select(c => c.UnitCalculation).ToList();
            var unitLine = new List<UnitCalculation> { furthest.UnitCalculation };
            while (remaining.Any())
            {
                var closest = remaining.OrderBy(c => Vector2.DistanceSquared(unitLine.LastOrDefault().Position, c.Position)).FirstOrDefault();
                unitLine.Add(closest);
                remaining.Remove(closest);
            }

            return unitLine;
        }

        List<Vector2> GetPositionLine(List<Vector2> concavePositions)
        {
            var average = TargetingService.GetArmyPoint(concavePositions).ToVector2();
            var furthest = concavePositions.OrderByDescending(c => Vector2.DistanceSquared(average, c)).FirstOrDefault();
            var remaining = concavePositions.Where(c => c != furthest).ToList();
            var unitLine = new List<Vector2> { furthest };
            while (remaining.Any())
            {
                var closest = remaining.OrderBy(c => Vector2.DistanceSquared(unitLine.LastOrDefault(), c)).FirstOrDefault();
                unitLine.Add(closest);
                remaining.Remove(closest);
            }

            return unitLine;
        }

        public void AssignCommandersToConcavePositions(ConcaveGroupData group, IEnumerable<UnitCommander> commanders, List<Vector2> concavePositions, bool ground, bool air, bool hybrid)
        {
            var orderedPositions = GetPositionLine(concavePositions);
            var commanderLine = GetUnitLine(commanders);
            var firstCommander = commanderLine.FirstOrDefault();

            var positionLine = orderedPositions.ToList();
            if (Vector2.DistanceSquared(firstCommander.Position, orderedPositions.LastOrDefault()) < Vector2.DistanceSquared(firstCommander.Position, orderedPositions.FirstOrDefault()))
            {
                positionLine.Reverse();
            }

            int index = 0;
            foreach (var commander in commanderLine)
            {
                if (ground)
                {
                    group.GroundConcavePoints[commander.Unit.Tag] = positionLine[index];
                }
                else if (air)
                {
                    group.AirConcavePoints[commander.Unit.Tag] = positionLine[index];
                }
                else if ( hybrid)
                {
                    group.HybridConcavePoints[commander.Unit.Tag] = positionLine[index];
                }
                index++;
            }
        }

        public UnitCalculation GetThreat(UnitCommander centerCommander)
        {
            var attack = centerCommander.UnitCalculation.NearbyEnemies.OrderBy(e => Vector2.DistanceSquared(centerCommander.UnitCalculation.Position, e.Position) - (e.Range * e.Range)).FirstOrDefault();
            if (attack != null)
            {
                return attack;
            }
            
            return null;
        }

        public Vector2 GetPositionFromRange(float targetX, float targetY, float positionX, float positionY, float range)
        {
            var angle = Math.Atan2(targetY - positionY, positionX - targetX);
            var x = range * Math.Cos(angle);
            var y = range * Math.Sin(angle);
            return new Vector2(targetX + (float)x, targetY - (float)y);
        }

        public List<Vector2> GetConcavePositions(Vector2 target, Vector2 startingPosition, float spreadDistance, float radius, int spotsNeeded, bool ground, bool air)
        {
            var positions = new List<Vector2>();

            var spots = GetConcavePositions(target, startingPosition, spreadDistance, radius, ground, air);
            positions.AddRange(spots);

            while (positions.Count < spotsNeeded)
            {
                var moreSpots = new List<Vector2>();
                foreach (var spot in spots)
                {
                    moreSpots.AddRange(GetConcavePositions(target, spot, spreadDistance, radius, ground, air));
                }
                moreSpots = moreSpots.Where(s => !positions.Any(p => Vector2.Distance(p, s) < spreadDistance)).Distinct().ToList();
                if (moreSpots.Count == 0) { break; }
                positions.AddRange(moreSpots);
                spots = moreSpots;
                positions = positions.Distinct().ToList();
            }

            return positions.Distinct().OrderBy(p => Vector2.DistanceSquared(p, startingPosition)).Take(spotsNeeded).ToList();
        }

        List<Vector2> GetConcavePositions(Vector2 target, Vector2 startingPosition, float spreadDistance, float radius, bool ground, bool air)
        {
            var positions = new List<Vector2>();

            var a = startingPosition;
            var b = target;
            var l3 = radius;
            var l2 = radius;
            var l1 = spreadDistance;

            var thing1 = Math.Atan2(b.Y - a.Y, b.X - a.X);

            var top = Math.Pow(l1, 2) + Math.Pow(l3, 2) - Math.Pow(l2, 2);
            var bottom = 2 * l1 * l3;
            var thing2 = Math.Acos(top / bottom);

            if (!double.IsNaN(thing1) && !double.IsNaN(thing2))
            {
                var x1 = a.X + (l1 * Math.Cos(thing1 + thing2));
                var y1 = a.Y + (l1 * Math.Sin(thing1 + thing2));
                var option1 = new Vector2((float)x1, (float)y1);
                if (Safe(ground, air, option1))
                {
                    positions.Add(option1);
                }

                var x2 = a.X + (l1 * Math.Cos(thing1 - thing2));
                var y2 = a.Y + (l1 * Math.Sin(thing1 - thing2));
                var option2 = new Vector2((float)x2, (float)y2);
                if (Safe(ground, air, option2))
                {
                    positions.Add(option2);
                }
            }

            return positions;
        }

        private bool Safe(bool ground, bool air, Vector2 position)
        {
            if (ground)
            {
                if (SafeForGround(position))
                {
                    if (!air || SafeForAir(position))
                    {
                        return true;
                    }
                }
            }
            else if (air && SafeForAir(position))
            {
                return true;
            }
            return false;
        }

        private bool SafeForGround(Vector2 option1)
        {
            return MapDataService.PathWalkable(option1) && !MapDataService.PathBlocked(option1) && MapDataService.EnemyGroundDpsInRange(option1.ToPoint2D()) == 0;
        }

        private bool SafeForAir(Vector2 option1)
        {
            return MapDataService.PathFlyable(option1) && MapDataService.EnemyAirDpsInRange(option1.ToPoint2D()) == 0;
        }
    }
}
