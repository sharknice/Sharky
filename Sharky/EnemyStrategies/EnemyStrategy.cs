using SC2APIProtocol;
using Sharky.Chat;
using System;
using System.Linq;

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
        protected EnemyData EnemyData;

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
            if (SharkyOptions.TagsEnabled)
            {
                ChatService.SendAllyChatMessage($"Tag:EnemyStrategy-{Name()}", true);
            }

            var lastGame = EnemyData?.EnemyPlayer.Games.FirstOrDefault();
            if (lastGame != null)
            {
                var match = lastGame.EnemyStrategies.FirstOrDefault(s => s.Value == Name());
                if (match.Value != null)
                {
                    ChatService.SendChatType($"{(Result)lastGame.Result}-Repeat-{Name()}-EnemyStrategy");
                    return;        
                }
                else
                {
                    ChatService.SendChatType($"{(Result)lastGame.Result}-New-{Name()}-EnemyStrategy");
                    return;
                }
            }

            ChatService.SendChatType($"{Name()}-EnemyStrategy");
        }

        protected abstract bool Detect(int frame);
    }
}
