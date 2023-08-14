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

        private HashSet<UnitTypes> UnitTagsWhitelist = new HashSet<UnitTypes>();
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
            UnitTagsWhitelist = new HashSet<UnitTypes>() 
            { 
                UnitTypes.ZERG_LURKERMP,
                UnitTypes.ZERG_LURKERMPBURROWED,
                UnitTypes.ZERG_LURKERDENMP,
                UnitTypes.ZERG_ULTRALISK,
                UnitTypes.ZERG_ULTRALISKBURROWED,
                UnitTypes.ZERG_ULTRALISKCAVERN,
                UnitTypes.ZERG_BANELINGBURROWED,
                UnitTypes.ZERG_INFESTATIONPIT,
                UnitTypes.ZERG_INFESTOR,
                UnitTypes.ZERG_INFESTORBURROWED,
                UnitTypes.ZERG_LOCUSTMP,
                UnitTypes.ZERG_LOCUSTMPFLYING,
                UnitTypes.ZERG_SWARMHOSTMP,
                UnitTypes.ZERG_SWARMHOSTBURROWEDMP,
                UnitTypes.ZERG_HIVE,
                UnitTypes.ZERG_VIPER,
                UnitTypes.ZERG_GREATERSPIRE,
                UnitTypes.ZERG_BROODLORD,

                UnitTypes.TERRAN_BATTLECRUISER,
                UnitTypes.TERRAN_PLANETARYFORTRESS,
                UnitTypes.TERRAN_SENSORTOWER,
                UnitTypes.TERRAN_FUSIONCORE,
                UnitTypes.TERRAN_BANSHEE,
                UnitTypes.TERRAN_LIBERATOR,
                UnitTypes.TERRAN_LIBERATORAG,
                UnitTypes.TERRAN_THOR,
                UnitTypes.TERRAN_THORAP,
                UnitTypes.TERRAN_WIDOWMINE,
                UnitTypes.TERRAN_WIDOWMINEBURROWED,
                UnitTypes.TERRAN_GHOST,
                UnitTypes.TERRAN_GHOSTACADEMY,
                UnitTypes.TERRAN_MEDIVAC,
                UnitTypes.TERRAN_PLANETARYFORTRESS,

                UnitTypes.PROTOSS_MOTHERSHIP,
                UnitTypes.PROTOSS_CARRIER,
                UnitTypes.PROTOSS_TEMPEST,
                UnitTypes.PROTOSS_COLOSSUS,
                UnitTypes.PROTOSS_DARKTEMPLAR,
                UnitTypes.PROTOSS_ARCHON,
                UnitTypes.PROTOSS_HIGHTEMPLAR,
                UnitTypes.PROTOSS_DARKSHRINE,
                UnitTypes.PROTOSS_DISRUPTOR,
                UnitTypes.PROTOSS_FLEETBEACON,
                UnitTypes.PROTOSS_ROBOTICSBAY,
                UnitTypes.PROTOSS_ORACLESTASISTRAP,
                UnitTypes.PROTOSS_PHOTONCANNON,
                UnitTypes.PROTOSS_SHIELDBATTERY,
            };
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
                if (!EnemyUnitTagsUsed.Contains(unit) && UnitTagsWhitelist.Contains(unit))
                {
                    EnemyUnitTagsUsed.Add(unit);
                    Tag($"eu_{tag}");
                }
            }
            else
            {
                if (!UnitTagsUsed.Contains(unit) && UnitTagsWhitelist.Contains(unit))
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
