using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sharky.Managers
{
    public class ChatManager : SharkyManager
    {
        ChatHistory ChatHistory; // Dictionary<int, string> SelfChatHistory and EnemyChatHistory 
        List<SC2APIProtocol.Action> ChatActions;

        // TODO: load multiple json files for chat messages

        // TODO: special chats, ex: greetings, greetings specific to enemies, build specific cheese chat and generic cheese chat, if it exists use it, otherwise don't say anything, first check build specific cheese chat, if nothing, use generic cheese chat
        // dictionary, key is the type, value is responses list of list of strings for multiple line replies

        // general chat response files
        // triggers, responses list of list of strings for multiple line replies, type, frequency

        public override IEnumerable<SC2APIProtocol.Action> OnFrame(ResponseObservation observation)
        {
            ObserveChatAsync(observation.Chat, (int)observation.Observation.GameLoop);

            if (!Greeted && observation.Observation.GameLoop > 20)
            {
                if (string.IsNullOrEmpty(EnemyName))
                {
                    SendChatMessage($"gl hf much love!");
                }
                else
                {
                    SendChatMessage($"gl hf much love {EnemyName}!");
                }

                Greeted = true;
            }

            return PopChatActions();
        }

        private async void ObserveChatAsync(RepeatedField<ChatReceived> chatsReceived, int frame)
        {
            foreach (var chatReceived in chatsReceived)
            {
                var chat = new Chat { botName = Bot.PlayerName, message = chatReceived.Message, time = DateTimeOffset.Now.ToUnixTimeMilliseconds(), user = Enemy.PlayerName }; // TODO: may never have PlayerName in bot matches
                if (chatReceived.PlayerId == Bot.PlayerId)
                {
                    Console.WriteLine($"{frame} sharkbot chat: {chatReceived.Message}");
                    MyChatHistory[frame] = chatReceived.Message;
                    if (ApiEnabled)
                    {
                        UpdateChatAsync(chat);
                    }
                }
                else
                {
                    Console.WriteLine($"{frame} enemy chat: {chatReceived.Message}");
                    EnemyChatHistory[frame] = chatReceived.Message;
                    if (string.IsNullOrEmpty(EnemyName))
                    {
                        EnemyName = EnemyNameManager.GetNameFromChat(chat.message, EnemyBotManager.Enemies);
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

        private bool GetGameResponse(Chat chat, int frame)
        {
            var lower = " " + chat.message.ToLower() + " ";

            foreach (var chatData in DefaultChataData)
            {
                if (ChatResponseTimeReady(chatData, frame))
                {
                    var matchData = MatchesTrigger(lower, chatData.Triggers);
                    if (matchData.Success)
                    {
                        chatData.LastResponseFrame = frame;
                        LastResponseTimes[chatData.Type] = frame;
                        var message = GetChatMessage(chatData, matchData);
                        SendChatMessage(message);
                        return true;
                    }
                }
            }

            return false;
        }

        private string GetChatMessage(ChatData chatData, Match matchData)
        {
            var response = chatData.Responses[Random.Next(chatData.Responses.Count)];
            var name = EnemyName;
            if (string.IsNullOrEmpty(name))
            {
                name = "opponent";
            }
            response = response.Replace("{0}", name).Replace("{match}", matchData.Groups[1].Value);
            return response;
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
            if (chatData.LastResponseFrame == 0 || frame - chatData.LastResponseFrame > FramesPerSecond * chatData.Frequency)
            {
                if (!LastResponseTimes.ContainsKey(chatData.Type) || frame - LastResponseTimes[chatData.Type] > FramesPerSecond * ChatTypeFrequencies[chatData.Type])
                {
                    return true;
                }
            }
            return false;
        }

        private async void GetChatResponseAsync(Chat chat, int frame)
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

        private async void UpdateChatAsync(Chat chat)
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
