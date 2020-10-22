using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Managers
{
    public class UnitDataManager : SharkyManager
    {
        Dictionary<uint, UnitTypeData> UnitData;
        /// <summary>
        /// key is ability, value is the unitType it belongs to
        /// </summary>
        Dictionary<uint, uint> UnitAbilities;
        HashSet<UnitTypes> BuildingTypes;

        public UnitDataManager()
        {
            BuildingTypes = new HashSet<UnitTypes> {
                UnitTypes.PROTOSS_CYBERNETICSCORE,
                UnitTypes.PROTOSS_DARKSHRINE,
                UnitTypes.PROTOSS_FLEETBEACON,
                UnitTypes.PROTOSS_FORGE,
                UnitTypes.PROTOSS_GATEWAY,
                UnitTypes.PROTOSS_NEXUS,
                UnitTypes.PROTOSS_PYLON,
                UnitTypes.PROTOSS_PYLONOVERCHARGED,
                UnitTypes.PROTOSS_ROBOTICSBAY,
                UnitTypes.PROTOSS_ROBOTICSFACILITY,
                UnitTypes.PROTOSS_SHIELDBATTERY,
                UnitTypes.PROTOSS_STARGATE,
                UnitTypes.PROTOSS_TEMPLARARCHIVE,
                UnitTypes.PROTOSS_TWILIGHTCOUNCIL,
                UnitTypes.TERRAN_ARMORY,
                UnitTypes.TERRAN_BARRACKS,
                UnitTypes.TERRAN_BARRACKSFLYING,
                UnitTypes.TERRAN_BARRACKSREACTOR,
                UnitTypes.TERRAN_BARRACKSTECHLAB,
                UnitTypes.TERRAN_BUNKER,
                UnitTypes.TERRAN_COMMANDCENTER,
                UnitTypes.TERRAN_COMMANDCENTERFLYING,
                UnitTypes.TERRAN_ENGINEERINGBAY,
                UnitTypes.TERRAN_FACTORY,
                UnitTypes.TERRAN_FACTORYFLYING,
                UnitTypes.TERRAN_FACTORYREACTOR,
                UnitTypes.TERRAN_FACTORYTECHLAB,
                UnitTypes.TERRAN_FUSIONCORE,
                UnitTypes.TERRAN_MISSILETURRET,
                UnitTypes.TERRAN_ORBITALCOMMAND,
                UnitTypes.TERRAN_ORBITALCOMMANDFLYING,
                UnitTypes.TERRAN_PLANETARYFORTRESS,
                UnitTypes.TERRAN_REACTOR,
                UnitTypes.TERRAN_REFINERY,
                UnitTypes.TERRAN_SENSORTOWER,
                UnitTypes.TERRAN_STARPORT,
                UnitTypes.TERRAN_STARPORTFLYING,
                UnitTypes.TERRAN_STARPORTREACTOR,
                UnitTypes.TERRAN_STARPORTTECHLAB,
                UnitTypes.TERRAN_SUPPLYDEPOT,
                UnitTypes.TERRAN_SUPPLYDEPOTLOWERED,
                UnitTypes.TERRAN_TECHLAB,
                UnitTypes.ZERG_CREEPTUMOR,
                UnitTypes.ZERG_CREEPTUMORBURROWED,
                UnitTypes.ZERG_CREEPTUMORQUEEN,
                UnitTypes.ZERG_EVOLUTIONCHAMBER,
                UnitTypes.ZERG_EXTRACTOR,
                UnitTypes.ZERG_GREATERSPIRE,
                UnitTypes.ZERG_HATCHERY,
                UnitTypes.ZERG_HIVE,
                UnitTypes.ZERG_LAIR,
                UnitTypes.ZERG_NYDUSCANAL,
                UnitTypes.ZERG_NYDUSNETWORK,
                UnitTypes.ZERG_ROACHWARREN,
                UnitTypes.ZERG_SPAWNINGPOOL,
                UnitTypes.ZERG_SPINECRAWLER,
                UnitTypes.ZERG_SPINECRAWLERUPROOTED,
                UnitTypes.ZERG_SPIRE,
                UnitTypes.ZERG_SPORECRAWLER,
                UnitTypes.ZERG_SPORECRAWLERUPROOTED
            };
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            foreach (UnitTypeData unitType in data.Units)
            {
                UnitData.Add(unitType.UnitId, unitType);
                if (unitType.AbilityId != 0)
                {
                    UnitAbilities.Add(unitType.AbilityId, unitType.UnitId);
                }
            }
        }
    }
}
