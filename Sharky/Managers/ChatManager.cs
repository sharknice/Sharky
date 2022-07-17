using Google.Protobuf.Collections;
using Newtonsoft.Json;
using SC2APIProtocol;
using Sharky.Chat;
using Sharky.EnemyPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Action = SC2APIProtocol.Action;

namespace Sharky.Managers
{
    public class ChatManager : SharkyManager
    {
        HttpClient HttpClient;
        ChatHistory ChatHistory;
        SharkyOptions SharkyOptions;
        IChatDataService ChatDataService;
        IEnemyPlayerService EnemyPlayerManager;
        IEnemyNameService EnemyNameService;
        ChatService ChatService;
        VersionService VersionService;
        ActiveChatData ActiveChatData;
        FrameToTimeConverter FrameToTimeConverter;

        Dictionary<TypeEnum, int> LastResponseTimes;
        Dictionary<TypeEnum, int> ChatTypeFrequencies;

        string ConversationName;
        private PlayerInfo Self;
        private PlayerInfo Enemy;
        bool GreetingSent;

        long LastFrameTime;
        double RuntimeFrameRate;

        bool ApiEnabled;

        public ChatManager(HttpClient httpClient, ChatHistory chatHistory, SharkyOptions sharkyOptions, IChatDataService chatDataService, IEnemyPlayerService enemyPlayerManager, IEnemyNameService enemyNameService, ChatService chatService, ActiveChatData activeChatData, FrameToTimeConverter frameToTimeConverter, VersionService versionService)
        {
            HttpClient = httpClient;
            ChatHistory = chatHistory;
            SharkyOptions = sharkyOptions;
            ChatDataService = chatDataService;
            EnemyPlayerManager = enemyPlayerManager;
            EnemyNameService = enemyNameService;
            ChatService = chatService;
            VersionService = versionService;
            ActiveChatData = activeChatData;
            FrameToTimeConverter = frameToTimeConverter;

            ApiEnabled = false;
            
            LastResponseTimes = new Dictionary<TypeEnum, int>();
            ChatTypeFrequencies = new Dictionary<TypeEnum, int>();
            foreach (var chatType in Enum.GetValues(typeof(TypeEnum)).Cast<TypeEnum>())
            {
                ChatTypeFrequencies[chatType] = 3;
            }
        }

        public override void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentId)
        {
            ActiveChatData.TimeModulation = 1;
            LastFrameTime = 0;
            RuntimeFrameRate = SharkyOptions.FramesPerSecond;

            GreetingSent = false;

            ActiveChatData.ChatActions = new List<Action>();

            Self = gameInfo.PlayerInfo[(int)observation.Observation.PlayerCommon.PlayerId - 1];
            Enemy = gameInfo.PlayerInfo[2 - (int)observation.Observation.PlayerCommon.PlayerId];

            ConversationName = $"starcraft-{Enemy.PlayerId}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            ActiveChatData.EnemyName = EnemyNameService.GetEnemyNameFromId(opponentId, EnemyPlayerManager.Enemies);

            ChatHistory.EnemyChatHistory = new Dictionary<int, string>();
            ChatHistory.MyChatHistory = new Dictionary<int, string>();

            ChatService.Tag($"b-{VersionService.BuildDate}");
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            UpdateTimeModulation();
            ObserveChatAsync(observation.Chat, (int)observation.Observation.GameLoop);

            if (!GreetingSent && observation.Observation.GameLoop > 20)
            {
                ChatService.SendChatType($"{ActiveChatData.EnemyName}-Greeting");
                GreetingSent = true;
            }

            return PopChatActions();
        }

