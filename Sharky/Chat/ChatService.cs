﻿using SC2APIProtocol;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sharky.Chat
{
    public class ChatService
    {
        IChatDataService ChatDataService;
        SharkyOptions SharkyOptions;
        ActiveChatData ActiveChatData;

        public ChatService(IChatDataService chatDataService, SharkyOptions sharkyOptions, ActiveChatData activeChatData)
        {
            ChatDataService = chatDataService;
            SharkyOptions = sharkyOptions;
            ActiveChatData = activeChatData;
        }

        public async void SendChatType(string chatType, bool instant = false)
        {
            var data = ChatDataService.GetChatTypeData(chatType);

            if (data != null)
            {
                var messages = ChatDataService.GetChatTypeMessage(data, ActiveChatData.EnemyName);
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

        public async void SendAllyChatMessage(string message, bool instant = false)
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
            foreach (var message in messages)
            {
                var chatAction = new Action { ActionChat = new ActionChat { Message = message } };
                if (teamChannel)
                {
                    chatAction.ActionChat.Channel = ActionChat.Types.Channel.Team;
                }

                typeTime += message.Length * 80; // simulate typing at 80 ms per keystroke
                // translate framerate to real
                await Task.Delay((int)(typeTime / ActiveChatData.TimeModulation)).ContinueWith((task) => { ActiveChatData.ChatActions.Add(chatAction); });
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
                    await Task.Delay((int)(typeTime / ActiveChatData.TimeModulation)).ContinueWith((task) => { ActiveChatData.ChatActions.Add(chatAction); });
                }
            }
        }

        void SendInstantChatMessage(string message, bool teamChannel = false)
        {
            var chatAction = new Action { ActionChat = new ActionChat { Message = message } };
            if (teamChannel)
            {
                chatAction.ActionChat.Channel = ActionChat.Types.Channel.Team;
            }
            ActiveChatData.ChatActions.Add(chatAction);
        }
    }
}
