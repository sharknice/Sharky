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
        protected EnemyData EnemyData; // TODO: set this on all enemy strategies

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

            // TODO: check if it was also used last game if won or loss and have specific chat for that
            var lastGame = EnemyData?.EnemyPlayer.Games.FirstOrDefault();
            if (lastGame != null)
            {
                // get the frame and maybe say faster or slower than last time
                var match = lastGame.EnemyStrategies.FirstOrDefault(s => s.Value == Name());
                if (match.Value != null)
                {
                    // check if they have done it every game

                    ChatService.SendChatType($"{(Result)lastGame.Result}-Repeat-{Name()}-EnemyStrategy");
                    return;

                    // you did this last game, this again or do you do this every game, or something, need a chattype pattern

                        // you lost with this last time, you tried this last time and it didn't work, etc.

                        // I'm ready for that this time, I expected this again           
                }
                else
                {
                    ChatService.SendChatType($"{(Result)lastGame.Result}-New-{Name()}-EnemyStrategy");
                    return;
                    // you didn't do this last game, trying something new, need a chat type pattern
                    // ideally one that works with the existing
                }
            }

            ChatService.SendChatType($"{Name()}-EnemyStrategy");
        }

        protected abstract bool Detect(int frame);
    }
}
