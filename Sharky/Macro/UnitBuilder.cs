namespace Sharky.Macro
{
    public class UnitBuilder
    {
        MacroData MacroData;
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        TargetingData TargetingData;
        AttackData AttackData;
        BuildOptions BuildOptions;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;

        UnitCountService UnitCountService;
        SharkyOptions SharkyOptions;
        CameraManager CameraManager;

        IBuildingPlacement WarpInPlacement;
        IProducerSelector ProducerSelector;
        IProducerSelector ZergProducerSelector;

        public UnitBuilder(DefaultSharkyBot defaultSharkyBot, IBuildingPlacement warpInPlacement, IProducerSelector defaultProducerSelector, IProducerSelector zergProducerSelector)
        {
            MacroData = defaultSharkyBot.MacroData;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            TargetingData = defaultSharkyBot.TargetingData;
            AttackData = defaultSharkyBot.AttackData;
            UnitCountService = defaultSharkyBot.UnitCountService;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            BuildOptions = defaultSharkyBot.BuildOptions;
            EnemyData = defaultSharkyBot.EnemyData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            CameraManager = defaultSharkyBot.CameraManager;

            WarpInPlacement = warpInPlacement;
            ProducerSelector = defaultProducerSelector;
            ZergProducerSelector = zergProducerSelector;
        }

        public List<SC2Action> ProduceUnits()
        {
            if (EnemyData.SelfRace == Race.Zerg)
                ProducerSelector = ZergProducerSelector;

            var commands = new List<SC2Action>();
            foreach (var buildUnit in MacroData.BuildUnits)
            {
                if (!buildUnit.Value)
                    continue;

                var unitType = buildUnit.Key;

                if (unitType != UnitTypes.PROTOSS_ARCHON)
                {
                    var unitData = SharkyUnitData.TrainingData[buildUnit.Key];
                    if ((unitData.Food == 0 || unitData.Food <= MacroData.FoodLeft) && unitData.Minerals <= MacroData.Minerals && unitData.Gas <= MacroData.VespeneGas)
                    {
                        var producers = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && (!c.Value.UnitCalculation.Unit.IsActive || !c.Value.UnitCalculation.Attributes.Contains(SC2Attribute.Structure)) && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.WarpInOffCooldown(MacroData.Frame, SharkyOptions.FramesPerSecond, SharkyUnitData)).Select(x => x.Value);

                        if (unitData.ProducingUnits.Contains(UnitTypes.TERRAN_BARRACKS) || unitData.ProducingUnits.Contains(UnitTypes.TERRAN_FACTORY) || unitData.ProducingUnits.Contains(UnitTypes.TERRAN_STARPORT))
                        {
                            if (unitData.RequiresTechLab)
                            {
                                producers = producers.Where(b => b.UnitCalculation.Unit.HasAddOnTag && SharkyUnitData.TechLabTypes.Contains((UnitTypes)ActiveUnitData.SelfUnits[b.UnitCalculation.Unit.AddOnTag].Unit.UnitType));
                            }
                            else
                            {
                                producers = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && c.Value.UnitCalculation.Unit.BuildProgress == 1 && c.Value.UnitCalculation.Unit.HasAddOnTag && // reactors first
                                        SharkyUnitData.ReactorTypes.Contains((UnitTypes)ActiveUnitData.SelfUnits[c.Value.UnitCalculation.Unit.AddOnTag].Unit.UnitType) && c.Value.UnitCalculation.Unit.Orders.Count() <= 1).Select(x => x.Value);
                                if (producers.Count() == 0 && !(unitType == UnitTypes.TERRAN_HELLION && BuildOptions.TerranBuildOptions.OnlyBuildHellionsWithReactors))
                                {
                                    producers = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1 && !c.Value.UnitCalculation.Unit.HasAddOnTag).Select(x => x.Value); // no add on second
                                    if (producers.Count() == 0)
                                    {
                                        producers = ActiveUnitData.Commanders.Where(c => unitData.ProducingUnits.Contains((UnitTypes)c.Value.UnitCalculation.Unit.UnitType) && !c.Value.UnitCalculation.Unit.IsActive && c.Value.UnitCalculation.Unit.BuildProgress == 1).Select(x => x.Value); // tech lab last
                                    }
                                }
                            }
                        }

                        if (producers.Any())
                        {
                            var producer = ProducerSelector.SelectBestProducer(unitType, producers);
                            if (producer.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_GATEWAY && SharkyUnitData.ResearchedUpgrades.Contains((uint)Upgrades.WARPGATERESEARCH))
                            {
                                var action = producer.Order(MacroData.Frame, Abilities.RESEARCH_WARPGATE);
                                if (action != null)
                                {
                                    CameraManager.SetCamera(producer.UnitCalculation.Position);
                                    commands.AddRange(action);
                                    return commands;
                                }
                            }
                            else if (producer.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPGATE)
                            {
                                var targetLocation = TargetingData.ForwardDefensePoint;
                                var undefendedNexus = ActiveUnitData.SelfUnits.FirstOrDefault(u => u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && u.Value.NearbyEnemies.Any(a => a.UnitClassifications.HasFlag(UnitClassification.ArmyUnit)) && !u.Value.NearbyAllies.Any(a => a.UnitClassifications.HasFlag(UnitClassification.ArmyUnit))).Value;
                                if (undefendedNexus != null)
                                {
                                    targetLocation = new Point2D { X = undefendedNexus.Position.X, Y = undefendedNexus.Position.Y };
                                }
                                else if (AttackData.Attacking)
                                {
                                    targetLocation = TargetingData.AttackPoint;
                                }
                                if (MacroData.ProtossMacroData.AlwaysWarpInAtWarpPrismsIfPossible)
                                {
                                    var warpPrism = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISMPHASING).OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, targetLocation.ToVector2())).FirstOrDefault();
                                    if (warpPrism != null)
                                    {
                                        targetLocation = warpPrism.UnitCalculation.Position.ToPoint2D();
                                    }
                                }

                                var location = WarpInPlacement.FindPlacement(targetLocation, unitType, 1, maxDistance: 0);
                                if (location != null)
                                {
                                    var action = producer.Order(MacroData.Frame, unitData.WarpInAbility, location);
                                    if (action != null)
                                    {
                                        CameraManager.SetCamera(location);
                                        commands.AddRange(action);
                                        return commands;
                                    }
                                }
                            }
                            else
                            {
                                var allowSpam = false;
                                if (producer.UnitCalculation.Unit.HasAddOnTag && SharkyUnitData.ReactorTypes.Contains((UnitTypes)ActiveUnitData.SelfUnits[producer.UnitCalculation.Unit.AddOnTag].Unit.UnitType))
                                {
                                    allowSpam = true;
                                }
                                var action = producer.Order(MacroData.Frame, unitData.Ability, allowSpam: allowSpam);
                                if (action != null)
                                {
                                    if (SharkyUnitData.ZergMorphUnitAbilities.Contains(unitData.Ability))
                                    {
                                        MicroTaskData.StealCommanderFromAllTasks(producer);
                                        producer.UnitRole = UnitRole.Morph;
                                        producer.Claimed = false;
                                    }
                                    CameraManager.SetCamera(producer.UnitCalculation.Position);
                                    commands.AddRange(action);
                                    return commands;
                                }
                            }
                        }
                    }
                }
                else if (unitType == UnitTypes.PROTOSS_ARCHON)
                {
                    var templar = ActiveUnitData.Commanders.Where(c => c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || c.Value.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_DARKTEMPLAR);
                    var merges = templar.Count(a => a.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON));
                    if (merges + UnitCountService.Count(UnitTypes.PROTOSS_ARCHON) < MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ARCHON])
                    {
                        var mergables = templar.Where(c => !c.Value.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.MORPH_ARCHON || o.AbilityId == (uint)Abilities.MORPH_ARCHON2));
                        if (mergables.Count() >= 2)
                        {
                            var commanders = mergables.OrderBy(c => c.Value.UnitCalculation.Unit.Energy).Take(2);
                            var action = commanders.First().Value.Merge(commanders.Last().Value.UnitCalculation.Unit.Tag);
                            if (action != null)
                            {
                                CameraManager.SetCamera(commanders.First().Value.UnitCalculation.Position);
                                commanders.First().Value.UnitRole = UnitRole.Morph;
                                commanders.Last().Value.UnitRole = UnitRole.Morph;
                                commands.Add(action);
                                return commands;
                            }
                        }
                    }
                }
            }

            return commands;
        }
    }
}
