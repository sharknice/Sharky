using Sharky.MicroTasks.Harass;
using System.ComponentModel.Design;

namespace Sharky.MicroTasks
{
    public class ReaperWorkerHarassTask : MicroTask
    {
        BaseData BaseData;
        TargetingData TargetingData;
        TargetingService TargetingService;
        IIndividualMicroController MicroController;
        SharkyUnitData SharkyUnitData;
        FrameToTimeConverter FrameToTimeConverter;
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        ArmySplitter DefenseArmySplitter;

        public Dictionary<ulong, UnitCalculation> Kills { get; private set; }
        public bool HuntEnemyReapers { get; set; } = false;

        int DesiredCount { get; set; }
        List<HarassInfo> HarassInfos { get; set; }

        public ReaperWorkerHarassTask(DefaultSharkyBot defaultSharkyBot, IIndividualMicroController microController, int desiredCount = 2, bool enabled = false, float priority = -1f)
        {
            BaseData = defaultSharkyBot.BaseData;
            TargetingData = defaultSharkyBot.TargetingData;
            TargetingService = defaultSharkyBot.TargetingService;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            MacroData = defaultSharkyBot.MacroData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            MicroController = microController;
            DesiredCount = desiredCount;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            Kills = new Dictionary<ulong, UnitCalculation>();

            DefenseArmySplitter = new ArmySplitter(defaultSharkyBot);
        }

        public override void ClaimUnits(Dictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() < DesiredCount)
            {
                foreach (var commander in commanders)
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER)
                    {
                        commander.Value.Claimed = true;
                        UnitCommanders.Add(commander.Value);
                    }
                    if (UnitCommanders.Count() == DesiredCount)
                    {
                        return;
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            AssignHarassers();

            IEnumerable<UnitCommander> defenders = new List<UnitCommander>();

            var attackingEnemies = ActiveUnitData.EnemyUnits.Values.Where(e => e.FrameLastSeen > frame - 100 && !e.Unit.IsFlying && e.Range < 3 &&
                (e.NearbyEnemies.Any(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter) || u.UnitClassifications.Contains(UnitClassification.ProductionStructure) || u.UnitClassifications.Contains(UnitClassification.DefensiveStructure))) && 
                (e.NearbyEnemies.Count(b => b.Attributes.Contains(SC2APIProtocol.Attribute.Structure)) >= e.NearbyAllies.Count(b => b.Attributes.Contains(SC2APIProtocol.Attribute.Structure)))).Where(e => e.Unit.UnitType != (uint)UnitTypes.TERRAN_KD8CHARGE);

