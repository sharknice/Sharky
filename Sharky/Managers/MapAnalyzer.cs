//using SC2APIProtocol;
//using Sharky.MapAnalysis;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;

//namespace Sharky.Managers
//{
//    public class MapAnalyzer : SharkyManager
//    {
//        public List<BaseLocation> BaseLocations { get; private set; } = new List<BaseLocation>();
//        public Point StartLocation { get; private set; }
//        public BoolGrid Placement;
//        public BoolGrid StartArea;
//        public BoolGrid MainAndPocketArea;
//        private int[,] enemyDistances;
//        private int[,] MainDistancesStore;
//        public int[,] WallDistances;

//        // Positions for wallin, needs better place.
//        public Point2D building1 = null;
//        public Point2D building2 = null;
//        public Point2D building3 = null;

//        public BoolGrid Ramp;
//        public BoolGrid Pathable;
//        //public BoolGrid UnPathable;

//        public BoolGrid Chokes;

//        private Point2D EnemyRamp = null;

//        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
//        {
//            // Determine the start location.
//            foreach (Unit unit in shark.Observation.Observation.RawData.Units)
//                if (unit.Owner == shark.PlayerId && UnitTypes.ResourceCenters.Contains(unit.UnitType))
//                    StartLocation = unit.Pos;

//            List<MineralField> mineralFields = new List<MineralField>();

//            foreach (Unit mineralField in shark.Observation.Observation.RawData.Units)
//                if (UnitTypes.MineralFields.Contains(mineralField.UnitType))
//                    mineralFields.Add(new MineralField() { Pos = mineralField.Pos, Tag = mineralField.Tag });

//            // The Units provided in our observation are not guaranteed to be in the same order every game.
//            // To ensure the base finding algorithm finds the same base location every time we sort the mineral fields by position.
//            mineralFields.Sort((a, b) => (int)(2 * (a.Pos.X + a.Pos.Y * 10000 - b.Pos.X - b.Pos.Y * 10000)));

//            Dictionary<ulong, int> mineralSetIds = new Dictionary<ulong, int>();
//            List<List<MineralField>> mineralSets = new List<List<MineralField>>();
//            int currentSet = 0;
//            foreach (MineralField mineralField in mineralFields)
//            {
//                if (mineralSetIds.ContainsKey(mineralField.Tag))
//                    continue;
//                BaseLocation baseLocation = new BaseLocation();
//                BaseLocations.Add(baseLocation);
//                mineralSetIds.Add(mineralField.Tag, currentSet);
//                baseLocation.MineralFields.Add(mineralField);

//                for (int i = 0; i < baseLocation.MineralFields.Count; i++)
//                {
//                    MineralField mineralFieldA = baseLocation.MineralFields[i];
//                    foreach (MineralField closeMineralField in mineralFields)
//                    {
//                        if (mineralSetIds.ContainsKey(closeMineralField.Tag))
//                            continue;

//                        if (SC2Util.DistanceSq(mineralFieldA.Pos, closeMineralField.Pos) <= 4 * 4)
//                        {
//                            mineralSetIds.Add(closeMineralField.Tag, currentSet);
//                            baseLocation.MineralFields.Add(closeMineralField);
//                        }
//                    }
//                }
//                currentSet++;
//            }

//            List<Gas> gasses = new List<Gas>();
//            foreach (Unit unit in shark.Observation.Observation.RawData.Units)
//                if (UnitTypes.GasGeysers.Contains(unit.UnitType))
//                    gasses.Add(new Gas() { Pos = unit.Pos, Tag = unit.Tag });

//            // The Units provided in our observation are not guaranteed to be in the same order every game.
//            // To ensure the base finding algorithm finds the same base location every time we sort the gasses by position.
//            gasses.Sort((a, b) => (int)(2 * (a.Pos.X + a.Pos.Y * 10000 - b.Pos.X - b.Pos.Y * 10000)));

//            foreach (BaseLocation loc in BaseLocations)
//                DetermineFinalLocation(loc, gasses);

