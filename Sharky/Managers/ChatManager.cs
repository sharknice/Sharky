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
using System.Threading.Tasks;
using Action = SC2APIProtocol.Action;

namespace Sharky.Managers
{
    public class ChatManager : SharkyManager, IChatManager
    {
        HttpClient HttpClient;
        ChatHistory ChatHistory;
        SharkyOptions SharkyOptions;
        IChatDataService ChatDataService;
        IEnemyPlayerService EnemyPlayerManager;
        IEnemyNameService EnemyNameService;

        List<Action> ChatActions;

        Dictionary<TypeEnum, int> LastResponseTimes;
        Dictionary<TypeEnum, int> ChatTypeFrequencies;

        string ConversationName;
        private PlayerInfo Self;
        private PlayerInfo Enemy;
        string EnemyName;
        bool GreetingSent;

        double TimeModulation;
        long LastFrameTime;
        double RuntimeFrameRate;

        bool ApiEnabled;

        public ChatManager(HttpClient httpClient, ChatHistory chatHistory, SharkyOptions sharkyOptions, IChatDataService chatDataService, IEnemyPlayerService enemyPlayerManager, IEnemyNameService enemyNameService)
        {
            HttpClient = httpClient;
            ChatHistory = chatHistory;
            SharkyOptions = sharkyOptions;
            ChatDataService = chatDataService;
            EnemyPlayerManager = enemyPlayerManager;
            EnemyNameService = enemyNameService;

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
            TimeModulation = 1;
            LastFrameTime = 0;
            RuntimeFrameRate = SharkyOptions.FramesPerSecond;

            GreetingSent = false;

            ChatActions = new List<Action>();

            Self = gameInfo.PlayerInfo[(int)observation.Observation.PlayerCommon.PlayerId - 1];
            Enemy = gameInfo.PlayerInfo[2 - (int)observation.Observation.PlayerCommon.PlayerId];

            ConversationName = $"starcraft-{Enemy.PlayerId}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            EnemyName = EnemyNameService.GetEnemyNameFromId(opponentId, EnemyPlayerManager.Enemies);

            ChatHistory.EnemyChatHistory = new Dictionary<int, string>();
            ChatHistory.MyChatHistory = new Dictionary<int, string>();
        }

        public override IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            UpdateTimeModulation();
            ObserveChatAsync(observation.Chat, (int)observation.Observation.GameLoop);

            if (!GreetingSent && observation.Observation.GameLoop > 20)
            {
                SendChatType($"{EnemyName}-Greeting");
                GreetingSent = true;
            }

            return PopChatActions();
        }

        void UpdateTimeModulation()
        {
            if (LastFrameTime != 0)
            {
                RuntimeFrameRate = 1000.0 / (DateTimeOffset.Now.ToUnixTimeMilliseconds() - LastFrameTime);
                TimeModulation = RuntimeFrameRate / SharkyOptions.FramesPerSecond;
            }
            LastFrameTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public async void SendChatType(string chatType, bool instant = false)
        {
            var data = ChatDataService.GetChatTypeData(chatType);

            if (data != null)
            {
                var messages = ChatDataService.GetChatTypeMessage(data, EnemyName);
                SendChatMessages(messages);
            }
        }

        public async void SendChatMessage(string message, bool instant = false)
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

        public async void SendChatMessages(IEnumerable<string> messages)
        {
            var typeTime = 0;
            foreach (var message in messages)
            {
                var chatAction = new Action { ActionChat = new ActionChat { Message = message } };

                typeTime += message.Length * 80; // simulate typing at 80 ms per keystroke
                // translate framerate to real
                await Task.Delay((int)(typeTime / TimeModulation)).ContinueWith((task) => { ChatActions.Add(chatAction); });
            }
        }

        public async void SendDebugChatMessage(string message)
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
                    var chatAction = new Action { ActionChat = new ActionChat { Message = message, Channel = ActionChat.Types.Channel.Team } };

                    typeTime += message.Length * 80; // simulate typing at 80 ms per keystroke
                                                     // translate framerate to real
                    await Task.Delay((int)(typeTime / TimeModulation)).ContinueWith((task) => { ChatActions.Add(chatAction); });
                }
            }
        }

        void SendInstantChatMessage(string message)
        {
            var chatAction = new Action { ActionChat = new ActionChat { Message = message } };
            ChatActions.Add(chatAction);
        }

        private async void ObserveChatAsync(RepeatedField<ChatReceived> chatsReceived, int frame)
        {
            foreach (var chatReceived in chatsReceived)
            {
                var chat = new Chat.Chat { botName = Self.PlayerName, message = chatReceived.Message, time = DateTimeOffset.Now.ToUnixTimeMilliseconds(), user = Enemy.PlayerName };
                if (chatReceived.PlayerId == Self.PlayerId)
                {
                    Console.WriteLine($"{frame} sharkbot chat: {chatReceived.Message}");
                    ChatHistory.MyChatHistory[frame] = chatReceived.Message;
                    if (ApiEnabled)
                    {
                        UpdateChatAsync(chat);
                    }
                }
                else
                {
                    Console.WriteLine($"{frame} enemy chat: {chatReceived.Message}");
                    ChatHistory.EnemyChatHistory[frame] = chatReceived.Message;
                    if (string.IsNullOrEmpty(EnemyName))
                    {
                        EnemyName = EnemyNameService.GetNameFromChat(chat.message, EnemyPlayerManager.Enemies);
                    }
                    GetChatResponseAsync(chat, frame);
                }
                //GetChatResponseAsync(chat, frame); // TODO: remove this, this is for testing
            }
        }

        private List<Action> PopChatActions()
        {
            var poppedActions = new List<Action>(ChatActions);
            ChatActions = new List<Action>();
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
                        var message = ChatDataService.GetChatMessage(chatData, matchData, EnemyName);
                        SendChatMessages(message);
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
                        SendChatMessages(chatResponse.response);
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
