namespace Sharky.Managers.Terran
{
    public class OrbitalManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;
        EnemyData EnemyData;
        MacroData MacroData;
        UnitCountService UnitCountService;
        TagService TagService;
        ChatService ChatService;
        ResourceCenterLocator ResourceCenterLocator;
        MapDataService MapDataService;
        SharkyUnitData SharkyUnitData;

        public Stack<Point2D> ScanQueue { get; set; }
        public int LastScanFrame { get; private set; }

        bool MulesUnderAttackChatSent;

        public OrbitalManager(ActiveUnitData activeUnitData, BaseData baseData, EnemyData enemyData, MacroData macroData, UnitCountService unitCountService, TagService tagService, ChatService chatService, ResourceCenterLocator resourceCenterLocator, MapDataService mapDataService, SharkyUnitData sharkyUnitData)
        {
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            EnemyData = enemyData;
            MacroData = macroData;
            UnitCountService = unitCountService;
            TagService = tagService;
            ChatService = chatService;
            ResourceCenterLocator = resourceCenterLocator;
            MapDataService = mapDataService;
            SharkyUnitData = sharkyUnitData;

            MulesUnderAttackChatSent = false;

            ScanQueue = new Stack<Point2D>();
            LastScanFrame = 0;
        }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2Action>();

            if (EnemyData.SelfRace != Race.Terran)
            {
                return actions;
            }

            var frame = (int)observation.Observation.GameLoop;

            var takeBaseAction = TakeBases(frame);
            if (takeBaseAction != null)
            {
                actions.AddRange(takeBaseAction);
            }