//            Stopwatch stopWatch = Stopwatch.StartNew();

//            int width = Shark.Bot.GameInfo.StartRaw.MapSize.X;
//            int height = Shark.Bot.GameInfo.StartRaw.MapSize.Y;

//            Placement = new ImageBoolGrid(shark.GameInfo.StartRaw.PlacementGrid);
//            StartArea = Placement.GetConnected(SC2Util.To2D(StartLocation));

//            ArrayBoolGrid startLocations = new ArrayBoolGrid(Placement.Width(), Placement.Height());
//            foreach (Point2D startLoc in Shark.Bot.GameInfo.StartRaw.StartLocations)
//                for (int x = -2; x <= 2; x++)
//                    for (int y = -2; y <= 2; y++)
//                        startLocations[(int)startLoc.X + x, (int)startLoc.Y + y] = true;
//            for (int x = -2; x <= 2; x++)
//                for (int y = -2; y <= 2; y++)
//                    startLocations[(int)StartLocation.X + x, (int)StartLocation.Y + y] = true;

//            BoolGrid unPathable;
//            if (Shark.Bot.OldMapData)
//            {
//                unPathable = new ImageBoolGrid(Shark.Bot.GameInfo.StartRaw.PathingGrid).GetAnd(startLocations.Invert());
//                Pathable = unPathable.Invert();
//            }
//            else
//            {
//                Pathable = new ImageBoolGrid(Shark.Bot.GameInfo.StartRaw.PathingGrid).GetOr(startLocations);
//                unPathable = Pathable.Invert();
//            }

//            BoolGrid chokes = Placement.Invert().GetAnd(Pathable);
//            BoolGrid mainExits = chokes.GetAdjacent(StartArea);

//            enemyDistances = EnemyDistances;

//            int dist = 1000;
//            Point2D mainRamp = null;
//            for (int x = 0; x < width; x++)
//                for (int y = 0; y < height; y++)
//                {
//                    if (mainExits[x, y])
//                    {
//                        int newDist = enemyDistances[x, y];
//                        if (newDist < dist)
//                        {
//                            dist = newDist;
//                            mainRamp = SC2Util.Point(x, y);
//                        }
//                    }
//                }

//            Ramp = chokes.GetConnected(mainRamp);

//            BoolGrid pathingWithoutRamp = Pathable.GetAnd(Ramp.Invert());
//            MainAndPocketArea = pathingWithoutRamp.GetConnected(SC2Util.To2D(StartLocation));

//            if (Shark.Bot.MyRace == Race.Protoss)
//                DetermineWall(Ramp, unPathable);

//            WallDistances = Distances(unPathable);

//            stopWatch.Stop();
//        }

//        public Point2D GetMainRamp()
//        {
//            float totalPoints = 0;
//            float totalX = 0;
//            float totalY = 0;
//            for (int x = 0; x < Ramp.Width(); x++)
//                for (int y = 0; y < Ramp.Height(); y++)
//                {
//                    if (Ramp[x, y])
//                    {
//                        totalX += x;
//                        totalY += y;
//                        totalPoints++;
//                    }
//                }
//            return SC2Util.Point((int)(totalX / totalPoints) + 1f, (int)(totalY / totalPoints) + 1f);
//        }

//        private void DetermineFinalLocation(BaseLocation loc, List<Gas> gasses)
//        {
//            for (int i = 0; i < gasses.Count; i++)
//            {
//                foreach (MineralField field in loc.MineralFields)
//                {
//                    if (SC2Util.DistanceSq(field.Pos, gasses[i].Pos) <= 8 * 8)
//                    {
//                        loc.Gasses.Add(gasses[i]);
//                        gasses[i] = gasses[gasses.Count - 1];
//                        gasses.RemoveAt(gasses.Count - 1);
//                        i--;
//                        break;
//                    }
//                }
//            }

