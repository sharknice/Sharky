namespace Sharky
{
    public class TagService
    {
        private readonly ChatService ChatService;
        private readonly SharkyOptions SharkyOptions;
        private readonly VersionService VersionService;
        private readonly MacroData MacroData;
        private readonly FrameToTimeConverter FrameToTimeConverter;

        private bool ExceptionTagged = false;
        private HashSet<string> ExceptionsTagged = new HashSet<string>();

        private HashSet<UnitTypes> UnitTagsUsed = new HashSet<UnitTypes>();
        private HashSet<UnitTypes> EnemyUnitTagsUsed = new HashSet<UnitTypes>();
        private HashSet<Upgrades> UpgradeTagsUsed = new HashSet<Upgrades>();
        private HashSet<EnemyStrategy> EnemyStrategyTagsUsed = new HashSet<EnemyStrategy>();
        private HashSet<string> AbilityTagsUsed = new HashSet<string>();

        private HashSet<string> TagsUsed = new HashSet<string>();

        public TagService(ChatService chatService, SharkyOptions sharkyOptions, VersionService versionService, MacroData macroData, FrameToTimeConverter frameToTimeConverter)
        {
            ChatService = chatService;
            SharkyOptions = sharkyOptions;
            VersionService = versionService;
            MacroData = macroData;
            FrameToTimeConverter = frameToTimeConverter;

            // Basic units we dont want to get tagged as they appear probably in every game (with very few exceptions)
            UnitTagsUsed = new HashSet<UnitTypes>() 
            { 
                UnitTypes.ZERG_DRONE,
                UnitTypes.ZERG_HATCHERY,
                UnitTypes.ZERG_SPAWNINGPOOL,
                UnitTypes.ZERG_OVERLORD,
                UnitTypes.ZERG_LARVA,
                UnitTypes.ZERG_EGG,
                UnitTypes.ZERG_OVERLORD,
                UnitTypes.ZERG_CREEPTUMOR,
                UnitTypes.ZERG_CREEPTUMORBURROWED,
                UnitTypes.ZERG_CREEPTUMORQUEEN,
                UnitTypes.ZERG_EXTRACTOR,
                UnitTypes.ZERG_QUEEN,
                UnitTypes.ZERG_BROODLING,
                UnitTypes.ZERG_ZERGLING,
                UnitTypes.ZERG_ZERGLINGBURROWED,
                UnitTypes.ZERG_CHANGELING,
                UnitTypes.ZERG_CHANGELINGMARINE,
                UnitTypes.ZERG_CHANGELINGMARINESHIELD,
                UnitTypes.ZERG_CHANGELINGZEALOT,
                UnitTypes.ZERG_CHANGELINGZERGLING,
                UnitTypes.ZERG_CHANGELINGZERGLINGWINGS,

                UnitTypes.TERRAN_SCV,
                UnitTypes.TERRAN_SUPPLYDEPOT,
                UnitTypes.TERRAN_SUPPLYDEPOTLOWERED,
                UnitTypes.TERRAN_COMMANDCENTER,
                UnitTypes.TERRAN_ORBITALCOMMAND,
                UnitTypes.TERRAN_ORBITALCOMMANDFLYING,
                UnitTypes.TERRAN_BARRACKS,
                UnitTypes.TERRAN_BARRACKSFLYING,
                UnitTypes.TERRAN_BARRACKSREACTOR,
                UnitTypes.TERRAN_BARRACKSTECHLAB,
                UnitTypes.TERRAN_FACTORY,
                UnitTypes.TERRAN_FACTORYFLYING,
                UnitTypes.TERRAN_FACTORYREACTOR,
                UnitTypes.TERRAN_FACTORYTECHLAB,
                UnitTypes.TERRAN_STARPORT,
                UnitTypes.TERRAN_STARPORTFLYING,
                UnitTypes.TERRAN_STARPORTREACTOR,
                UnitTypes.TERRAN_STARPORTTECHLAB,
                UnitTypes.TERRAN_MULE,
                UnitTypes.TERRAN_REFINERY,

                UnitTypes.PROTOSS_PROBE,
                UnitTypes.PROTOSS_NEXUS,
                UnitTypes.PROTOSS_PYLON,
                UnitTypes.PROTOSS_GATEWAY,
                UnitTypes.PROTOSS_FORGE,
                UnitTypes.PROTOSS_CYBERNETICSCORE,
                UnitTypes.PROTOSS_ASSIMILATOR,

                // Some additional unit tag ignores to fit to the tag limit on aiarena
                UnitTypes.PROTOSS_WARPGATE,
                UnitTypes.PROTOSS_SHIELDBATTERY,
                UnitTypes.PROTOSS_FORGE,
                UnitTypes.PROTOSS_ROBOTICSFACILITY,
                UnitTypes.PROTOSS_STARGATE,
                UnitTypes.PROTOSS_PHOTONCANNON,
                UnitTypes.PROTOSS_ADEPT,
                UnitTypes.PROTOSS_STALKER,
                UnitTypes.PROTOSS_ZEALOT,
                UnitTypes.PROTOSS_SENTRY,
                UnitTypes.PROTOSS_WARPPRISM,
                UnitTypes.PROTOSS_WARPPRISMPHASING,
                UnitTypes.PROTOSS_OBSERVER,
                UnitTypes.PROTOSS_IMMORTAL,

                UnitTypes.TERRAN_STARPORT,
                UnitTypes.TERRAN_STARPORTFLYING,
                UnitTypes.TERRAN_ARMORY,
                UnitTypes.TERRAN_MEDIVAC,
                UnitTypes.TERRAN_ENGINEERINGBAY,
                UnitTypes.TERRAN_WIDOWMINE,
                UnitTypes.TERRAN_WIDOWMINEBURROWED,
                UnitTypes.TERRAN_MARAUDER,
                UnitTypes.TERRAN_REAPER,
                UnitTypes.TERRAN_KD8CHARGE,

                UnitTypes.ZERG_HYDRALISK,
                UnitTypes.ZERG_HYDRALISKBURROWED,
                UnitTypes.ZERG_HYDRALISKDEN,
                UnitTypes.ZERG_SPIRE,
                UnitTypes.ZERG_ROACH,
                UnitTypes.ZERG_ROACHBURROWED,
                UnitTypes.ZERG_ROACHWARREN,
                UnitTypes.ZERG_BANELING,
                UnitTypes.ZERG_BANELINGBURROWED,
                UnitTypes.ZERG_BANELINGCOCOON,
                UnitTypes.ZERG_BANELINGNEST,
                UnitTypes.ZERG_RAVAGER,
                UnitTypes.ZERG_RAVAGERBURROWED,
                UnitTypes.ZERG_RAVAGERCOCOON,
            };

            EnemyUnitTagsUsed = new HashSet<UnitTypes>(UnitTagsUsed);
        }

        private string TimeFormat(TimeSpan timeSpan)
        {
            var str = string.Empty;
            if (timeSpan.Hours > 0)
            {
                str = $"{timeSpan.Hours:00}_";
            }

            str = $"{str}{timeSpan.Minutes:00}_{timeSpan.Seconds:00}";

            return str;
        }

        /// <summary>
        /// Sends chat message in tag format
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="ignoreDuplicateCheck">If true, sends the chat message even though this tag was already used</param>
        public void Tag(string tag, bool ignoreDuplicateCheck = false, bool allowAppendTime = true)
        {
            if (!SharkyOptions.TagOptions.TagsEnabled)
            {
                return;
            }

            tag = tag.ToLower();
            tag = new string(tag.Select(c => (char.IsLetterOrDigit(c) || c == '-') ? c : '_').ToArray());

            if (ignoreDuplicateCheck || !TagsUsed.Contains(tag))
            {
                TagsUsed.Add(tag);

                if (allowAppendTime && SharkyOptions.TagOptions.TagTime)
                {
                    var elapsed = FrameToTimeConverter.GetTime(MacroData.Frame);
                    tag = $"{tag}_{TimeFormat(elapsed)}";
                }

                if (SharkyOptions.TagOptions.TagsAllChat)
                {
                    ChatService.SendChatMessage($"Tag:{tag}", true);
                }
                else
                {
                    ChatService.SendAllyChatMessage($"Tag:{tag}", true);
                }
            }
        }

        public void TagUnits(ActiveUnitData activeUnitData)
        {
            foreach (var unit in activeUnitData.EnemyUnits.Values)
            {
                TagUnit((UnitTypes)unit.Unit.UnitType, true);
            }

            foreach (var unit in activeUnitData.SelfUnits.Values)
            {
                TagUnit((UnitTypes)unit.Unit.UnitType);
            }
        }

        public void TagAbility(string tag)
        {
            if (!SharkyOptions.TagOptions.AbilityTagsEnabled)
                return;

            if (!AbilityTagsUsed.Contains(tag))
            {
                AbilityTagsUsed.Add(tag);
                Tag($"a_{tag}");
            }
        }

        public void TagUnit(UnitTypes unit, bool enemy = false)
        {
            if (!SharkyOptions.TagOptions.UnitTagsEnabled)
                return;

            var unitNameParts = unit.ToString().Split('_');
            var tag = unitNameParts.Count() > 1 ? unitNameParts[1] : unitNameParts[0];

            if (enemy)
            {
                if (!EnemyUnitTagsUsed.Contains(unit))
                {
                    EnemyUnitTagsUsed.Add(unit);
                    Tag($"eu_{tag}");
                }
            }
            else
            {
                if (!UnitTagsUsed.Contains(unit))
                {
                    UnitTagsUsed.Add(unit);
                    Tag($"u_{tag}");
                }
            }
        }

        public void TagEnemyStrategy(EnemyStrategy strategy)
        {
            if (!SharkyOptions.TagOptions.UnitTagsEnabled)
                return;

            if (!EnemyStrategyTagsUsed.Contains(strategy))
            {
                var tag = strategy.Name();
                EnemyStrategyTagsUsed.Add(strategy);
                Tag($"es_{tag}");
            }
        }

        public void TagUpgrades(SharkyUnitData sharkyUnitData)
        {
            if (sharkyUnitData.ResearchedUpgrades is null)
                return;

            foreach (var upgrade in sharkyUnitData.ResearchedUpgrades)
            {
                TagUpgrade((Upgrades)upgrade);
            }
        }

        public void TagUpgrade(Upgrades upgrade)
        {
            if (!SharkyOptions.TagOptions.UpgradeTagsEnabled)
                return;

            if (!UpgradeTagsUsed.Contains(upgrade))
            {
                var tag = upgrade.ToString();
                UpgradeTagsUsed.Add(upgrade);
                Tag($"g_{tag}");
            }
        }

        public void TagVersion()
        {
            Tag($"v_{VersionService.BuildDate.ToString("yyyy-MM-dd__HH-mm-ss")}", allowAppendTime: false);
        }

        public void TagBuild(SharkyBuild build)
        {
            Tag($"b_{build.Name()}");
        }

        public void TagException(string type = null)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                if (!ExceptionTagged)
                {
                    Tag($"Tag:Exception", true);
                    ExceptionTagged = true;
                }
            }
            else
            {
                if (!ExceptionsTagged.Contains(type))
                {
                    Tag($"Tag:Exception_{type}", true);
                    ExceptionsTagged.Add(type);
                }
            }
        }
    }
}
