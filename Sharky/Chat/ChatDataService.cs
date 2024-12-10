namespace Sharky.Chat
{
    public class ChatDataService : IChatDataService
    {
        Random Random;

        public List<ChatData> DefaultChataData { get; private set; }
        Dictionary<string, ChatTypeData> ChatTypeData;

        public ChatDataService()
        {
            Random = new Random();

            DefaultChataData = LoadDefaultChatData();
            ChatTypeData = LoadChatTypeData();
        }

        Dictionary<string, ChatTypeData> LoadChatTypeData()
        {
            var data = new Dictionary<string, ChatTypeData>();
            var chatFolder = Directory.GetCurrentDirectory() + "/StaticData/chat/type";
            if (Directory.Exists(chatFolder))
            {
                foreach (var fileName in Directory.GetFiles(chatFolder).Where(f => f.EndsWith(".json")))
                {
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        var fileData = JsonConvert.DeserializeObject<List<ChatTypeData>>(file.ReadToEnd());
                        foreach (var chat in fileData)
                        {
                            data[chat.ChatType] = chat;
                        }
                    }
                }
            }
            return data;
        }

        List<ChatData> LoadDefaultChatData()
        {
            var chatData = new List<ChatData>();
            var chatFolder = Directory.GetCurrentDirectory() + "/StaticData/chat/default";
            if (Directory.Exists(chatFolder))
            {
                foreach (var fileName in Directory.GetFiles(chatFolder).Where(f => f.EndsWith(".json")))
                {
                    using (StreamReader file = File.OpenText(fileName))
                    {
                        var data = ChatData.FromJson(file.ReadToEnd());
                        chatData.AddRange(data);
                    }
                }
            }

            for (int index = 0; index < chatData.Count; index++)
            {
                chatData[index].LastResponseFrame = 0;
            }
            return chatData;
        }

        public List<string> GetChatTypeMessage(ChatTypeData chatTypeData, string enemyName, List<KeyValuePair<string, string>> parameters = null)
        {
            var responses = chatTypeData.Messages[Random.Next(chatTypeData.Messages.Count)];
            var name = enemyName;
            if (string.IsNullOrEmpty(name))
            {
                name = "opponent";
            }
            for (var index = 0; index < responses.Count; index++)
            {
                responses[index] = responses[index].Replace("{name}", name);

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        responses[index] = responses[index].Replace("{" + parameter.Key + "}", parameter.Value);
                    }
                }
            }
            return responses;
        }

        public ChatTypeData GetChatTypeData(string chatType)
        {
            if (ChatTypeData.ContainsKey(chatType))
            {
                return ChatTypeData[chatType];
            }

            while (chatType.Contains("-"))
            {
                chatType = chatType.Substring(chatType.IndexOf('-') + 1);
                if (ChatTypeData.ContainsKey(chatType))
                {
                    return ChatTypeData[chatType];
                }
            }

            return null;
        }

        public List<string> GetChatMessage(ChatData chatData, Match matchData, string enemyName)
        {
            var responses = chatData.Responses[Random.Next(chatData.Responses.Count)];
            var name = enemyName;
            if (string.IsNullOrEmpty(name))
            {
                name = "opponent";
            }
            for (var index = 0; index < responses.Count; index++)
            {
                responses[index] = responses[index].Replace("{name}", name).Replace("{match}", matchData.Groups[1].Value);
            }
            return responses;
        }
    }
}