//            if (loc.Gasses.Count == 1)
//            {
//                for (int i = 0; i < gasses.Count; i++)
//                    if (SC2Util.DistanceSq(loc.Gasses[0].Pos, gasses[i].Pos) <= 8 * 8)
//                    {
//                        loc.Gasses.Add(gasses[i]);
//                        gasses[i] = gasses[gasses.Count - 1];
//                        gasses.RemoveAt(gasses.Count - 1);
//                        i--;
//                        break;
//                    }
//            }

//            float x = 0;
//            float y = 0;
//            foreach (MineralField field in loc.MineralFields)
//            {
//                x += (int)field.Pos.X;
//                y += (int)field.Pos.Y;
//            }
//            x /= loc.MineralFields.Count;
//            y /= loc.MineralFields.Count;

//            // Round to nearest half position. Nexii are 5x5 and therefore always centered in the middle of a tile.
//            x = (int)(x) + 0.5f;
//            y = (int)(y) + 0.5f;

//            // Temporary position, we still need a proper position.
//            loc.Pos = SC2Util.Point(x, y);


//            MineralField closest = null;
//            float distance = 10000;
//            foreach (MineralField field in loc.MineralFields)
//                if (SC2Util.DistanceGrid(field.Pos, loc.Pos) < distance)
//                {
//                    distance = SC2Util.DistanceGrid(field.Pos, loc.Pos);
//                    closest = field;
//                }

//            // Move the estimated base position slightly away from the closest mineral.
//            // This ensures that the base location will not end up on the far side of the minerals.
//            if (closest.Pos.X < loc.Pos.X)
//                loc.Pos.X += 2;
//            else if (closest.Pos.X > loc.Pos.X)
//                loc.Pos.X -= 2;
//            if (closest.Pos.Y < loc.Pos.Y)
//                loc.Pos.Y += 2;
//            else if (closest.Pos.Y > loc.Pos.Y)
//                loc.Pos.Y -= 2;

//            bool test = SC2Util.DistanceSq(loc.Pos, new Point2D() { X = 127.5f, Y = 77.5f }) <= 10 * 10;

//            float closestDist = 1000000;
//            Point2D approxPos = loc.Pos;
//            for (int i = 0; i < 20; i++)
//            {
//                for (int j = 0; j == 0 || j < i; j++)
//                {
//                    float maxDist;
//                    Point2D newPos;
//                    newPos = SC2Util.Point(approxPos.X + i - j, approxPos.Y + j);
//                    maxDist = checkPosition(newPos, loc);
//                    if (maxDist < closestDist)
//                    {
//                        loc.Pos = newPos;
//                        closestDist = maxDist;
//                    }

//                    newPos = SC2Util.Point(approxPos.X + i - j, approxPos.Y - j);
//                    maxDist = checkPosition(newPos, loc);
//                    if (maxDist < closestDist)
//                    {
//                        loc.Pos = newPos;
//                        closestDist = maxDist;
//                    }

//                    newPos = SC2Util.Point(approxPos.X - i + j, approxPos.Y + j);
//                    maxDist = checkPosition(newPos, loc);
//                    if (maxDist < closestDist)
//                    {
//                        loc.Pos = newPos;
//                        closestDist = maxDist;
//                    }

//                    newPos = SC2Util.Point(approxPos.X - i + j, approxPos.Y - j);
//                    maxDist = checkPosition(newPos, loc);
//                    if (maxDist < closestDist)
//                    {
//                        loc.Pos = newPos;
//                        closestDist = maxDist;
//                    }
//                }
//            }

//            //if (loc.Gasses.Count != 2)
//            //    FileUtil.Debug("Wrong number of gasses, found: " + loc.Gasses.Count);
//            //if (closestDist >= 999999)
//            //    DebugUtil.WriteLine("Unable to find proper base placement: " + loc.Pos);

//        }

//        public int MapHeight(int x, int y)
//        {
//            if (x >= Shark.Bot.GameInfo.StartRaw.TerrainHeight.Size.X || x < 0 || y < 0 || y >= Shark.Bot.GameInfo.StartRaw.TerrainHeight.Size.Y)
//            {
//                return 0;
//            }
//            return SC2Util.GetDataValue(Shark.Bot.GameInfo.StartRaw.TerrainHeight, x, y);
//        }