            var orbital = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND && c.UnitCalculation.Unit.BuildProgress == 1).OrderByDescending(c => c.UnitCalculation.Unit.Energy).FirstOrDefault();
            if (orbital != null)
            {
                var action = Scan(orbital, frame);
                if (action != null)
                {
                    actions.AddRange(action);
                }
                else
                {
                    action = Mule(orbital, frame);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            return actions;
        }

        List<SC2Action> TakeBases(int frame)
        {
            var actions = new List<SC2Action>();

            var excess = UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) - BaseData.SelfBases.Count() - MacroData.DesiredMacroCommandCenters;
            if (excess > 0)
            {
                var flyingOrbitals = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMANDFLYING && c.UnitRole != UnitRole.Repair);
                var macroOrbitals = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND && !BaseData.SelfBases.Any(b => b.ResourceCenter != null && b.ResourceCenter.Tag == c.UnitCalculation.Unit.Tag));
                if (excess > flyingOrbitals.Count() && macroOrbitals.Count() > 0)
                {
                    actions.AddRange(macroOrbitals.FirstOrDefault().Order(frame, Abilities.CANCEL_LAST));
                    actions.AddRange(macroOrbitals.FirstOrDefault().Order(frame, Abilities.LIFT, queue: true));
                    return actions;
                }
                else
                {
                    foreach (var flyingOrbital in flyingOrbitals)
                    {
                        if (!flyingOrbital.UnitCalculation.Unit.Orders.Any(o => o.AbilityId == (uint)Abilities.LAND || o.AbilityId == (uint)Abilities.LAND_ORBITALCOMMAND))
                        {
                            var location = ResourceCenterLocator.GetResourceCenterLocation(false);
                            if (location != null)
                            {
                                actions.AddRange(flyingOrbital.Order(frame, Abilities.LAND, location));
                                return actions;
                            }
                        }
                    }
                }
            }

            return null;
        }

        List<SC2APIProtocol.Action> Scan(UnitCommander orbital, int frame)
        {
            if (orbital.UnitCalculation.Unit.Energy >= 50)
            {
                var undetectedEnemy = ActiveUnitData.EnemyUnits.Where(e => e.Value.Unit.DisplayType == DisplayType.Hidden).OrderByDescending(e => e.Value.EnemiesInRangeOf.Count()).FirstOrDefault();
                if (undetectedEnemy.Value != null && undetectedEnemy.Value.EnemiesInRangeOf.Count() > 0)
                {
                    if (!undetectedEnemy.Value.EnemiesInRangeOf.All(a => a.Unit.UnitType == (uint)UnitTypes.TERRAN_BANSHEE && a.NearbyEnemies.Any(e => e.UnitClassifications.Contains(UnitClassification.Worker))))
                    {
                        LastScanFrame = frame;
                        TagService.TagAbility("scan");
                        return orbital.Order(frame, Abilities.EFFECT_SCAN, new Point2D { X = undetectedEnemy.Value.Position.X, Y = undetectedEnemy.Value.Position.Y });
                    }
                }

                foreach (var siegedTank in ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_SIEGETANKSIEGED))
                {
                    if (siegedTank.BestTarget != null && siegedTank.UnitCalculation.Unit.WeaponCooldown < 0.1f && siegedTank.UnitCalculation.EnemiesInRange.Any(e => e.Unit.Tag == siegedTank.BestTarget.Unit.Tag) && frame - siegedTank.BestTarget.FrameLastSeen > 10 && !MapDataService.SelfVisible(siegedTank.BestTarget.Unit.Pos))
                    {
                        LastScanFrame = frame;
                        TagService.TagAbility("scan");
                        return orbital.Order(frame, Abilities.EFFECT_SCAN, new Point2D { X = siegedTank.BestTarget.Position.X, Y = siegedTank.BestTarget.Position.Y });
                    }
                }

                if (ScanQueue.Count() > 0)
                {
                    var scanPoint = ScanQueue.Pop();
                    LastScanFrame = frame;
                    TagService.TagAbility("scan");
                    return orbital.Order(frame, Abilities.EFFECT_SCAN, scanPoint);
                }
            }

            return null;
        }

        List<SC2APIProtocol.Action> Mule(UnitCommander orbital, int frame)
        {
            if ((orbital.UnitCalculation.Unit.Energy >= 50 && !EnemyData.EnemyStrategies[typeof(InvisibleAttacks).Name].Detected && !EnemyData.EnemyStrategies[typeof(InvisibleAttacksSuspected).Name].Detected) || orbital.UnitCalculation.Unit.Energy > 95)
            {
                var highestMineralPatch = BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress > .99 && b.MineralFields.Count() > 0 && ActiveUnitData.SelfUnits.ContainsKey(b.ResourceCenter.Tag) && ActiveUnitData.SelfUnits[b.ResourceCenter.Tag].NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) < 2).SelectMany(m => m.MineralFields).OrderByDescending(m => m.MineralContents).FirstOrDefault();
                if (highestMineralPatch != null)
                {
                    TagService.TagAbility("mule");
                    return orbital.Order(frame, Abilities.EFFECT_CALLDOWNMULE, targetTag: highestMineralPatch.Tag);
                }

                foreach (var baseLocation in BaseData.SelfBases.Where(b => b.ResourceCenter != null && b.ResourceCenter.BuildProgress == 1 && b.MineralFields.Count() > 0))
                {
                    var baseVector = new Vector2(baseLocation.Location.X, baseLocation.Location.Y);
                    var mineralPatch = baseLocation.MineralFields.OrderByDescending(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), baseVector)).ThenByDescending(m => m.MineralContents).FirstOrDefault();
                    if (mineralPatch != null)
                    {
                        if (!MulesUnderAttackChatSent)
                        {
                            MulesUnderAttackChatSent = true;
                            ChatService.SendChatType("MulesCalledWhileUnderAttack");
                        }
                        return orbital.Order(frame, Abilities.EFFECT_CALLDOWNMULE, targetTag: mineralPatch.Tag);
                    }
                }

                var visibleMineral = ActiveUnitData.NeutralUnits.FirstOrDefault(u => SharkyUnitData.MineralFieldTypes.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.DisplayType == DisplayType.Visible).Value;
                if (visibleMineral != null)
                {
                    TagService.TagAbility("mule");
                    return orbital.Order(frame, Abilities.EFFECT_CALLDOWNMULE, targetTag: visibleMineral.Unit.Tag);
                }
            }

            return null;
        }
    }
}
