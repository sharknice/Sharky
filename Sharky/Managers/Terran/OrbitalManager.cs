using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.Managers.Protoss
{
    public class OrbitalManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        BaseData BaseData;
        EnemyData EnemyData;
        MacroData MacroData;
        UnitCountService UnitCountService;
        ChatService ChatService;
        ResourceCenterLocator ResourceCenterLocator;

        bool MulesUnderAttackChatSent;

        public OrbitalManager(ActiveUnitData activeUnitData, BaseData baseData, EnemyData enemyData, MacroData macroData, UnitCountService unitCountService, ChatService chatService, ResourceCenterLocator resourceCenterLocator)
        {
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            EnemyData = enemyData;
            MacroData = macroData;
            UnitCountService = unitCountService;
            ChatService = chatService;
            ResourceCenterLocator = resourceCenterLocator;

            MulesUnderAttackChatSent = false;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();

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

        List<SC2APIProtocol.Action> TakeBases(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var excess = UnitCountService.EquivalentTypeCount(UnitTypes.TERRAN_COMMANDCENTER) - BaseData.SelfBases.Count() - MacroData.DesiredMacroCommandCenters;
            if (excess > 0)
            {
                var flyingOrbitals = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMANDFLYING);
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
                            var location = ResourceCenterLocator.GetResourceCenterLocation();
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
                    return orbital.Order(frame, Abilities.EFFECT_SCAN, new Point2D { X = undetectedEnemy.Value.Position.X, Y = undetectedEnemy.Value.Position.Y });
                }
            }

            return null;
        }

        List<SC2APIProtocol.Action> Mule(UnitCommander orbital, int frame)
        {
            if (orbital.UnitCalculation.Unit.Energy >= 50 && !EnemyData.EnemyStrategies["InvisibleAttacks"].Detected || orbital.UnitCalculation.Unit.Energy > 95)
            {
                var highestMineralPatch = BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress > .99 && b.MineralFields.Count() > 0 && ActiveUnitData.SelfUnits[b.ResourceCenter.Tag].NearbyEnemies.Count(e => e.UnitClassifications.Contains(UnitClassification.ArmyUnit)) < 2).SelectMany(m => m.MineralFields).OrderByDescending(m => m.MineralContents).FirstOrDefault();
                if (highestMineralPatch != null)
                {
                    return orbital.Order(frame, Abilities.EFFECT_CALLDOWNMULE, targetTag: highestMineralPatch.Tag);
                }

                foreach (var baseLocation in BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.MineralFields.Count() > 0))
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
            }

            return null;
        }
    }
}