//        public int GroundPathable(int x, int y)
//        {
//            if (x >= Shark.Bot.GameInfo.StartRaw.PathingGrid.Size.X || x < 0 || y < 0 || y >= Shark.Bot.GameInfo.StartRaw.PathingGrid.Size.Y)
//            {
//                return 0;
//            }
//            return SC2Util.GetDataValue(Shark.Bot.GameInfo.StartRaw.PathingGrid, x, y);
//        }

//        public int MapVisibility(Point2D point)
//        {
//            if (point.X >= Shark.Bot.Observation.Observation.RawData.MapState.Visibility.Size.X || point.X < 0 || point.Y < 0 || point.Y >= Shark.Bot.Observation.Observation.RawData.MapState.Visibility.Size.Y)
//            {
//                return 0;
//            }
//            return SC2Util.GetDataValue(Shark.Bot.Observation.Observation.RawData.MapState.Visibility, (int)point.X, (int)point.Y);
//        }

//        private void DetermineWall(BoolGrid ramp, BoolGrid unPathable)
//        {
//            BoolGrid rampAdjacent = unPathable.GetAdjacent(ramp);
//            BoolGrid rampSides = unPathable.GetConnected(rampAdjacent, 5);
//            List<BoolGrid> sides = rampSides.GetGroups();

//            BoolGrid shrunkenStart = StartArea.Shrink();

//            List<Point2D> building1Positions = Placable(sides[0], shrunkenStart).ToList();
//            List<Point2D> building2Positions = Placable(sides[1], shrunkenStart).ToList();

//            float wallScore = 1000;


//            foreach (Point2D p1 in building1Positions)
//                foreach (Point2D p2 in building2Positions)
//                {
//                    if (System.Math.Abs(p1.X - p2.X) < 3 && System.Math.Abs(p1.Y - p2.Y) < 3)
//                        continue;

//                    float newScore = SC2Util.DistanceGrid(p1, p2);
//                    if (newScore >= wallScore)
//                        continue;

//                    for (float i = -2.5f; i < 3; i++)
//                    {
//                        if (CheckPylon(SC2Util.Point(p1.X + 2.5f, p1.Y + i), p1, p2))
//                        {
//                            wallScore = newScore;
//                            building1 = p1;
//                            building2 = p2;
//                            building3 = SC2Util.Point(p1.X + 2.5f, p1.Y + i);
//                        }
//                        if (CheckPylon(SC2Util.Point(p1.X - 2.5f, p1.Y + i), p1, p2))
//                        {
//                            wallScore = newScore;
//                            building1 = p1;
//                            building2 = p2;
//                            building3 = SC2Util.Point(p1.X - 2.5f, p1.Y + i);
//                        }
//                        if (CheckPylon(SC2Util.Point(p1.X + i, p1.Y + 2.5f), p1, p2))
//                        {
//                            wallScore = newScore;
//                            building1 = p1;
//                            building2 = p2;
//                            building3 = SC2Util.Point(p1.X + i, p1.Y + 2.5f);
//                        }
//                        if (CheckPylon(SC2Util.Point(p1.X + i, p1.Y - 2.5f), p1, p2))
//                        {
//                            wallScore = newScore;
//                            building1 = p1;
//                            building2 = p2;
//                            building3 = SC2Util.Point(p1.X + i, p1.Y - 2.5f);
//                        }
//                    }
//                }

//        }

//        private bool CheckPylon(Point2D pylon, Point2D p1, Point2D p2)
//        {
//            if (!StartArea[SC2Util.Point(pylon.X, pylon.Y)])
//                return false;
//            if (!StartArea[SC2Util.Point(pylon.X + 0.6f, pylon.Y)])
//                return false;
//            if (!StartArea[SC2Util.Point(pylon.X, pylon.Y + 0.6f)])
//                return false;
//            if (!StartArea[SC2Util.Point(pylon.X + 0.6f, pylon.Y + 0.6f)])
//                return false;

