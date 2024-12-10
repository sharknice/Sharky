﻿namespace Sharky.Managers
{
    public class DebugManager : SharkyManager
    {
        GameConnection GameConnection;
        SharkyOptions SharkyOptions;
        DebugService DebugService;
        MapData MapData;
        TargetingData TargetingData;
        ActiveUnitData ActiveUnitData;
        EnemyData EnemyData;
        SharkyUnitData SharkyUnitData;
        ChatService ChatService;
        TagService TagService;
        BaseData BaseData;

        bool SlowMode = false;
        int SlowTime = 0;

        public DebugManager(GameConnection gameConnection, SharkyOptions sharkyOptions, DebugService debugService, MapData mapData, TargetingData targetingData, ActiveUnitData activeUnitData, EnemyData enemyData, ChatService chatService, TagService tagService, SharkyUnitData sharkyUnitData, BaseData baseData)
        {
            GameConnection = gameConnection;
            SharkyOptions = sharkyOptions;
            DebugService = debugService;
            MapData = mapData;
            TargetingData = targetingData;
            ActiveUnitData = activeUnitData;
            EnemyData = enemyData;
            ChatService = chatService;
            TagService = tagService;
            SharkyUnitData = sharkyUnitData;
            BaseData = baseData;
        }

        public override bool NeverSkip { get => true; }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            if (SharkyOptions.TagOptions.UnitTagsEnabled)
            {
                TagService.TagUnits(ActiveUnitData);
            }

            if (SharkyOptions.TagOptions.UpgradeTagsEnabled)
            {
                TagService.TagUpgrades(SharkyUnitData);
            }

            if (SharkyOptions.Debug)
            {
                if (SharkyOptions.DebugMicroTaskUnits)
                {
                    DebugService.DrawUnitInfo();
                }

                ReadCommand(observation.Chat, observation.Observation.RawData.Player.Camera);
                try
                {
                    GameConnection.SendRequest(DebugService.DrawRequest).Wait();
                    GameConnection.SendRequest(DebugService.SpawnRequest).Wait();
                    if (DebugService.Surrender)
                    {
                        GameConnection.SendRequest(new Request { LeaveGame = new RequestLeaveGame() }).Wait();
                    }
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine($"{e.Message}");
                }

                if (SlowMode)
                {
                    Thread.Sleep(SlowTime);
                }
            }

            DebugService.ResetDrawRequest();
            DebugService.ResetSpawnRequest();

            return new List<SC2Action>();
        }

        private void ReadCommand(RepeatedField<ChatReceived> chatsReceived, Point camera)
        {
            foreach (var chatReceived in chatsReceived)
            {
                var match = Regex.Match(chatReceived.Message.ToLower(), "debug units");
                if (match.Success)
                {
                    DebugService.DebugUnits();
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "debug creep");
                if (match.Success)
                {
                    DebugService.DebugCreep();
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy wall");
                if (match.Success)
                {
                    foreach (var wallData in MapData.WallData.Where(w => w.BasePosition.X == TargetingData.EnemyMainBasePoint.X && w.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y && w.Pylons != null))
                    {
                        if (EnemyData.EnemyRace == Race.Protoss)
                        {
                            foreach (var spot in wallData.Pylons)
                            {
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_PYLON, spot, 2, 1);
                            }
                            foreach (var spot in wallData.WallSegments)
                            {
                                if (spot.Size == 3)
                                {
                                    DebugService.SpawnUnits(UnitTypes.PROTOSS_GATEWAY, spot.Position, 2, 1);
                                }
                                if (spot.Size == 2)
                                {
                                    DebugService.SpawnUnits(UnitTypes.PROTOSS_SHIELDBATTERY, spot.Position, 2, 1);
                                }
                            }
                            if (wallData.Block != null)
                            {
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_SHIELDBATTERY, wallData.Block, 2, 1);
                            }
                        }
                        else
                        {
                            foreach (var spot in wallData.Depots)
                            {
                                DebugService.SpawnUnits(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED, spot, 2, 1);
                            }
                            foreach (var spot in wallData.Production)
                            {
                                DebugService.SpawnUnits(UnitTypes.TERRAN_BARRACKS, spot, 2, 1);
                            }
                        }
                    }

                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy natural wall");
                if (match.Success)
                {
                    foreach (var wallData in MapData.WallData.Where(w => w.BasePosition.X == BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().Location.X && w.BasePosition.Y == BaseData.EnemyBaseLocations.Skip(1).FirstOrDefault().Location.Y && w.Pylons != null))
                    {
                        if (EnemyData.EnemyRace == Race.Protoss)
                        {
                            foreach (var spot in wallData.Pylons)
                            {
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_PYLON, spot, 2, 1);
                            }
                            foreach (var spot in wallData.WallSegments)
                            {
                                if (spot.Size == 3)
                                {
                                    DebugService.SpawnUnits(UnitTypes.PROTOSS_GATEWAY, spot.Position, 2, 1);
                                }
                                if (spot.Size == 2)
                                {
                                    DebugService.SpawnUnits(UnitTypes.PROTOSS_SHIELDBATTERY, spot.Position, 2, 1);
                                }
                            }
                            if (wallData.Block != null)
                            {
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_SHIELDBATTERY, wallData.Block, 2, 1);
                            }
                        }
                        else if (wallData.FullDepotWall != null)
                        {
                            foreach (var spot in wallData.FullDepotWall)
                            {
                                DebugService.SpawnUnits(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED, spot, 2, 1);
                            }
                            foreach (var spot in wallData.Production)
                            {
                                DebugService.SpawnUnits(UnitTypes.TERRAN_BARRACKS, spot, 2, 1);
                            }
                        }
                    }

                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn late game");
                if (match.Success)
                {
                    foreach (var baseLocation in BaseData.BaseLocations.Take(6))
                    {
                        var spot = baseLocation.Location;
                        if (!ActiveUnitData.SelfUnits.Values.Any(u => u.Unit.Pos.X == spot.X && u.Unit.Pos.Y == spot.Y))
                        {
                            if (EnemyData.SelfRace == Race.Protoss)
                            {
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_NEXUS, spot, 1, 1);
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_PROBE, baseLocation.MineralLineLocation, 1, 15);                            
                            }
                            else if (EnemyData.SelfRace == Race.Terran)
                            {
                                DebugService.SpawnUnits(UnitTypes.TERRAN_ORBITALCOMMAND, spot, 1, 1);
                                DebugService.SpawnUnits(UnitTypes.TERRAN_SCV, baseLocation.MineralLineLocation, 1, 15);
                            }
                            else if (EnemyData.SelfRace == Race.Zerg)
                            {
                                DebugService.SpawnUnits(UnitTypes.ZERG_HIVE, spot, 1, 1);
                                DebugService.SpawnUnits(UnitTypes.ZERG_DRONE, baseLocation.MineralLineLocation, 1, 15);
                            }
                        }
                    }

                    foreach (var baseLocation in BaseData.EnemyBaseLocations.Take(6))
                    {
                        var spot = baseLocation.Location;
                        if (!ActiveUnitData.EnemyUnits.Values.Any(u => u.Unit.Pos.X == spot.X && u.Unit.Pos.Y == spot.Y))
                        {
                            if (EnemyData.EnemyRace == Race.Protoss)
                            {
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_NEXUS, spot, 2, 1);
                                DebugService.SpawnUnits(UnitTypes.PROTOSS_PROBE, baseLocation.MineralLineLocation, 2, 15);
                            }
                            else if (EnemyData.EnemyRace == Race.Terran)
                            {
                                DebugService.SpawnUnits(UnitTypes.TERRAN_ORBITALCOMMAND, spot, 2, 1);
                                DebugService.SpawnUnits(UnitTypes.TERRAN_SCV, baseLocation.MineralLineLocation, 2, 15);
                            }
                            else if (EnemyData.EnemyRace == Race.Zerg)
                            {
                                DebugService.SpawnUnits(UnitTypes.ZERG_HIVE, spot, 2, 1);
                                DebugService.SpawnUnits(UnitTypes.ZERG_DRONE, baseLocation.MineralLineLocation, 2, 15);
                            }
                        }
                    }

                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy depot racks");
                if (match.Success)
                {
                    foreach (var wallData in MapData.WallData.Where(w => w.BasePosition.X == TargetingData.EnemyMainBasePoint.X && w.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y && w.Depots != null))
                    {
                        var spot = wallData.Depots.FirstOrDefault();
                        DebugService.SpawnUnits(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED, spot, 2, 1);

                        foreach (var racksSpot in wallData.Production)
                        {
                            DebugService.SpawnUnits(UnitTypes.TERRAN_BARRACKS, racksSpot, 2, 1);
                        }
                    }

                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy depots");
                if (match.Success)
                {
                    foreach (var wallData in MapData.WallData.Where(w => w.BasePosition.X == TargetingData.EnemyMainBasePoint.X && w.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y && w.FullDepotWall != null))
                    {
                        foreach (var spot in wallData.FullDepotWall)
                        {
                            DebugService.SpawnUnits(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED, spot, 2, 1);
                        }
                    }

                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy depot");
                if (match.Success)
                {
                    foreach (var wallData in MapData.WallData.Where(w => w.BasePosition.X == TargetingData.EnemyMainBasePoint.X && w.BasePosition.Y == TargetingData.EnemyMainBasePoint.Y && w.Depots != null && w.Depots.Any()))
                    {
                        var spot = wallData.Depots.FirstOrDefault();
                        DebugService.SpawnUnits(UnitTypes.TERRAN_SUPPLYDEPOTLOWERED, spot, 2, 1);
                    }

                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"spawn (\d+) both (.*)");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[2].Value, true);
                    DebugService.SpawnUnits(unitType, new Point2D { X = camera.X, Y = camera.Y }, (int)chatReceived.PlayerId, int.Parse(quantity));
                    var enemyId = 1;
                    if (chatReceived.PlayerId == 1)
                    {
                        enemyId = 2;
                    }
                    DebugService.SpawnUnits(unitType, new Point2D { X = camera.X, Y = camera.Y }, enemyId, int.Parse(quantity));
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"spawn (\d+) friendly (.*)");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[2].Value, true);
                    DebugService.SpawnUnits(unitType, new Point2D { X = camera.X, Y = camera.Y }, (int)chatReceived.PlayerId, int.Parse(quantity));
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"spawn (\d+) enemy (.*)");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[2].Value, true);
                    var enemyId = 1;
                    if (chatReceived.PlayerId == 1)
                    {
                        enemyId = 2;
                    }
                    DebugService.SpawnUnits(unitType, new Point2D { X = camera.X, Y = camera.Y }, enemyId, int.Parse(quantity));
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn friendly (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    DebugService.SpawnUnit(unitType, new Point2D { X = camera.X, Y = camera.Y }, (int)chatReceived.PlayerId);
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "spawn enemy (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    var enemyId = 1;
                    if (chatReceived.PlayerId == 1)
                    {
                        enemyId = 2;
                    }
                    DebugService.SpawnUnit(unitType, new Point2D { X = camera.X, Y = camera.Y }, enemyId);
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"camera (\d+\.?\d*) (\d+\.?\d*)");
                if (match.Success)
                {
                    var x = float.Parse(match.Groups[1].Value);
                    var y = float.Parse(match.Groups[2].Value);
                    DebugService.SetCamera(new Point { X = x, Y = y, Z = camera.Z });
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"show camera");
                if (match.Success)
                {
                    ChatService.SendDebugChatMessage($"X: {camera.X}, Y: {camera.Y}");
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "camera");
                if (match.Success)
                {
                    SharkyOptions.ControlCamera = !SharkyOptions.ControlCamera;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "kill friendly (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    DebugService.KillFriendlyUnits(unitType);
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "kill (\\d+) friendly (.*)");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[2].Value, true);
                    DebugService.KillFriendlyUnits(unitType, int.Parse(quantity));
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "kill enemy (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    DebugService.KillEnemyUnits(unitType);
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "energize friendly (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    foreach (var unit in ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.UnitType == (uint)unitType))
                    {
                        DebugService.SetEnergy(unit.Key, unit.Value.Unit.EnergyMax);
                    }
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), "energize enemy (.*)");
                if (match.Success)
                {
                    var unitType = (UnitTypes)System.Enum.Parse(typeof(UnitTypes), match.Groups[1].Value, true);
                    foreach (var unit in ActiveUnitData.EnemyUnits.Where(u => u.Value.Unit.UnitType == (uint)unitType))
                    {
                        DebugService.SetEnergy(unit.Key, unit.Value.Unit.EnergyMax);
                    }
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"slow (\d+)");
                if (match.Success)
                {
                    SlowMode = true;
                    SlowTime = int.Parse(match.Groups[1].Value);
                    return;
                }

                match = Regex.Match(chatReceived.Message.ToLower(), @"slow off");
                if (match.Success)
                {
                    SlowMode = false;
                    return;
                }
            }
        }
    }
}