        void UpdateTimeModulation()
        {
            if (LastFrameTime != 0)
            {
                RuntimeFrameRate = 1000.0 / (DateTimeOffset.Now.ToUnixTimeMilliseconds() - LastFrameTime);
                ActiveChatData.TimeModulation = RuntimeFrameRate / SharkyOptions.FramesPerSecond;
            }
            LastFrameTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void ObserveChatAsync(RepeatedField<ChatReceived> chatsReceived, int frame)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach (var chatReceived in chatsReceived)
            {
                var chat = new Chat.Chat { botName = Self.PlayerName, message = chatReceived.Message, time = DateTimeOffset.Now.ToUnixTimeMilliseconds(), user = Enemy.PlayerName };
                if (chatReceived.PlayerId == Self.PlayerId)
                {
                    Console.WriteLine($"{frame} {FrameToTimeConverter.GetTime(frame)} my chat: {chatReceived.Message}");
                    ChatHistory.MyChatHistory[frame] = chatReceived.Message;
                    if (ApiEnabled)
                    {
                        UpdateChatAsync(chat);
                    }
                }
                else
                {
                    Console.WriteLine($"{frame} {FrameToTimeConverter.GetTime(frame)} enemy chat: {chatReceived.Message}");
                    ChatHistory.EnemyChatHistory[frame] = chatReceived.Message;
                    if (string.IsNullOrEmpty(ActiveChatData.EnemyName))
                    {
                        ActiveChatData.EnemyName = EnemyNameService.GetNameFromChat(chat.message, EnemyPlayerManager.Enemies);
                    }
                    GetChatResponseAsync(chat, frame);
                }
                //GetChatResponseAsync(chat, frame); // TODO: remove this, this is for testing
            }
        }

        private List<Action> PopChatActions()
        {
            var poppedActions = new List<Action>(ActiveChatData.ChatActions);
            ActiveChatData.ChatActions = new List<Action>();
            return poppedActions;
        }

        private Match MatchesTrigger(string lower, List<string> triggers)
        {
            Match match = null;
            foreach (var trigger in triggers)
            {
                match = Regex.Match(lower, trigger);
                if (match.Success)
                {
                    return match;
                }
            }
            return match;
        }

        private bool ChatResponseTimeReady(ChatData chatData, int frame)
        {
            if (chatData.LastResponseFrame == 0 || frame - chatData.LastResponseFrame > SharkyOptions.FramesPerSecond * chatData.Frequency)
            {
                if (!LastResponseTimes.ContainsKey(chatData.Type) || frame - LastResponseTimes[chatData.Type] > SharkyOptions.FramesPerSecond * ChatTypeFrequencies[chatData.Type])
                {
                    return true;
                }
            }
            return false;
        }

        private bool GetGameResponse(Chat.Chat chat, int frame)
        {
            var lower = " " + chat.message.ToLower() + " ";

            foreach (var chatData in ChatDataService.DefaultChataData)
            {
                if (ChatResponseTimeReady(chatData, frame))
                {
                    var matchData = MatchesTrigger(lower, chatData.Triggers);
                    if (matchData.Success)
                    {
                        chatData.LastResponseFrame = frame;
                        LastResponseTimes[chatData.Type] = frame;
                        var message = ChatDataService.GetChatMessage(chatData, matchData, ActiveChatData.EnemyName);
                        ChatService.SendChatMessages(message);
                        return true;
                    }
                }
            }

            return false;
        }

        private async void GetChatResponseAsync(Chat.Chat chat, int frame)
        {
            if (GetGameResponse(chat, frame))
            {
                return;
            }

            if (ApiEnabled)
            {
                var chatRequest = new ChatRequest { chat = chat, type = "starcraft", conversationName = ConversationName, requestTime = DateTime.Now };

                var jsonString = JsonConvert.SerializeObject(chatRequest);
                var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                try
                {
                    var response = await HttpClient.PutAsync("https://localhost:44311/api/chat", httpContent); // TODO: load url from configuration

                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var chatResponse = JsonConvert.DeserializeObject<ChatResponse>(jsonResponse);

                    if (chatResponse.confidence > .5)
                    {
                        ChatService.SendChatMessages(chatResponse.response);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private async void UpdateChatAsync(Chat.Chat chat)
        {
            var chatRequest = new ChatRequest { chat = chat, type = "starcraft", conversationName = ConversationName, requestTime = DateTime.Now };

            var jsonString = JsonConvert.SerializeObject(chatRequest);
            var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            try
            {
                var response = await HttpClient.PutAsync("https://localhost:44311/api/chatupdate", httpContent); // TODO: load url from configuration, have option to disable

                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {

            }
        }
    }
}
