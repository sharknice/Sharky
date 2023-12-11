namespace Sharky.MicroTasks.Mining
{
    public class GasMiner
    {
        BaseData BaseData;
        SharkyUnitData SharkyUnitData;
        CollisionCalculator CollisionCalculator;

        public GasMiner(DefaultSharkyBot defaultSharkyBot)
        {
            BaseData = defaultSharkyBot.BaseData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;
        }

        public List<SC2APIProtocol.Action> MineGas(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var selfBase in BaseData.SelfBases)
            {
                if (selfBase.ResourceCenter == null) { continue; }
                var baseVector = new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y);
                foreach (var miningInfo in selfBase.GasMiningInfo)
                {
                    if (miningInfo.Workers.Any() && miningInfo.ResourceUnit.VespeneContents == 0)
                    {
                        foreach (var worker in miningInfo.Workers)
                        {
                            worker.UnitRole = UnitRole.None;
                        }
                        miningInfo.Workers.Clear();
                    }

                    var mineralVector = new Vector2(miningInfo.ResourceUnit.Pos.X, miningInfo.ResourceUnit.Pos.Y);
                    var count = miningInfo.Workers.Count;
                    foreach (var worker in miningInfo.Workers.Where(w => w.UnitRole == UnitRole.Gas))
                    {
                        if (count == 3 || miningInfo.ResourceUnit.UnitType == (uint)UnitTypes.PROTOSS_ASSIMILATORRICH || miningInfo.ResourceUnit.UnitType == (uint)UnitTypes.TERRAN_REFINERYRICH || miningInfo.ResourceUnit.UnitType == (uint)UnitTypes.ZERG_EXTRACTORRICH)
                        {
                            if (worker.LastTargetTag != miningInfo.ResourceUnit.Tag && worker.LastAbility != Abilities.SMART)
                            {
                                if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                                {
                                    var returnAction = worker.Order(frame, Abilities.HARVEST_RETURN);
                                    if (returnAction != null)
                                    {
                                        actions.AddRange(returnAction);
                                    }
                                    continue;
                                }
                                var action = worker.Order(frame, Abilities.SMART, null, miningInfo.ResourceUnit.Tag, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            continue;
                        }
                        var workerVector = worker.UnitCalculation.Position;
                        if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                        {
                            var distanceSquared = Vector2.DistanceSquared(baseVector, workerVector);
                            var onPath = CollisionCalculator.Collides(worker.UnitCalculation.Position, 2, new Vector2(miningInfo.DropOffPoint.X, miningInfo.DropOffPoint.Y), new Vector2(miningInfo.HarvestPoint.X, miningInfo.HarvestPoint.Y));

                            if (distanceSquared > 25 || distanceSquared < 10 || !onPath)
                            {
                                var action = worker.Order(frame, Abilities.HARVEST_RETURN);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            else
                            {
                                var action = worker.Order(frame, Abilities.MOVE, miningInfo.DropOffPoint, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                        else
                        {
                            var onPath = CollisionCalculator.Collides(worker.UnitCalculation.Position, 2, new Vector2(miningInfo.DropOffPoint.X, miningInfo.DropOffPoint.Y), new Vector2(miningInfo.HarvestPoint.X, miningInfo.HarvestPoint.Y));
                            var touchingWorker = worker.UnitCalculation.NearbyAllies.Take(25).Any(w => Vector2.DistanceSquared(workerVector, w.Position) < .5);
                            if (!onPath || touchingWorker || Vector2.DistanceSquared(mineralVector, workerVector) < 4)
                            {
                                var action = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningInfo.ResourceUnit.Tag, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                            else
                            {
                                var action = worker.Order(frame, Abilities.MOVE, miningInfo.HarvestPoint, 0, false);
                                if (action != null)
                                {
                                    actions.AddRange(action);
                                }
                            }
                        }
                    }
                }
            }

            return actions;
        }
    }
}
