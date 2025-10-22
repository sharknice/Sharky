﻿namespace Sharky.Managers.Protoss
{
    public class NexusManager : SharkyManager
    {
        ActiveUnitData ActiveUnitData;
        SharkyUnitData SharkyUnitData;
        ChronoData ChronoData;
        EnemyData EnemyData;
        TagService TagService;
        CameraManager CameraManager;
        BaseData BaseData;
        SharkyOptions SharkyOptions;
        float EnergyRechargeRangeSquared = 144;
        float RestoreRange = 6;
        int LastEnergyRechargeFrame = 0;

        public NexusManager(ActiveUnitData activeUnitData, SharkyUnitData sharkyUnitData, ChronoData chronoData, EnemyData enemyData, TagService tagService, CameraManager cameraManager, BaseData baseData, SharkyOptions sharkyOptions)
        {
            ActiveUnitData = activeUnitData;
            SharkyUnitData = sharkyUnitData;
            ChronoData = chronoData;
            EnemyData = enemyData;
            TagService = tagService;
            CameraManager = cameraManager;
            BaseData = baseData;
            SharkyOptions = sharkyOptions;
        }

        public override IEnumerable<SC2Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2Action>();

            if (EnemyData.SelfRace != Race.Protoss)
            {
                return actions;
            }

            var nexuses = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS && c.UnitCalculation.Unit.BuildProgress == 1).OrderByDescending(c => c.UnitCalculation.Unit.Energy);
            foreach (var nexus in nexuses)
            {
                var action = EnerygyRecharge(nexus, (int)observation.Observation.GameLoop);
                if (action != null)
                {
                    TagService.TagAbility("enerygyrecharge");
                    actions.AddRange(action);
                }
                else
                {
                    action = ChronoBoost(nexus, (int)observation.Observation.GameLoop);
                    if (action != null)
                    {
                        TagService.TagAbility("chronoboost");
                        actions.AddRange(action);
                        return actions;
                    }               
                }

                if (nexus.UnitRole != UnitRole.Defend && nexus.UnitCalculation.NearbyAllies.Any(a => a.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && Vector2.DistanceSquared(nexus.UnitCalculation.Position, a.Position) <= 64))
                {
                    nexus.UnitRole = UnitRole.Defend;
                }

                if (!nexus.RallyPointSet)
                {
                    var baseData = BaseData.SelfBases.FirstOrDefault(b => b.Location.X == nexus.UnitCalculation.Position.X && b.Location.Y == nexus.UnitCalculation.Position.Y);
                    if (baseData != null && baseData.MineralMiningInfo.Any(m => m.Workers.Count() > 0))
                    {
                        var unsaturatedMineralPatch = baseData.MineralMiningInfo.Where(m => m.Workers.Count < 2).OrderBy(m => Vector2.DistanceSquared(m.HarvestPoint.ToVector2(), baseData.Location.ToVector2())).FirstOrDefault();
                        if (unsaturatedMineralPatch != null)
                        {
                            action = nexus.Order((int)observation.Observation.GameLoop, Abilities.RALLY_BUILDING, unsaturatedMineralPatch.HarvestPoint);
                            if (action != null)
                            {
                                nexus.RallyPointSet = true;
                                actions.AddRange(action);
                                return actions;
                            }
                        }
                        nexus.RallyPointSet = true;
                    }
                }
            }

            return actions;
        }

        public bool EnergyRechargeOffCooldown(int frame)
        {
            if (LastEnergyRechargeFrame > 0)
            {
                if ((frame - LastEnergyRechargeFrame) / SharkyOptions.FramesPerSecond <= SharkyUnitData.AbilityCooldownTimes[Abilities.ENERYGYRECHARGE])
                {
                    return false;
                }
            }
            return true;
        }

        List<SC2Action> EnerygyRecharge(UnitCommander nexus, int frame)
        {
            if (nexus.UnitCalculation.Unit.Energy >= 50)
            {
                if (!EnergyRechargeOffCooldown(frame))
                {
                    return null;
                }

                foreach (var energyUnit in nexus.UnitCalculation.NearbyAllies.Where(u => (u.Unit.UnitType == (uint)UnitTypes.PROTOSS_HIGHTEMPLAR || u.Unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE || u.Unit.UnitType == (uint)UnitTypes.PROTOSS_SENTRY) && u.Unit.BuildProgress == 1 && u.Unit.Energy < u.Unit.EnergyMax - 75 && Vector2.DistanceSquared(nexus.UnitCalculation.Position, u.Position) < EnergyRechargeRangeSquared).OrderBy(u => u.Unit.Energy).ThenBy(u => u.Unit.Shield).ThenBy(u=> u.Unit.Health))
                {
                    CameraManager.SetCamera(energyUnit.Position);
                    LastEnergyRechargeFrame = frame;
                    return nexus.Order(frame, Abilities.ENERYGYRECHARGE, null, energyUnit.Unit.Tag);
                }
            }

            return null;
        }

        /// <summary>
        /// This ability has been removed from StarCraft 2
        /// leaving it here in case they bring it back
        /// </summary>
        List<SC2Action> Overcharge(UnitCommander nexus, int frame)
        {
            if (nexus.UnitCalculation.Unit.Energy >= 50)
            {
                foreach (var shieldBattery in nexus.UnitCalculation.NearbyAllies.Where(u => u.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY && u.Unit.BuildProgress == 1 && Vector2.DistanceSquared(nexus.UnitCalculation.Position, u.Position) < EnergyRechargeRangeSquared).OrderBy(u => u.Unit.Energy))
                {
                    if (shieldBattery.NearbyAllies.Any(a => a.Unit.BuildProgress == 1 && a.EnemiesInRangeOf.Any() && a.Unit.Shield < 5 && Vector2.Distance(shieldBattery.Position, a.Position) <= RestoreRange + a.Unit.Radius))
                    {
                        CameraManager.SetCamera(shieldBattery.Position);
                        return nexus.Order(frame, Abilities.BATTERYOVERCHARGE, null, shieldBattery.Unit.Tag);
                    }
                }
            }

            return null;
        }

        List<SC2Action> ChronoBoost(UnitCommander nexus, int frame)
        {
            if (nexus.UnitRole == UnitRole.Defend && nexus.UnitCalculation.Unit.Energy < 100) { return null; } // save for overcharge or recall

            if (nexus.UnitCalculation.Unit.Energy >= 50)
            {
                foreach (var upgrade in ChronoData.ChronodUpgrades)
                {
                    var upgradeData = SharkyUnitData.UpgradeData[upgrade];
                    var building = ActiveUnitData.SelfUnits.Where(u => u.Value.Unit.IsPowered && !u.Value.Unit.BuffIds.Contains((uint)Buffs.CHRONOBOOST) && upgradeData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)upgradeData.Ability && o.Progress < .90)).FirstOrDefault().Value;
                    if (building != null)
                    {
                        CameraManager.SetCamera(building.Position);
                        return nexus.Order(frame, Abilities.CHRONOBOOST, null, building.Unit.Tag);
                    }
                }

                foreach (var unit in ChronoData.ChronodUnits)
                {
                    var trainingData = SharkyUnitData.TrainingData[unit];
                    var building = ActiveUnitData.SelfUnits.Where(u => (u.Value.Unit.IsPowered || u.Value.Unit.UnitType == (uint)UnitTypes.PROTOSS_NEXUS) && !u.Value.Unit.BuffIds.Contains((uint)Buffs.CHRONOBOOST) && trainingData.ProducingUnits.Contains((UnitTypes)u.Value.Unit.UnitType) && u.Value.Unit.Orders.Any(o => o.AbilityId == (uint)trainingData.Ability)).FirstOrDefault().Value;
                    if (building != null)
                    {
                        CameraManager.SetCamera(building.Position);
                        return nexus.Order(frame, Abilities.CHRONOBOOST, null, building.Unit.Tag);
                    }
                }
            }

            return null;
        }
    }
}