//            float dist = System.Math.Max(System.Math.Abs(pylon.X - p2.X), Math.Abs(pylon.Y - p2.Y));
//            return dist > 2.4 && dist < 2.6;
//        }

//        private BoolGrid Placable(BoolGrid around, BoolGrid shrunkenStart)
//        {
//            ArrayBoolGrid result = new ArrayBoolGrid(around.Width(), around.Height());
//            for (int x = 0; x < around.Width(); x++)
//                for (int y = 0; y < around.Height(); y++)
//                {
//                    if (around[x, y])
//                    {
//                        for (int i = -2; i <= 2; i++)
//                        {
//                            if (shrunkenStart[x + i, y - 2])
//                                result[x + i, y - 2] = true;
//                            if (shrunkenStart[x + i, y + 2])
//                                result[x + i, y + 2] = true;
//                            if (shrunkenStart[x + 2, y + i])
//                                result[x + 2, y + i] = true;
//                            if (shrunkenStart[x - 2, y + i])
//                                result[x - 2, y + i] = true;
//                        }
//                    }
//                }
//            return result;
//        }

//        private float checkPosition(Point2D pos, BaseLocation loc)
//        {
//            foreach (MineralField mineralField in loc.MineralFields)
//                if (SC2Util.DistanceGrid(mineralField.Pos, pos) <= 10
//                    && System.Math.Abs(mineralField.Pos.X - pos.X) <= 5.5
//                    && System.Math.Abs(mineralField.Pos.Y - pos.Y) <= 5.5)
//                {
//                    return 100000000;
//                }
//            foreach (Gas gas in loc.Gasses)
//            {
//                if (SC2Util.DistanceGrid(gas.Pos, pos) <= 11
//                    && System.Math.Abs(gas.Pos.X - pos.X) <= 6.1
//                    && System.Math.Abs(gas.Pos.Y - pos.Y) <= 6.1)
//                {
//                    return 100000000;
//                }
//                if (SC2Util.DistanceSq(gas.Pos, pos) >= 11 * 11)
//                    return 100000000;
//            }

//            // Check if a resource center can actually be built here.
//            for (float x = -2.5f; x < 2.5f + 0.1f; x++)
//                for (float y = -2.5f; y < 2.5f + 0.1f; y++)
//                    if (!SC2Util.GetTilePlacable((int)System.Math.Round(pos.X + x), (int)System.Math.Round(pos.Y + y)))
//                        return 100000000;

//            float maxDist = 0;
//            foreach (MineralField mineralField in loc.MineralFields)
//                maxDist += SC2Util.DistanceSq(mineralField.Pos, pos);

//            foreach (Gas gas in loc.Gasses)
//                maxDist += SC2Util.DistanceSq(gas.Pos, pos);
//            return maxDist;
//        }

//        public Point2D CrossSpawn()
//        {
//            int dist = 0;
//            Point2D crossSpawn = null;
//            foreach (Point2D enemy in Shark.Bot.GameInfo.StartRaw.StartLocations)
//            {
//                int enemyDist = (int)SC2Util.DistanceSq(enemy, StartLocation);
//                if (enemyDist > dist)
//                {
//                    crossSpawn = enemy;
//                    dist = enemyDist;
//                }
//            }
//            return crossSpawn;
//        }

//        public int[,] EnemyDistances
//        {
//            get
//            {
//                if (enemyDistances == null)
//                    enemyDistances = Distances(CrossSpawn());
//                return enemyDistances;
//            }
//        }

//        public int[,] MainDistances
//        {
//            get
//            {
//                if (MainDistancesStore == null)
//                    MainDistancesStore = Distances(SC2Util.To2D(StartLocation));
//                return MainDistancesStore;
//            }
//        }

//        public int[,] Distances(Point2D pos)
//        {
//            int width = Shark.Bot.GameInfo.StartRaw.MapSize.X;
//            int height = Shark.Bot.GameInfo.StartRaw.MapSize.Y;
//            int[,] distances = new int[width, height];

