namespace Sharky.Chat
{
    public class ChatService
    {
        IChatDataService ChatDataService;
        SharkyOptions SharkyOptions;
        ActiveChatData ActiveChatData;
        EnemyData EnemyData;
        MacroData MacroData;

        int ChatTypeLastSentFrame;
        int MaxChatTypeFrequency;

        public ChatService(DefaultSharkyBot defaultSharkyBot)
        {
            ChatDataService = defaultSharkyBot.ChatDataService;
            SharkyOptions = defaultSharkyBot.SharkyOptions;
            ActiveChatData = defaultSharkyBot.ActiveChatData;
            EnemyData = defaultSharkyBot.EnemyData;
            MacroData = defaultSharkyBot.MacroData;

            MaxChatTypeFrequency = (int)(SharkyOptions.FramesPerSecond * 15);
            ChatTypeLastSentFrame = -MaxChatTypeFrequency;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void SendChatType(string chatType, bool instant = false)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (ChatTypeLastSentFrame + MaxChatTypeFrequency > MacroData.Frame)
            {
                return;
            }
            ChatTypeLastSentFrame = MacroData.Frame;

            var lastGame = EnemyData?.EnemyPlayer.Games.FirstOrDefault();
            if (lastGame != null)
            {
                chatType = $"{(Result)lastGame.Result}-{chatType}";
            }

            var data = ChatDataService.GetChatTypeData(chatType);

            if (data != null)
            {
                var messages = ChatDataService.GetChatTypeMessage(data, ActiveChatData.EnemyName);
                SendChatMessages(messages);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void SendChatMessage(string message, bool instant = false)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (instant)
            {
                SendInstantChatMessage(message);
            }
            else
            {
                SendChatMessages(new List<string> { message });
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void SendAllyChatMessage(string message, bool instant = false)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (instant)
            {
                SendInstantChatMessage(message, true);
            }
            else
            {
                SendChatMessages(new List<string> { message }, true);
            }
        }

        public async void SendChatMessages(IEnumerable<string> messages, bool teamChannel = false)
        {
            var typeTime = 0;
            foreach (var message in messages.ToList())
            {
                var chatAction = new SC2Action { ActionChat = new ActionChat { Message = message } };
                if (teamChannel)
                {
                    chatAction.ActionChat.Channel = ActionChat.Types.Channel.Team;
                }

                typeTime += message.Length * 80; // simulate typing at 80 ms per keystroke
                // translate framerate to real
                await Task.Delay((int)(typeTime / ActiveChatData.TimeModulation)).ContinueWith((task) => { ActiveChatData.ChatActions.Add(chatAction); });
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void SendDebugChatMessage(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            SendDebugChatMessages(new List<string> { message });
        }

        public async void SendDebugChatMessages(IEnumerable<string> messages)
        {
            if (SharkyOptions.Debug)
            {
                var typeTime = 0;
                foreach (var message in messages)
                {
                    var chatAction = new SC2Action { ActionChat = new ActionChat { Message = message, Channel = ActionChat.Types.Channel.Team } };

                    typeTime += message.Length * 80; // simulate typing at 80 ms per keystroke
                                                     // translate framerate to real
                    await Task.Delay((int)(typeTime / ActiveChatData.TimeModulation)).ContinueWith((task) => { ActiveChatData.ChatActions.Add(chatAction); });
                }
            }
        }

        void SendInstantChatMessage(string message, bool teamChannel = false)
        {
            var chatAction = new SC2Action { ActionChat = new ActionChat { Message = message } };
            if (teamChannel)
            {
                chatAction.ActionChat.Channel = ActionChat.Types.Channel.Team;
            }
            ActiveChatData.ChatActions.Add(chatAction);
        }
    }
}