            var enemyReapers = ActiveUnitData.EnemyUnits.Values.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER);
            if (HuntEnemyReapers && enemyReapers.Any())
            {
                var attackingEnemyVector = TargetingService.GetArmyPoint(enemyReapers).ToVector2();

                var reaperVReapers = new List<UnitCommander>();
                foreach (var commander in UnitCommanders)
                {
                    var enemyReaper = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (enemyReaper != null)
                    {
                        if (commander.UnitCalculation.SimulatedHitpoints >= enemyReaper.SimulatedHitpoints)
                        {
                            var attack = MicroController.Attack(commander, enemyReaper.Position.ToPoint2D(), TargetingData.ForwardDefensePoint, null, frame);
                            if (attack != null)
                            {
                                commands.AddRange(attack);
                            }
                            reaperVReapers.Add(commander);
                        }
                    }
                }

                defenders = UnitCommanders.Where(c => !reaperVReapers.Contains(c));
                foreach (var commander in defenders)
                {
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS && e.Unit.IsActive && !e.NearbyEnemies.Any(sa => sa.Unit.UnitType == (uint)UnitTypes.TERRAN_MARAUDER))))
                    {
                        var harassAction = MicroController.HarassWorkers(commander, attackingEnemyVector.ToPoint2D(), TargetingData.ForwardDefensePoint, frame);
                        if (harassAction != null)
                        {
                            commands.AddRange(harassAction);
                        }
                        continue;
                    }

                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.FrameLastSeen == frame))
                    {
                        var action = MicroController.NavigateToPoint(commander, attackingEnemyVector.ToPoint2D(), TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }
                    else
                    {
                        var action = commander.Order(frame, Abilities.MOVE, attackingEnemyVector.ToPoint2D());
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                    }

                }
                return commands;
            }
            else if (attackingEnemies.Any())
            {
                var attackingEnemyVector = TargetingService.GetArmyPoint(attackingEnemies).ToVector2();
                defenders = UnitCommanders.Where(c => Vector2.DistanceSquared(c.UnitCalculation.Position, attackingEnemyVector) < Vector2.DistanceSquared(c.UnitCalculation.Position, TargetingData.EnemyMainBasePoint.ToVector2()));
                if (defenders.Any())
                {
                    var actions = DefenseArmySplitter.SplitArmy(frame, attackingEnemies, TargetingData.AttackPoint, defenders, true);
                    commands.AddRange(actions);
                }
            }

            foreach (var harassInfo in HarassInfos)
            {
                foreach (var commander in harassInfo.Harassers.Where(c => !defenders.Contains(c)))
                {
                    var enemyReaper = commander.UnitCalculation.NearbyEnemies.Where(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_REAPER).OrderBy(e => Vector2.DistanceSquared(e.Position, commander.UnitCalculation.Position)).FirstOrDefault();
                    if (enemyReaper != null)
                    {
                        if (commander.UnitCalculation.SimulatedHitpoints >= enemyReaper.SimulatedHitpoints)
                        {
                            var attack = MicroController.Attack(commander, enemyReaper.Position.ToPoint2D(), TargetingData.ForwardDefensePoint, null, frame);
                            if (attack != null)
                            {
                                commands.AddRange(attack);
                            }
                            continue;
                        }
                    }

                    if (commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax && commander.UnitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_MARAUDER))
                    {
                        var action = MicroController.Retreat(commander, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                    if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker)) && !commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) || e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || (e.Unit.UnitType == (uint)UnitTypes.TERRAN_BARRACKS && e.Unit.IsActive && !e.NearbyEnemies.Any(sa => sa.Unit.UnitType == (uint)UnitTypes.TERRAN_MARAUDER))))
                    {
                        var action = MicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }
                        continue;
                    }
                    else if (Vector2.DistanceSquared(commander.UnitCalculation.Position, new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y)) < 100)
                    {
                        var action = MicroController.HarassWorkers(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (!commander.UnitCalculation.NearbyEnemies.Any(e => Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            harassInfo.LastClearedFrame = frame;
                            harassInfo.Harassers.Remove(commander);
                            return commands;
                        }
                        else if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.DamageGround && Vector2.DistanceSquared(new Vector2(harassInfo.BaseLocation.MineralLineLocation.X, harassInfo.BaseLocation.MineralLineLocation.Y), e.Position) < 100))
                        {
                            if (commander.UnitCalculation.TargetPriorityCalculation.GroundWinnability < 1 && commander.UnitCalculation.Unit.Health < commander.UnitCalculation.Unit.HealthMax)
                            {
                                harassInfo.LastDefendedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                        continue;
                    }
                    else
                    {
                        var action = MicroController.NavigateToPoint(commander, harassInfo.BaseLocation.MineralLineLocation, TargetingData.ForwardDefensePoint, null, frame);
                        if (action != null)
                        {
                            commands.AddRange(action);
                        }

                        if (commander.RetreatPath.Count() == 0)
                        {
                            if (commander.UnitCalculation.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit) && e.DamageGround && Vector2.DistanceSquared(commander.UnitCalculation.Position, e.Position) < 120))
                            {
                                harassInfo.LastPathFailedFrame = frame;
                                harassInfo.Harassers.Remove(commander);
                                return commands;
                            }
                        }
                        continue;
                    }
                }
            }

            return commands;
        }

        void AssignHarassers()
        {
            if (HarassInfos == null)
            {
                HarassInfos = new List<HarassInfo>();
                foreach (var baseLocation in BaseData.BaseLocations.Where(b => b.ResourceCenter == null || b.ResourceCenter.Alliance != SC2APIProtocol.Alliance.Self).Reverse())
                {
                    HarassInfos.Add(new HarassInfo { BaseLocation = baseLocation, Harassers = new List<UnitCommander>(), LastClearedFrame = -1, LastDefendedFrame = -1, LastPathFailedFrame = -1 });
                }
            }
            else
            {
                foreach (var baseLocation in BaseData.SelfBases)
                {
                    HarassInfos.RemoveAll(h => h.BaseLocation.Location.X == baseLocation.Location.X && h.BaseLocation.Location.Y == baseLocation.Location.Y);
                }
                foreach (var harassInfo in HarassInfos)
                {
                    harassInfo.Harassers.RemoveAll(h => !UnitCommanders.Any(u => u.UnitCalculation.Unit.Tag == h.UnitCalculation.Unit.Tag));
                }
            }

            if (HarassInfos.Any())
            {
                var unasignedCommanders = UnitCommanders.Where(u => !HarassInfos.Any(info => info.Harassers.Any(h => h.UnitCalculation.Unit.Tag == u.UnitCalculation.Unit.Tag))).ToList();
                while (unasignedCommanders.Any())
                {
                    foreach (var info in HarassInfos.OrderBy(h => h.Harassers.Count()).ThenBy(h => HighestFrame(h)))
                    {
                        var commander = unasignedCommanders.First();
                        info.Harassers.Add(commander);
                        unasignedCommanders.Remove(commander);
                        if (unasignedCommanders.Count() == 0)
                        {
                            return;
                        }
                    }
                }
            }
        }

        int HighestFrame(HarassInfo h)
        {
            var highest = h.LastClearedFrame;
            if (h.LastDefendedFrame > highest)
            {
                highest = h.LastDefendedFrame;
            }
            if (h.LastPathFailedFrame > highest)
            {
                highest = h.LastPathFailedFrame;
            }
            return highest;
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            var deaths = 0;
            var kills = 0;
            foreach (var tag in deadUnits)
            {
                if (UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag) > 0)
                {
                    deaths++;
                }
                else
                {
                    var kill = UnitCommanders.SelectMany(c => c.UnitCalculation.PreviousUnitCalculation.NearbyEnemies.Where(e => Vector2.DistanceSquared(c.UnitCalculation.Position, e.Position) < 50)).FirstOrDefault(e => e.Unit.Tag == tag);
                    if (kill != null && !SharkyUnitData.UndeadTypes.Contains((UnitTypes)kill.Unit.UnitType))
                    {
                        kills++;
                        Kills[tag] = kill;
                    }
                }
            }

            if (deaths > 0)
            {
                // TODO: chat for death, oh no I needed that for late game
                Deaths += deaths;
            }
            if (kills > 0 || deaths > 0)
            {
                // TODO: chat for kills, I was just scouting but ok, etc.
                ReportResults();
            }
        }

        private void ReportResults()
        {
            Console.WriteLine($"{FrameToTimeConverter.GetTime(MacroData.Frame)} HellionHarass Report: Deaths:{Deaths}, Kills:{Kills.Count()}");
            foreach (var killGroup in Kills.Values.GroupBy(k => k.Unit.UnitType))
            {
                Console.WriteLine($"{(UnitTypes)killGroup.Key}: {killGroup.Count()}");
            }
        }

        public override void PrintReport(int frame)
        {
            ReportResults();
            base.PrintReport(frame);
        }

        public override void Disable()
        {
            ReportResults();
            base.Disable();
        }
    }
}
