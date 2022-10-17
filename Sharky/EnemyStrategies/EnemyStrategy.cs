using Sharky.Chat;
using Sharky.DefaultBot;
using System;
using System.Linq;

namespace Sharky.EnemyStrategies
{
    public abstract class EnemyStrategy : IEnemyStrategy
    {
        public bool Active { get; private set; }
        public bool Detected { get; private set; }
        public int FirstActiveFrame { get; private set; }
        public int LastActiveFrame { get; private set; }

        protected ChatService ChatService;
        protected EnemyStrategyHistory EnemyStrategyHistory;
        protected ActiveUnitData ActiveUnitData;
        protected SharkyOptions SharkyOptions;
        protected DebugService DebugService;
        protected UnitCountService UnitCountService;
        protected FrameToTimeConverter FrameToTimeConverter;
        protected EnemyData EnemyData;

        public string Name()
        {
            return GetType().Name;
        }

        public EnemyStrategy(DefaultSharkyBot defaultSharkyBot) 
        {
            EnemyStrategyHistory = defaultSharkyBot.EnemyStrategyHistory;
            ChatService = defaultSharkyBot.ChatService;
            ActiveUnitData = defaultSharkyBot.ActiveUnitData;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            DebugService = defaultSharkyBot.DebugService;
            UnitCountService = defaultSharkyBot.UnitCountService;
            FrameToTimeConverter = defaultSharkyBot.FrameToTimeConverter;
            EnemyData = defaultSharkyBot.EnemyData;
        }

        public void OnFrame(int frame)
        {
            var detected = Detect(frame);

            if (detected)
            {
                Active = true;
                LastActiveFrame = frame;

                if (FirstActiveFrame == 0)
                {
                    FirstActiveFrame = frame;
                }

                if (!Detected)
                {
                    EnemyStrategyHistory.History[frame] = Name();
                    Console.WriteLine($"{frame} {FrameToTimeConverter.GetTime(frame)} Detected: {Name()}");
                    Detected = true;
                    DetectedChat();
                }
                DebugService.DrawText($"Active: {Name()}");
            }
            else
            {
                Active = false;
            }
        }

        protected void DetectedChat()
        {
            ChatService.Tag($"EnemyStrategy-{Name()}");

            var lastGame = EnemyData?.EnemyPlayer.Games.FirstOrDefault();
            if (lastGame != null)
            {
                var match = lastGame.EnemyStrategies.FirstOrDefault(s => s.Value == Name());
                if (match.Value != null)
                {
                    ChatService.SendChatType($"Repeat-{Name()}-EnemyStrategy");
                    return;        
                }
                else
                {
                    ChatService.SendChatType($"New-{Name()}-EnemyStrategy");
                    return;
                }
            }

            ChatService.SendChatType($"{Name()}-EnemyStrategy");
        }

        protected abstract bool Detect(int frame);
    }
}