//            for (int x = 0; x < width; x++)
//                for (int y = 0; y < height; y++)
//                    distances[x, y] = 1000000000;
//            distances[(int)pos.X, (int)pos.Y] = 0;

//            Queue<Point2D> q = new Queue<Point2D>();
//            q.Enqueue(pos);

//            while (q.Count > 0)
//            {
//                Point2D cur = q.Dequeue();
//                check(Pathable, distances, q, SC2Util.Point(cur.X + 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//                check(Pathable, distances, q, SC2Util.Point(cur.X - 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y + 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y - 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//            }

//            return distances;
//        }

//        public int[,] Distances(BoolGrid start)
//        {
//            int width = Shark.Bot.GameInfo.StartRaw.MapSize.X;
//            int height = Shark.Bot.GameInfo.StartRaw.MapSize.Y;
//            int[,] distances = new int[width, height];

//            for (int x = 0; x < width; x++)
//                for (int y = 0; y < height; y++)
//                    distances[x, y] = 1000000000;


//            Queue<Point2D> q = new Queue<Point2D>();
//            for (int x = 0; x < width; x++)
//                for (int y = 0; y < height; y++)
//                {
//                    if (start[x, y])
//                    {
//                        distances[x, y] = 0;
//                        q.Enqueue(SC2Util.Point(x, y));
//                    }
//                }

//            while (q.Count > 0)
//            {
//                Point2D cur = q.Dequeue();
//                check(Pathable, distances, q, SC2Util.Point(cur.X + 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//                check(Pathable, distances, q, SC2Util.Point(cur.X - 1, cur.Y), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y + 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//                check(Pathable, distances, q, SC2Util.Point(cur.X, cur.Y - 1), width, height, distances[(int)cur.X, (int)cur.Y] + 1);
//            }

//            return distances;
//        }

//        private void check(BoolGrid pathingData, int[,] distances, Queue<Point2D> q, Point2D pos, int width, int height, int newVal)
//        {
//            if (check(pathingData, pos, width, height) && distances[(int)pos.X, (int)pos.Y] == 1000000000)
//            {
//                q.Enqueue(pos);
//                distances[(int)pos.X, (int)pos.Y] = newVal;
//            }
//        }

//        private bool check(BoolGrid pathingData, Point2D pos, int width, int height)
//        {
//            if (pos.X < 0 || pos.X >= width || pos.Y < 0 || pos.Y >= height)
//                return false;
//            if (pathingData[pos])
//                return true;

//            foreach (Point2D p in Shark.Bot.GameInfo.StartRaw.StartLocations)
//                if (SC2Util.DistanceGrid(pos, p) <= 3)
//                    return true;
//            if (SC2Util.DistanceGrid(pos, StartLocation) <= 3)
//                return true;
//            return false;
//        }

//        public bool IsChoke(Point2D point)
//        {
//            if (Chokes == null)
//            {
//                Chokes = Placement.Invert().GetAnd(Pathable);
//            }
//            return Chokes[(int)Math.Round(point.X), (int)Math.Round(point.Y)];
//        }

//        public Point2D GetEnemyRamp()
//        {
//            if (EnemyRamp != null)
//                return EnemyRamp;
//            if (Shark.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
//                return null;

//            int width = Shark.Bot.GameInfo.StartRaw.MapSize.X;
//            int height = Shark.Bot.GameInfo.StartRaw.MapSize.Y;

//            Point2D start = Shark.Bot.TargetManager.PotentialEnemyStartLocations[0];
//            BoolGrid enemyStartArea = Placement.GetConnected(start);


//            BoolGrid chokes = Placement.Invert().GetAnd(Pathable);
//            BoolGrid mainExits = chokes.GetAdjacent(enemyStartArea);

//            int[,] startDistances = Distances(SC2Util.To2D(StartLocation));

