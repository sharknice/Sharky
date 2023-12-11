namespace Sharky.Managers
{
    public class BaseManager : SharkyManager
    {
        ImageData PlacementGrid;

        SharkyUnitData SharkyUnitData;
        ActiveUnitData ActiveUnitData;
        IPathFinder PathFinder;
        UnitCountService UnitCountService;
        BaseData BaseData;
        MapDataService MapDataService;

        Dictionary<string, BaseLocationData> GeneratedBaseLocationData;

        public BaseManager(SharkyUnitData sharkyUnitData, ActiveUnitData activeUnitData, IPathFinder pathFinder, UnitCountService unitCountService, BaseData baseData, MapDataService mapDataService)
        {
            SharkyUnitData = sharkyUnitData;
            ActiveUnitData = activeUnitData;
            PathFinder = pathFinder;
            UnitCountService = unitCountService;
            BaseData = baseData;
            MapDataService = mapDataService;
            BaseData.BaseLocations = new List<BaseLocation>();

            GeneratedBaseLocationData = LoadBaseLocationData();
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            PlacementGrid = gameInfo.StartRaw.PlacementGrid;

            var mineralFields = new List<Unit>();
            foreach (var unit in observation.Observation.RawData.Units)
            {
                if (SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)unit.UnitType))
                {
                    mineralFields.Add(unit);
                }
            }
            mineralFields = mineralFields.OrderBy(m => m.Pos.X).ThenBy(m => m.Pos.Y).ToList();

            var mineralGroups = new Dictionary<ulong, int>();
            int currentSet = 0;
            foreach (var mineralField in mineralFields)
            {
                if (!mineralGroups.ContainsKey(mineralField.Tag))
                {
                    var baseLocation = new BaseLocation();
                    BaseData.BaseLocations.Add(baseLocation);
                    mineralGroups.Add(mineralField.Tag, currentSet);
                    baseLocation.MineralFields.Add(mineralField);

                    for (int i = 0; i < baseLocation.MineralFields.Count; i++)
                    {
                        var mineralFieldA = baseLocation.MineralFields[i];
                        foreach (var closeMineralField in mineralFields)
                        {
                            if (mineralGroups.ContainsKey(closeMineralField.Tag))
                            {
                                continue;
                            }

                            if (Vector2.DistanceSquared(new Vector2(mineralFieldA.Pos.X, mineralFieldA.Pos.Y), new Vector2(closeMineralField.Pos.X, closeMineralField.Pos.Y)) <= 16)
                            {
                                mineralGroups.Add(closeMineralField.Tag, currentSet);
                                baseLocation.MineralFields.Add(closeMineralField);
                            }
                        }
                    }
                    currentSet++;
                }
            }

            var gasses = new List<Unit>();
            foreach (var unit in observation.Observation.RawData.Units)
            {
                if (SharkyUnitData.GasGeyserTypes.Contains((UnitTypes)unit.UnitType))
                {
                    gasses.Add(unit);
                }
            }
            gasses = gasses.OrderBy(g => g.Pos.X).ThenBy(g => g.Pos.Y).ToList();

            foreach (var location in BaseData.BaseLocations)
            {
                DetermineFinalLocation(location, gasses);
                SetMineralLineLocation(location);
            }

            if (gameInfo.MapName.ToLower().Contains("blackburn"))
            {
                BaseData.BaseLocations.RemoveAll(b => b.Location.X == 72.5f && b.Location.Y == 14.5f); // unreachable base unless rocks are destroyed
            }

            var startingUnit = observation.Observation.RawData.Units.FirstOrDefault(u => u.Alliance == Alliance.Self && SharkyUnitData.ResourceCenterTypes.Contains((UnitTypes)u.UnitType));
            var enemystartingLocation = gameInfo.StartRaw.StartLocations.LastOrDefault();

            if (startingUnit == null || enemystartingLocation == null)
            {
                BaseData.SelfBases = new List<BaseLocation>();
                BaseData.EnemyBases = new List<BaseLocation>();
                BaseData.BaseLocations = new List<BaseLocation>();
                BaseData.EnemyBaseLocations = new List<BaseLocation>();
                return;
            }

            var folder = GetGneratedBaseDataFolder();
            var fileName = GetGeneratedBaseDataFileName(gameInfo.MapName, folder, gameInfo.StartRaw.StartLocations.First());
            if (GeneratedBaseLocationData.ContainsKey(FilePath.GetFileNameWithoutExtension(fileName)))
            {
                Console.WriteLine($"loading {fileName}");
                var loadedData = GeneratedBaseLocationData[FilePath.GetFileNameWithoutExtension(fileName)];
                BaseData.BaseLocations = BaseData.BaseLocations.OrderBy(b => loadedData.SelfBaseLocations.FindIndex(d => d.X == b.Location.X && d.Y == b.Location.Y)).ToList();
                BaseData.EnemyBaseLocations = BaseData.BaseLocations.OrderBy(b => loadedData.EnemyBaseLocations.FindIndex(d => d.X == b.Location.X && d.Y == b.Location.Y)).ToList();
            }
            else
            {
                GenerateBaseLocations(startingUnit, enemystartingLocation);
                Console.WriteLine($"saving {fileName}");
                SaveBaseLocationData(folder, fileName, BaseData);
            }

            BaseData.MainBase = BaseData.BaseLocations.FirstOrDefault();
            BaseData.MainBase.ResourceCenter = startingUnit;
            BaseData.SelfBases = new List<BaseLocation> { BaseData.MainBase };
            BaseData.EnemyBases = new List<BaseLocation> { BaseData.EnemyBaseLocations.FirstOrDefault() };
            BaseData.EnemyNaturalBase = BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault();
            if (ActiveUnitData.NeutralUnits.Values.Any(u => Vector2.DistanceSquared(u.Position,BaseData.EnemyNaturalBase.Location.ToVector2()) < 4))
            {
                BaseData.EnemyNaturalBase = BaseData.EnemyBaseLocations.Skip(2).FirstOrDefault();
            }

            SetupMiningInfo();
        }

        private void GenerateBaseLocations(Unit startingUnit, Point2D enemystartingLocation)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Calculating base expansion order");
            var closerBases = BaseData.BaseLocations.Where(b => PathFinder.GetGroundPath(startingUnit.Pos.X + 4, startingUnit.Pos.Y + 4, b.Location.X, b.Location.Y, 0).Count() <= PathFinder.GetGroundPath(enemystartingLocation.X + 4, enemystartingLocation.Y + 4, b.Location.X, b.Location.Y, 0).Count()).ToList();
            var fartherBases = BaseData.BaseLocations.Where(b => PathFinder.GetGroundPath(startingUnit.Pos.X + 4, startingUnit.Pos.Y + 4, b.Location.X, b.Location.Y, 0).Count() > PathFinder.GetGroundPath(enemystartingLocation.X + 4, enemystartingLocation.Y + 4, b.Location.X, b.Location.Y, 0).Count()).ToList();
            BaseData.BaseLocations = GetOrderedBaseLocations(startingUnit, closerBases, enemystartingLocation);
            BaseData.BaseLocations.AddRange(fartherBases.OrderByDescending(b => PathFinder.GetGroundPath(enemystartingLocation.X + 4, enemystartingLocation.Y + 4, b.Location.X, b.Location.Y, 0).Count()));
            BaseData.EnemyBaseLocations = fartherBases.OrderBy(b => PathFinder.GetGroundPath(enemystartingLocation.X + 4, enemystartingLocation.Y + 4, b.Location.X, b.Location.Y, 0).Count()).ToList();
            BaseData.EnemyBaseLocations.AddRange(closerBases.OrderByDescending(b => PathFinder.GetGroundPath(startingUnit.Pos.X + 4, startingUnit.Pos.Y + 4, b.Location.X, b.Location.Y, 0).Count()));
            stopwatch.Stop();
            Console.WriteLine($"Calculating base expansion order in {stopwatch.ElapsedMilliseconds} ms");
        }

        private List<BaseLocation> GetOrderedBaseLocations(Unit startingUnit, List<BaseLocation> closerBases, Point2D enemystartingLocation)
        {
            var ordered = closerBases.OrderBy(b => PathFinder.GetGroundPath(startingUnit.Pos.X + 4, startingUnit.Pos.Y + 4, b.Location.X, b.Location.Y, 0).Count()).ToList();
            var first = ordered.FirstOrDefault();
            var firstDistance = PathFinder.GetGroundPath(enemystartingLocation.X + 4, enemystartingLocation.Y + 4, first.Location.X, first.Location.Y, 0).Count();
            var natural = ordered.FirstOrDefault(b => PathFinder.GetGroundPath(enemystartingLocation.X + 4, enemystartingLocation.Y + 4, b.Location.X, b.Location.Y, 0).Count() < firstDistance);

            var desiredOrder = new List<BaseLocation> { first };
            if (natural != null)
            {
                desiredOrder.Add(natural);
                desiredOrder.AddRange(ordered.Where(b => !(b.Location.X == first.Location.X && b.Location.Y == first.Location.Y) && !(b.Location.X == natural.Location.X && b.Location.Y == natural.Location.Y)));
            }
            return desiredOrder;
        }

        private void SaveBaseLocationData(string folder, string fileName, BaseData baseData)
        {
            Console.WriteLine($"Saving base expansion order to {fileName}");
            Directory.CreateDirectory(folder);
            var data = new BaseLocationData { SelfBaseLocations = baseData.BaseLocations.Select(b => new Vector2(b.Location.X, b.Location.Y)).ToList(), EnemyBaseLocations = baseData.EnemyBaseLocations.Select(b => new Vector2(b.Location.X, b.Location.Y)).ToList() };
            string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            File.WriteAllText(fileName, json, Encoding.UTF8);
        }

        public Dictionary<string, BaseLocationData> LoadBaseLocationData()
        {
            var dictionary = new Dictionary<string, BaseLocationData>();
            var folder = GetGneratedBaseDataFolder();
            Directory.CreateDirectory(folder);
            foreach (var fileName in Directory.GetFiles(folder))
            {
                using (StreamReader file = File.OpenText(fileName))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                    var data = (BaseLocationData)serializer.Deserialize(file, typeof(BaseLocationData));
                    dictionary[FilePath.GetFileNameWithoutExtension(fileName)] = data;
                }
            }
            return dictionary;
        }

        private string GetGneratedBaseDataFolder()
        {
            return Directory.GetCurrentDirectory() + "/data/base/";
        }

        private static string GetGeneratedBaseDataFileName(string map, string folder, Point2D startLocation)
        {
            return $"{folder}{map}-{startLocation.X}-{startLocation.Y}.json";
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            if (BaseData.SelfBases == null) { return null; }

            foreach (var tag in ActiveUnitData.DeadUnits)
            {
                foreach (var baseLocation in BaseData.BaseLocations)
                {
                    baseLocation.MineralFields.RemoveAll(m => m.Tag == tag);
                }
            }

            UpdateSelfBases();
            UpdateEnemyBases();

            return null;
        }

        void UpdateSelfBases()
        {
            if (BaseData.SelfBases.Count() != UnitCountService.EquivalentTypeCount(UnitTypes.PROTOSS_NEXUS) + UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) + UnitCountService.EquivalentTypeCount(UnitTypes.ZERG_HATCHERY))
            {
                var resourceCenters = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter) && !u.Unit.IsFlying);
                BaseData.SelfBases = BaseData.BaseLocations.Where(b => resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25)).ToList();
                foreach (var selfBase in BaseData.SelfBases)
                {
                    selfBase.ResourceCenter = resourceCenters.FirstOrDefault(r => Vector2.DistanceSquared(r.Position, new Vector2(selfBase.Location.X, selfBase.Location.Y)) < 25).Unit;
                }
            }
            foreach (var selfBase in BaseData.SelfBases)
            {
                if (selfBase.ResourceCenter != null && ActiveUnitData.SelfUnits.TryGetValue(selfBase.ResourceCenter.Tag, out UnitCalculation updatedUnit))
                {
                    if (updatedUnit != null)
                    {
                        selfBase.ResourceCenter = updatedUnit.Unit;
                        if (Vector2.DistanceSquared(new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y), new Vector2(selfBase.Location.X, selfBase.Location.Y)) > 25)
                        {
                            selfBase.ResourceCenter = null;
                            continue;
                        }
                    }
                }
                else
                {
                    var resourceCenter = ActiveUnitData.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter)).FirstOrDefault(r => Vector2.DistanceSquared(r.Position, new Vector2(selfBase.Location.X, selfBase.Location.Y)) < 25);
                    if (resourceCenter != null)
                    {
                        selfBase.ResourceCenter = resourceCenter.Unit;
                    }
                }

                for (var index = 0; index < selfBase.MineralFields.Count; index++)
                {
                    if (selfBase.MineralFields[index].DisplayType == DisplayType.Snapshot)
                    {
                        var visibleMineral = ActiveUnitData.NeutralUnits.FirstOrDefault(m => m.Value.Unit.DisplayType == DisplayType.Visible && m.Value.Unit.Pos.X == selfBase.MineralFields[index].Pos.X && m.Value.Unit.Pos.Y == selfBase.MineralFields[index].Pos.Y).Value;
                        if (visibleMineral != null)
                        {
                            selfBase.MineralFields[index] = visibleMineral.Unit;
                        }
                    }
                }

                for (var index = 0; index < selfBase.MineralFields.Count; index++)
                {
                    if (ActiveUnitData.NeutralUnits.ContainsKey(selfBase.MineralFields[index].Tag))
                    {
                        selfBase.MineralFields[index] = ActiveUnitData.NeutralUnits[selfBase.MineralFields[index].Tag].Unit;
                    }
                    else
                    {
                        selfBase.MineralFields.RemoveAt(index);
                        break;
                    }
                }

                for (var index = 0; index < selfBase.VespeneGeysers.Count; index++)
                {
                    if (selfBase.VespeneGeysers[index].DisplayType == DisplayType.Snapshot)
                    {
                        var visibleGeyser = ActiveUnitData.NeutralUnits.FirstOrDefault(m => m.Value.Unit.DisplayType == DisplayType.Visible && m.Value.Unit.Pos.X == selfBase.VespeneGeysers[index].Pos.X && m.Value.Unit.Pos.Y == selfBase.VespeneGeysers[index].Pos.Y).Value;
                        if (visibleGeyser != null)
                        {
                            selfBase.VespeneGeysers[index] = visibleGeyser.Unit;
                        }
                    }
                }

                if (selfBase.MineralMiningInfo == null)
                {
                    selfBase.MineralMiningInfo = new List<MiningInfo>();
                    foreach (var mineral in selfBase.MineralFields)
                    {
                        selfBase.MineralMiningInfo.Add(new MiningInfo(mineral, selfBase.ResourceCenter.Pos));
                    }
                }
                else
                {
                    for (var index = 0; index < selfBase.MineralMiningInfo.Count; index++)
                    {
                        var updatedMineral = selfBase.MineralFields.FirstOrDefault(m => selfBase.MineralMiningInfo[index].ResourceUnit.Tag == m.Tag);
                        if (updatedMineral != null)
                        {
                            selfBase.MineralMiningInfo[index].ResourceUnit = updatedMineral;
                        }
                        else
                        {
                            selfBase.MineralMiningInfo.RemoveAt(index);
                        }
                    }

                    if (selfBase.MineralMiningInfo.Count != selfBase.MineralFields.Count)
                    {
                        var missing = selfBase.MineralFields.Where(m => !selfBase.MineralMiningInfo.Any(i => i.ResourceUnit.Tag == m.Tag));
                        foreach (var mineral in missing)
                        {
                            selfBase.MineralMiningInfo.Add(new MiningInfo(mineral, selfBase.ResourceCenter.Pos));
                        }
                    }
                }

                var takenGases = ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)).Concat(ActiveUnitData.EnemyUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)));
                if (selfBase.GasMiningInfo == null)
                {
                    selfBase.GasMiningInfo = new List<MiningInfo>();
                    foreach (var geyser in selfBase.VespeneGeysers)
                    {
                        var built = takenGases.FirstOrDefault(t => t.Value.Unit.Pos.X == geyser.Pos.X && t.Value.Unit.Pos.Y == geyser.Pos.Y).Value;
                        if (built != null)
                        {
                            selfBase.GasMiningInfo.Add(new MiningInfo(built.Unit, selfBase.ResourceCenter.Pos));
                        }
                    }
                }
                else
                {
                    for (var index = 0; index < selfBase.GasMiningInfo.Count; index++)
                    {
                        var updatedGas = takenGases.FirstOrDefault(m => selfBase.GasMiningInfo[index].ResourceUnit.Tag == m.Value.Unit.Tag).Value;
                        if (updatedGas != null)
                        {
                            selfBase.GasMiningInfo[index].ResourceUnit = updatedGas.Unit;
                        }
                        else
                        {
                            selfBase.GasMiningInfo.RemoveAt(index);
                        }
                    }

                    foreach (var geyser in selfBase.VespeneGeysers)
                    {
                        var match = selfBase.GasMiningInfo.FirstOrDefault(t => t.ResourceUnit.Pos.X == geyser.Pos.X && t.ResourceUnit.Pos.Y == geyser.Pos.Y);
                        if (match == null)
                        {
                            var built = takenGases.FirstOrDefault(t => t.Value.Unit.Pos.X == geyser.Pos.X && t.Value.Unit.Pos.Y == geyser.Pos.Y).Value;
                            if (built != null)
                            {
                                selfBase.GasMiningInfo.Add(new MiningInfo(built.Unit, selfBase.ResourceCenter.Pos));
                            }
                        }
                    }
                }
            }
            BaseData.SelfBases.RemoveAll(b => b.ResourceCenter == null);
        }

        void UpdateEnemyBases()
        {
            if (BaseData.EnemyBases.Count() != UnitCountService.EquivalentEnemyTypeCount(UnitTypes.PROTOSS_NEXUS) + UnitCountService.EquivalentEnemyTypeCount(UnitTypes.TERRAN_COMMANDCENTER) + UnitCountService.EquivalentEnemyTypeCount(UnitTypes.ZERG_HATCHERY))
            {
                var resourceCenters = ActiveUnitData.EnemyUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
                BaseData.EnemyBases = BaseData.BaseLocations.Where(b => resourceCenters.Any(r => Vector2.DistanceSquared(r.Position, new Vector2(b.Location.X, b.Location.Y)) < 25)).ToList();
                foreach (var enemyBase in BaseData.EnemyBases)
                {
                    enemyBase.ResourceCenter = resourceCenters.FirstOrDefault(r => Vector2.DistanceSquared(r.Position, new Vector2(enemyBase.Location.X, enemyBase.Location.Y)) < 25).Unit;
                }
            }

            var takenGases = ActiveUnitData.EnemyUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)).Concat(ActiveUnitData.SelfUnits.Where(u => SharkyUnitData.GasGeyserRefineryTypes.Contains((UnitTypes)u.Value.Unit.UnitType)));
            foreach (var enemyBase in BaseData.EnemyBases)
            {
                for (var index = 0; index < enemyBase.VespeneGeysers.Count; index++)
                {
                    if (enemyBase.VespeneGeysers[index].DisplayType == DisplayType.Snapshot)
                    {
                        var visibleGeyser = ActiveUnitData.NeutralUnits.FirstOrDefault(m => m.Value.Unit.DisplayType == DisplayType.Visible && m.Value.Unit.Pos.X == enemyBase.VespeneGeysers[index].Pos.X && m.Value.Unit.Pos.Y == enemyBase.VespeneGeysers[index].Pos.Y).Value;
                        if (visibleGeyser != null)
                        {
                            enemyBase.VespeneGeysers[index] = visibleGeyser.Unit;
                        }
                    }
                    var takenGas = takenGases.FirstOrDefault(g => g.Value.Position.X == enemyBase.VespeneGeysers[index].Pos.X && g.Value.Position.Y == enemyBase.VespeneGeysers[index].Pos.Y).Value;
                    if (takenGas != null)
                    {
                        enemyBase.VespeneGeysers[index] = takenGas.Unit;
                    }
                }
                if (enemyBase.ResourceCenter != null)
                {
                    if (ActiveUnitData.EnemyUnits.ContainsKey(enemyBase.ResourceCenter.Tag))
                    {
                        enemyBase.ResourceCenter = ActiveUnitData.EnemyUnits[enemyBase.ResourceCenter.Tag].Unit;
                    }
                    else
                    {
                        enemyBase.ResourceCenter = null;
                    }
                }
                else
                {
                    var enemyBases = ActiveUnitData.EnemyUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
                    if (enemyBases.Any() && enemyBase?.Location != null)
                    {
                        var eb = enemyBases.FirstOrDefault(r => Vector2.DistanceSquared(r.Position, new Vector2(enemyBase.Location.X, enemyBase.Location.Y)) < 25);
                        if (eb?.Unit != null)
                        {
                            enemyBase.ResourceCenter = eb.Unit;
                            continue;
                        }
                    }

                    enemyBase.ResourceCenter = null;
                }
            }
        }

        void SetMineralLineLocation(BaseLocation baseLocation)
        {
            var vectors = baseLocation.MineralFields.Select(m => new Vector2(m.Pos.X, m.Pos.Y));
            baseLocation.MineralLineLocation = new Point2D { X = vectors.Average(v => v.X), Y = vectors.Average(v => v.Y) };

            var angle = Math.Atan2(baseLocation.Location.Y - baseLocation.MineralLineLocation.Y, baseLocation.MineralLineLocation.X - baseLocation.Location.X);
            baseLocation.MineralLineBuildingLocation = new Point2D { X = baseLocation.MineralLineLocation.X + (float)(3 * Math.Cos(angle)), Y = baseLocation.MineralLineLocation.Y - (float)(3 * Math.Sin(angle)) };
            baseLocation.BehindMineralLineLocation = new Point2D { X = baseLocation.MineralLineLocation.X + (float)(3 * Math.Cos(angle)), Y = baseLocation.MineralLineLocation.Y - (float)(3 * Math.Sin(angle)) };
            var height = MapDataService.MapHeight(baseLocation.Location);
            if (MapDataService.MapHeight(baseLocation.MineralLineBuildingLocation) != height)
            {
                baseLocation.MineralLineBuildingLocation = new Point2D { X = baseLocation.MineralLineLocation.X + (float)(2 * Math.Cos(angle)), Y = baseLocation.MineralLineLocation.Y - (float)(2 * Math.Sin(angle)) };
                baseLocation.BehindMineralLineLocation = new Point2D { X = baseLocation.MineralLineLocation.X + (float)(2 * Math.Cos(angle)), Y = baseLocation.MineralLineLocation.Y - (float)(2 * Math.Sin(angle)) };
            }
            baseLocation.MiddleMineralLocation = new Point2D { X = baseLocation.MineralLineLocation.X + (float)(1 * Math.Cos(angle)), Y = baseLocation.MineralLineLocation.Y - (float)(1 * Math.Sin(angle)) };
            if (MapDataService.MapHeight(baseLocation.MineralLineBuildingLocation) != height)
            {
                baseLocation.MineralLineBuildingLocation = baseLocation.MiddleMineralLocation;
                baseLocation.BehindMineralLineLocation = baseLocation.MiddleMineralLocation;
            }
        }

        void DetermineFinalLocation(BaseLocation baseLocation, List<Unit> gasses)
        {
            for (int i = 0; i < gasses.Count; i++)
            {
                foreach (var mineralField in baseLocation.MineralFields)
                {
                    if (Vector2.DistanceSquared(new Vector2(mineralField.Pos.X, mineralField.Pos.Y), new Vector2(gasses[i].Pos.X, gasses[i].Pos.Y)) <= 64)
                    {
                        baseLocation.VespeneGeysers.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                        break;
                    }
                }
            }

            if (baseLocation.VespeneGeysers.Count == 1)
            {
                for (int i = 0; i < gasses.Count; i++)
                {
                    if (Vector2.DistanceSquared(new Vector2(baseLocation.VespeneGeysers[0].Pos.X, baseLocation.VespeneGeysers[0].Pos.Y), new Vector2(gasses[i].Pos.X, gasses[i].Pos.Y)) <= 64)
                    {
                        baseLocation.VespeneGeysers.Add(gasses[i]);
                        gasses[i] = gasses[gasses.Count - 1];
                        gasses.RemoveAt(gasses.Count - 1);
                        i--;
                        break;
                    }
                }
            }

            float x = 0;
            float y = 0;
            foreach (var field in baseLocation.MineralFields)
            {
                x += (int)field.Pos.X;
                y += (int)field.Pos.Y;
            }
            x /= baseLocation.MineralFields.Count;
            y /= baseLocation.MineralFields.Count;

            // Round to nearest half position. bases are 5x5 and therefore always centered in the middle of a tile.
            x = (int)(x) + 0.5f;
            y = (int)(y) + 0.5f;

            // Temporary position, we still need a proper position.
            baseLocation.Location = new Point2D { X = x, Y = y };

            Unit closest = null;
            var closestDistance = 10000f;
            foreach (var mineralField in baseLocation.MineralFields)
            {
                var distance = Math.Abs(mineralField.Pos.X - baseLocation.Location.X) + Math.Abs(mineralField.Pos.Y - baseLocation.Location.Y);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = mineralField;
                }
            }

            // Move the estimated base position slightly away from the closest mineral.
            // This ensures that the base location will not end up on the far side of the minerals.
            if (closest.Pos.X < baseLocation.Location.X)
            {
                baseLocation.Location.X += 2;
            }
            else if (closest.Pos.X > baseLocation.Location.X)
            {
                baseLocation.Location.X -= 2;
            }
            if (closest.Pos.Y < baseLocation.Location.Y)
            {
                baseLocation.Location.Y += 2;
            }
            else if (closest.Pos.Y > baseLocation.Location.Y)
            {
                baseLocation.Location.Y -= 2;
            }

            var closestLocation = 1000000f;
            var approximateLocation = baseLocation.Location;
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j == 0 || j < i; j++)
                {
                    float maxDist;
                    Point2D newPos;
                    newPos = new Point2D { X = approximateLocation.X + i - j, Y = approximateLocation.Y + j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }

                    newPos = new Point2D { X = approximateLocation.X + i - j, Y = approximateLocation.Y - j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }

                    newPos = new Point2D { X = approximateLocation.X - i + j, Y = approximateLocation.Y + j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }

                    newPos = new Point2D { X = approximateLocation.X - i + j, Y = approximateLocation.Y - j };
                    maxDist = checkPosition(newPos, baseLocation);
                    if (maxDist < closestLocation)
                    {
                        baseLocation.Location = newPos;
                        closestLocation = maxDist;
                    }
                }
            }
        }

        float checkPosition(Point2D position, BaseLocation location)
        {
            foreach (var mineralField in location.MineralFields)
            {
                if (Math.Abs(mineralField.Pos.X - position.X) + Math.Abs(mineralField.Pos.Y - position.Y) <= 10 && Math.Abs(mineralField.Pos.X - position.X) <= 5.5 && Math.Abs(mineralField.Pos.Y - position.Y) <= 5.5)
                {
                    return 100000000;
                }
            }
            foreach (var gas in location.VespeneGeysers)
            {
                if (Math.Abs(gas.Pos.X - position.X) + Math.Abs(gas.Pos.Y - position.Y) <= 11 && Math.Abs(gas.Pos.X - position.X) <= 6.1 && Math.Abs(gas.Pos.Y - position.Y) <= 6.1)
                {
                    return 100000000;
                }
                
                if (Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), new Vector2(position.X, position.Y)) >= 121)
                {
                    return 100000000;
                }
            }

            // Check if a resource center can actually be built here.
            for (float x = -2.5f; x < 2.5f + 0.1f; x++)
            {
                for (float y = -2.5f; y < 2.5f + 0.1f; y++)
                {
                    if (!GetTilePlacable((int)Math.Round(position.X + x), (int)Math.Round(position.Y + y)))
                    {
                        return 100000000;
                    }
                }
            }

            float maxDist = 0;
            foreach (var mineralField in location.MineralFields)
            {
                maxDist += Vector2.DistanceSquared(new Vector2(mineralField.Pos.X, mineralField.Pos.Y), new Vector2(position.X, position.Y));
            }

            foreach (var gas in location.VespeneGeysers)
            {
                maxDist += Vector2.DistanceSquared(new Vector2(gas.Pos.X, gas.Pos.Y), new Vector2(position.X, position.Y));
            }
            return maxDist;
        }

        void SetupMiningInfo()
        {
            foreach (var selfBase in BaseData.SelfBases)
            {
                selfBase.MineralMiningInfo = new List<MiningInfo>();
                foreach (var mineral in selfBase.MineralFields)
                {
                    selfBase.MineralMiningInfo.Add(new MiningInfo(mineral, selfBase.ResourceCenter.Pos));
                }
            }
        }

        bool GetTilePlacable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= PlacementGrid.Size.X || y >= PlacementGrid.Size.Y)
            {
                return false;
            }
            int pixelID = x + y * PlacementGrid.Size.X;
            int byteLocation = pixelID / 8;
            int bitLocation = pixelID % 8;
            var result = ((PlacementGrid.Data[byteLocation] & 1 << (7 - bitLocation)) == 0) ? 0 : 1;
            return result != 0;
        }
    }
}
