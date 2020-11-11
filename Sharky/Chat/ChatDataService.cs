using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
            var chatFolder = Directory.GetCurrentDirectory() + "/data/chat/type";
            if (Directory.Exists(chatFolder))
            {
                foreach (var fileName in Directory.GetFiles(chatFolder))
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
            var chatFolder = Directory.GetCurrentDirectory() + "/data/chat/default";
            if (Directory.Exists(chatFolder))
            {
                foreach (var fileName in Directory.GetFiles(chatFolder))
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

        public List<string> GetChatTypeMessage(ChatTypeData chatTypeData, string enemyName)
        {
            var responses = chatTypeData.Messages[Random.Next(chatTypeData.Messages.Count)];
            var name = enemyName;
            if (string.IsNullOrEmpty(name))
            {
                name = "opponent";
            }
            responses.ForEach(r => r.Replace("{name}", name));
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
                chatType = chatType.Substring(chatType.LastIndexOf('-') + 1);
                if (ChatTypeData.ContainsKey(chatType))
                {
                    return ChatTypeData[chatType];
                }
            }

            return null;
        }

        public string GetChatMessage(ChatData chatData, Match matchData, string enemyName)
        {
            var response = chatData.Responses[Random.Next(chatData.Responses.Count)];
            var name = enemyName;
            if (string.IsNullOrEmpty(name))
            {
                name = "opponent";
            }
            response = response.Replace("{0}", name).Replace("{match}", matchData.Groups[1].Value);
            return response;
        }
    }
}