//            int dist = 1000;
//            Point2D mainRamp = null;
//            for (int x = 0; x < width; x++)
//                for (int y = 0; y < height; y++)
//                {
//                    if (mainExits[x, y])
//                    {
//                        int newDist = startDistances[x, y];
//                        FileUtil.Debug("Ramp distance: " + newDist);
//                        if (newDist < dist)
//                        {
//                            dist = newDist;
//                            mainRamp = SC2Util.Point(x, y);
//                        }
//                    }
//                }

//            BoolGrid enemyRamp = chokes.GetConnected(mainRamp);

//            float totalX = 0;
//            float totalY = 0;
//            float count = 0;
//            for (int x = 0; x < width; x++)
//                for (int y = 0; y < height; y++)
//                {
//                    if (enemyRamp[x, y])
//                    {
//                        totalX += x;
//                        totalY += y;
//                        count++;
//                    }
//                }

//            EnemyRamp = new Point2D() { X = totalX / count, Y = totalY / count };
//            return EnemyRamp;
//        }

//        public Point2D Walk(Point2D start, int[,] distances, int steps)
//        {
//            Point2D cur = start;
//            int dx = 0;
//            int dy = 0;
//            for (int i = 0; i <= steps; i++)
//            {
//                List<Point2D> newDirections = new List<Point2D>();
//                newDirections.Add(SC2Util.Point(cur.X + 1, cur.Y));
//                newDirections.Add(SC2Util.Point(cur.X - 1, cur.Y));
//                newDirections.Add(SC2Util.Point(cur.X, cur.Y + 1));
//                newDirections.Add(SC2Util.Point(cur.X, cur.Y - 1));

//                for (int j = newDirections.Count - 1; j >= 0; j--)
//                {
//                    Point2D next = newDirections[j];
//                    if (distances[(int)cur.X, (int)cur.Y] <= distances[(int)next.X, (int)next.Y])
//                        newDirections.RemoveAt(j);
//                }

//                if (newDirections.Count == 0)
//                    break;

//                Point2D goTo;
//                if (newDirections.Count == 1 || newDirections[0].X - cur.X != dx || newDirections[0].Y - cur.Y != dy)
//                    goTo = newDirections[0];
//                else
//                    goTo = newDirections[1];

//                dx = (int)(goTo.X - cur.X);
//                dy = (int)(goTo.Y - cur.Y);
//                cur = goTo;

//                if (distances[(int)cur.X, (int)cur.Y] == 0)
//                    break;
//            }
//            return cur;
//        }

//        public BaseLocation GetEnemyNatural()
//        {
//            if (Shark.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
//                return null;
//            int[,] distances = Distances(Shark.Bot.TargetManager.PotentialEnemyStartLocations[0]);
//            int dist = 1000000000;
//            BaseLocation enemyNatural = null;
//            foreach (BaseLocation loc in Shark.Bot.MapAnalyzer.BaseLocations)
//            {
//                int distanceToMain = distances[(int)loc.Pos.X, (int)loc.Pos.Y];

//                if (distanceToMain <= 5)
//                    continue;

//                if (distanceToMain < dist)
//                {
//                    dist = distanceToMain;
//                    enemyNatural = loc;
//                }
//            }
//            return enemyNatural;
//        }

//        public BaseLocation GetEnemyThird()
//        {
//            if (Shark.Bot.TargetManager.PotentialEnemyStartLocations.Count != 1)
//                return null;
//            float dist = 1000000000;
//            BaseLocation enemyNatural = GetEnemyNatural();
//            BaseLocation enemyThird = null;
//            foreach (BaseLocation loc in Shark.Bot.MapAnalyzer.BaseLocations)
//            {
//                float distanceToMain = SC2Util.DistanceSq(Shark.Bot.TargetManager.PotentialEnemyStartLocations[0], loc.Pos);

//                if (distanceToMain <= 4)
//                    continue;

//                if (SC2Util.DistanceSq(enemyNatural.Pos, loc.Pos) <= 2 * 2)
//                    continue;

//                if (distanceToMain < dist)
//                {
//                    dist = distanceToMain;
//                    enemyThird = loc;
//                }
//            }
//            return enemyThird;
//        }
//    }
//}
