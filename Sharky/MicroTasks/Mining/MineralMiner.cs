﻿namespace Sharky.MicroTasks.Mining
{
    public class MineralMiner
    {
        BaseData BaseData;
        SharkyUnitData SharkyUnitData;
        CollisionCalculator CollisionCalculator;
        DebugService DebugService;
        Dictionary<ulong, int> OffPathTimes = new Dictionary<ulong, int>();

        public MineralMiner(DefaultSharkyBot defaultSharkyBot)
        {
            BaseData = defaultSharkyBot.BaseData;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            CollisionCalculator = defaultSharkyBot.CollisionCalculator;
            DebugService = defaultSharkyBot.DebugService;
        }

        public List<SC2APIProtocol.Action> MineMinerals(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            foreach (var selfBase in BaseData.SelfBases)
            {
                if (selfBase.ResourceCenter == null) { continue; }
                var baseVector = new Vector2(selfBase.ResourceCenter.Pos.X, selfBase.ResourceCenter.Pos.Y);
                foreach (var miningInfo in selfBase.MineralMiningInfo)
                {
                    var mineralVector = new Vector2(miningInfo.ResourceUnit.Pos.X, miningInfo.ResourceUnit.Pos.Y);
                    foreach (var worker in miningInfo.Workers.Where(w => w.UnitRole == UnitRole.Minerals))
                    {
                        var workerVector = worker.UnitCalculation.Position;
                        if (worker.UnitCalculation.Unit.BuffIds.Any(b => SharkyUnitData.CarryingResourceBuffs.Contains((Buffs)b)))
                        {
                            actions.AddRange(ReturnMinerals(frame, baseVector, miningInfo, worker, workerVector, selfBase.ResourceCenter.Tag));
                        }
                        else
                        {
                            actions.AddRange(GatherMinerals(frame, miningInfo, mineralVector, worker, workerVector, selfBase.MineralFields));
                        }
                    }
                    miningInfo.Workers.RemoveAll(w => w.UnitRole != UnitRole.Minerals);
                }
            }

            return actions;
        }

        List<SC2APIProtocol.Action> GatherMinerals(int frame, MiningInfo miningInfo, Vector2 mineralVector, UnitCommander worker, Vector2 workerVector, List<SC2APIProtocol.Unit> mineralFields)
        {
            if (!OffPathTimes.ContainsKey(worker.UnitCalculation.Unit.Tag))
            {
                OffPathTimes[worker.UnitCalculation.Unit.Tag] = 0;
            }

            var touchingWorker = worker.UnitCalculation.NearbyAllies.Take(25).Any(w => Vector2.DistanceSquared(workerVector, w.Position) < .5f && !w.UnitClassifications.HasFlag(UnitClassification.Worker));
            var distanceSquared = Vector2.DistanceSquared(mineralVector, workerVector);
            var onPath = CollisionCalculator.Collides(worker.UnitCalculation.Position, 2, new Vector2(miningInfo.DropOffPoint.X, miningInfo.DropOffPoint.Y), new Vector2(miningInfo.HarvestPoint.X, miningInfo.HarvestPoint.Y));
            if (OffPathTimes[worker.UnitCalculation.Unit.Tag] > 0 || distanceSquared < 2 || distanceSquared > 6 || touchingWorker || !onPath)
            {
                var actions = worker.Order(frame, Abilities.HARVEST_GATHER, null, miningInfo.ResourceUnit.Tag, false);
                actions.AddRange(worker.Order(frame, Abilities.MOVE, miningInfo.DropOffPoint, 0, false, true));
                if (!onPath)
                {
                    if (frame > 100 && distanceSquared < 36)
                    {
                        OffPathTimes[worker.UnitCalculation.Unit.Tag]++;
                    }
                    DebugService.DrawSphere(worker.UnitCalculation.Unit.Pos);
                }
                return actions;
            }
            else
            {
                var actions = worker.Order(frame, Abilities.MOVE, miningInfo.HarvestPoint, 0, false);
                actions.AddRange(worker.Order(frame, Abilities.HARVEST_GATHER, null, miningInfo.ResourceUnit.Tag, false, true));
                return actions;
            }
        }

        List<SC2APIProtocol.Action> ReturnMinerals(int frame, Vector2 baseVector, MiningInfo miningInfo, UnitCommander worker, Vector2 workerVector, ulong baseTag)
        {
            var distanceSquared = Vector2.DistanceSquared(baseVector, workerVector);
            var onPath = CollisionCalculator.Collides(worker.UnitCalculation.Position, 2, new Vector2(miningInfo.DropOffPoint.X, miningInfo.DropOffPoint.Y), new Vector2(miningInfo.HarvestPoint.X, miningInfo.HarvestPoint.Y));

            if (distanceSquared > 20 || !onPath)
            {
                return worker.Order(frame, Abilities.SMART, null, baseTag);
            }
            else if (distanceSquared < 10)
            {
                if (OffPathTimes.TryGetValue(worker.UnitCalculation.Unit.Tag, out int offTime) && offTime > 0)
                {
                    OffPathTimes[worker.UnitCalculation.Unit.Tag] = offTime - 1;                
                }

                var actions = worker.Order(frame, Abilities.HARVEST_RETURN);
                actions.AddRange(worker.Order(frame, Abilities.MOVE, miningInfo.HarvestPoint, 0, false, true));
                return actions;
            }
            else
            {
                var actions = worker.Order(frame, Abilities.MOVE, miningInfo.DropOffPoint, 0, false);
                actions.AddRange(worker.Order(frame, Abilities.SMART, null, baseTag, false, true));
                return actions;
            }
        }
    }
}
