using SC2APIProtocol;
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

        public OrbitalManager(ActiveUnitData activeUnitData, BaseData baseData, EnemyData enemyData)
        {
            ActiveUnitData = activeUnitData;
            BaseData = baseData;
            EnemyData = enemyData;
        }

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<SC2APIProtocol.Action>();

            var orbital = ActiveUnitData.Commanders.Values.Where(c => c.UnitCalculation.Unit.UnitType == (uint)UnitTypes.TERRAN_ORBITALCOMMAND && c.UnitCalculation.Unit.BuildProgress == 1).OrderByDescending(c => c.UnitCalculation.Unit.Energy).FirstOrDefault();
            if (orbital != null)
            {
                var action = Scan(orbital, (int)observation.Observation.GameLoop);
                if (action != null)
                {
                    actions.AddRange(action);
                }
                else
                {
                    action = Mule(orbital, (int)observation.Observation.GameLoop);
                    if (action != null)
                    {
                        actions.AddRange(action);
                    }
                }
            }

            return actions;
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
                foreach (var baseLocation in BaseData.SelfBases.Where(b => b.ResourceCenter.BuildProgress == 1 && b.MineralFields.Count() > 0))
                {
                    var baseVector = new Vector2(baseLocation.Location.X, baseLocation.Location.Y);
                    var mineralPatch = baseLocation.MineralFields.OrderByDescending(m => Vector2.DistanceSquared(new Vector2(m.Pos.X, m.Pos.Y), baseVector)).ThenByDescending(m => m.MineralContents).FirstOrDefault();
                    if (mineralPatch != null)
                    {
                        return orbital.Order(frame, Abilities.EFFECT_CALLDOWNMULE, targetTag: mineralPatch.Tag);
                    }
                }
            }

            return null;
        }
    }
}
