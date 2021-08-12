using Sharky.Chat;
using System;

namespace Sharky.EnemyStrategies
{
    public abstract class EnemyStrategy : IEnemyStrategy
    {
        public bool Active { get; private set; }
        public bool Detected { get; private set; }

        protected ChatService ChatService;
        protected EnemyStrategyHistory EnemyStrategyHistory;
        protected ActiveUnitData ActiveUnitData;
        protected SharkyOptions SharkyOptions;
        protected DebugService DebugService;
        protected UnitCountService UnitCountService;
        protected FrameToTimeConverter FrameToTimeConverter;

        public string Name()
        {
            return GetType().Name;
        }

        public void OnFrame(int frame)
        {
            var detected = Detect(frame);

            if (detected)
            {
                Active = true;

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
            ChatService.SendChatType($"{Name()}-EnemyStrategy");
            if (SharkyOptions.TagsEnabled)
            {
                ChatService.SendAllyChatMessage($"Tag:EnemyStrategy-{Name()}");
            }
        }

        protected abstract bool Detect(int frame);
    }
}
