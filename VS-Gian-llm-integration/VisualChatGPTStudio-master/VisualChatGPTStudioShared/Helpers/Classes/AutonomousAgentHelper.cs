using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows;
using Unakin.Utils;
using UnakinShared.DTO;
using UnakinShared.Helpers.Interfaces;
using ICSharpCode.AvalonEdit.Document;
using System.Linq;
using UnakinShared.Models;
using UnakinShared.Utils;
using UnakinShared.Enums;
using System.Threading.Tasks;
using OpenAI_API.Chat;
using Unakin;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using UnakinShared.Helpers.Base;
using System.Collections.ObjectModel;
using System.IO;

/*
namespace UnakinShared.Helpers.Classes
{
    internal class AutonomousAgentHelper : BaseChatHelper, IChatHelper
    {
        private bool isFirstUserInput = true;

        public void RefreshChat(OpenAIService llm)
        {
            ChatMaster = new ChatMaster();
            ChatDetails = new List<ChatDetail>();
            ChatItems = new List<ChatItemDTO>();
            ChatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
            ChatMaster.CreatedTime = DateTime.Now;
            ChatMaster.UpdatedTime = DateTime.Now;
            ChatMaster.ChatType = Constants.AUTONOMOUSAGENT_ID;
        }

        public override void AppendUserInput(OpenAIService llm, string prompt)
        {
            CommandText = TurboChatHelper.GetChatName(Constants.AUTONOMOUSAGENT_NAME);
            var chatItem = AddChatItem(AuthorEnum.Me, prompt, true, ChatItems.Count);

            // Check if it's the first user input
            if (isFirstUserInput)
            {
                chatItem.IsItemVisible = Visibility.Collapsed; // Assuming there's a visibility property you can control
                isFirstUserInput = false; // Update the flag so this condition only applies once
                chatItem.FirstTagVisiblity = Visibility.Collapsed;
            }
            else
            {
                chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                chatItem.SecondTagVisiblity = Visibility.Visible;
                chatItem.SecondTag = "Autonomous Agents";
            }

            ChatItems.Add(chatItem);
        }

        public override ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        {
            ChatItemDTO chat = base.AddChatItem(author, message, firstSegment, index, isDirty, imagePath);

            if (author == AuthorEnum.Me)
            {
                
                    chat.IsItemVisible = Visibility.Collapsed; // Assuming there's a visibility property you can control
                    isFirstUserInput = false; // Update the flag so this condition only applies once
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                
            }


            if (author == AuthorEnum.ChatGPT)
            {
                if (firstSegment)
                {
                    chat.FirstTag = Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                }
                else
                {
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
            }
            else if (author == AuthorEnum.ChatGPTCode)
            {
                if (firstSegment)
                {
                    chat.FirstTag = Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                }
                else
                {
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
            }
            chat.ActionButtonVisiblity = Visibility.Collapsed;
            return chat;
        }

        public async System.Threading.Tasks.Task<string> GetUnakinResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            var isComplete = false;
            AuthorEnum aurthor = AuthorEnum.ChatGPT;
            ChatItemDTO chatItem = null;
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            //System.Threading.Tasks.Task responseTask;
            AppendUserInput(OpenAIService.UNAKIN, prompt);

            DateTime lastReceived = DateTime.Now;
            ResponseBuilder = new StringBuilder();
            //Agent = agent;

            System.Threading.Tasks.Task responseTask;
            IsFirst = true;

            if (option.SingleResponse)
            {
                responseTask = Unakin.Utils.Unakin.CallWebSocketSingleAnswer(UnakinPackage.Instance.OptionsGeneral, prompt, HandleSingleContent, cancellationToken, "big-expensive");
                await CommonUtils.HasTimedOutAsync(responseTask, lastReceived);
                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments(ResponseBuilder.ToString());
                IsFirst = true;
                HandleSegmentsAsync(segments);
                isComplete = true;
            }
            else
            {
                void HandlestreamContent(string content)
                {
                    ResponseBuilder.Append(content);
                    LastReceived = DateTime.Now;

                    if (chatItem == null)
                    {
                        chatItem = AddChatItem(aurthor, string.Empty, true, ChatItems.Count);
                        ChatItems.Add(chatItem);
                        ChangeResponseBtnVisiblity();
                    }

                    var item = TurboChatHelper.SaprateCode(ChatItems, chatItem, aurthor, this);
                    chatItem = item.Item1;
                    aurthor = item.Item2;

                    chatItem.Message = chatItem.Message + content;
                    chatItem.UpdateDoccument();
                    chatItem.SecondTag = agent.Name;
                    chatItem.SecondTagVisiblity = Visibility.Visible;
                    //chatItem.ImageSource = "pack://application:,,,/Unakin;component/Resources/user.png";
                    RefreshChatItem();
                }
                responseTask = Unakin.Utils.Unakin.GetUnakinSingleChatResponseAsync(UnakinPackage.Instance.OptionsGeneral, ChatItems, HandlestreamContent, cancellationToken, "big-expensive");

                LastReceived = DateTime.Now;
                int initialWaitTime = 5, finalWaitTime = 3;
                if (inactivityTimeout == TimeSpan.FromSeconds(initialWaitTime))
                    inactivityTimeout = TimeSpan.FromSeconds(finalWaitTime);
                else
                    inactivityTimeout = TimeSpan.FromSeconds(initialWaitTime);
                while (!responseTask.IsCompleted)
                {
                    if (DateTime.Now - LastReceived > inactivityTimeout)
                        break;
                    await System.Threading.Tasks.Task.Delay(500);
                }
                isComplete = true;
            }

            RefreshChatItem();
            OnRequestComplete(true);

            while (isComplete == false)
            {
                await System.Threading.Tasks.Task.Delay(2000);
            }
            await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);

            return ResponseBuilder.ToString();
        }

        public async System.Threading.Tasks.Task<string> GetChatGPTResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            return await GetUnakinResponseAsync(prompt, agent, cancellationToken);
        }


        public async System.Threading.Tasks.Task SaveChatAsync()
        {
            if (ChatMaster == null)
                return;

            var detailsToUpdate = new List<ChatItemDTO>(ChatItems.Where(x => x.IsDirty == true).ToList());
            if (ChatDetails == null || ChatDetails == null || (detailsToUpdate.Count() == 0))
                return;

            var title = ChatItems.OrderBy(x => x.Index).First()?.Message;
            title = title.Truncate(40);
            if (string.IsNullOrEmpty(ChatMaster.Desc) || ChatMaster.Desc != title)
            {
                ChatMaster.Desc = title;
                ChatMaster.ChatType = Constants.AUTONOMOUSAGENT_ID;

                if (AppDatabase.database != null && ChatMaster.Id == 0)
                {
                    await AppDatabase.database.InsertAsync(ChatMaster);
                }
            }
            ChatMaster.UpdatedTime = DateTime.Now;
            if (AppDatabase.database != null)
            {
                await AppDatabase.database.UpdateAsync(ChatMaster);
            }
            List<ChatDetail> inserted = new List<ChatDetail>();
            List<ChatDetail> updated = new List<ChatDetail>();

            foreach (var item in ChatItems.Where(x => x.IsDirty == true))
            {
                var detail = ChatDetails.FirstOrDefault(x => x.Index == item.Index);
                var tag = item.ActionButtonTag.Split('|')[0];
                int aurthor = 0;
                if (tag == "R") aurthor = 1; else if (tag == "C") aurthor = 2;

                if (detail == null)
                {
                    detail = new ChatDetail();
                    detail.ChatMasterId = ChatMaster.Id;
                    detail.Index = item.Index;
                    detail.Aurthor = aurthor;
                    detail.IsFirstSegment = item.IsFirstSegment;
                    detail.Content = item.Message;
                    detail.Syntax = item.Syntax;
                    inserted.Add(detail);
                }
                else
                {
                    detail.Aurthor = aurthor;
                    detail.IsFirstSegment = item.IsFirstSegment;
                    detail.Content = item.Message;
                    detail.Syntax = item.Syntax;
                    updated.Add(detail);
                }
            }

            if (AppDatabase.database != null)
            {
                await AppDatabase.database.RunInTransactionAsync((SQLite.SQLiteConnection tran) =>
                {
                    if (inserted.Count > 0)
                        inserted.ForEach(added => tran.Insert(added));
                    if (updated.Count > 0)
                        updated.ForEach(update => tran.Update(update));
                });

                foreach (var item in ChatItems.Where(x => x.IsDirty == true))
                {
                    if (detailsToUpdate.Where(x => x.Index == item.Index && x.Message == item.Message).Any())
                        item.IsDirty = false;
                }
            }
        }
        public async System.Threading.Tasks.Task LoadChatAsync(OpenAIService llm, int index)
        {
            var detailList = await AppDatabase.database.Table<ChatDetail>().Where(x => x.ChatMasterId == index).OrderBy(x => x.Index).ToListAsync();

            ChatItems.Clear();
            ChatMaster = await AppDatabase.database.Table<ChatMaster>().Where(x => x.Id == index).FirstOrDefaultAsync();

            foreach (var item in detailList)
            {

                AuthorEnum aurthor = AuthorEnum.Me;
                if (item.Aurthor == 1) aurthor = AuthorEnum.ChatGPT;
                if (item.Aurthor == 2) aurthor = AuthorEnum.ChatGPTCode;
                var chatItem = AddChatItem(aurthor, item.Content, item.IsFirstSegment, item.Index, false);
                if (ChatItems.Count == 2)
                {
                    chatItem.IsItemVisible = Visibility.Collapsed;
                }
                ChatItems.Add(chatItem);
                RefreshChatItem();

                if (aurthor == AuthorEnum.Me)
                {
                    chatItem.FirstTagVisiblity = Visibility.Visible;
                    chatItem.SecondTagVisiblity = Visibility.Visible;
                    //item.Content = item.Content.TrimEnd('\n');

                    //var Commands = OptionPageGridCommands.Commands.Where(x => item.Content.Trim().StartsWith(new string(x.Value.Item2.Take(30).ToArray())));
                    //var commandType = Commands.First().Value;

                    chatItem.SecondTag = "Autonomous Agents";
                }
            }
        }

        private async System.Threading.Tasks.Task<string> GetResponseFromWebSocketAutonomousAgent(string prompt)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            StringBuilder responseBuilder = new StringBuilder();

            DateTime lastReceived = DateTime.Now; // Timestamp of the last received data

            void HandleContent(string content)
            {
                responseBuilder.Append(content);
                lastReceived = DateTime.Now; // Update the timestamp when new data is received
            }

            Task responseTask = Unakin.Utils.Unakin.CallWebSocket(UnakinPackage.Instance.OptionsGeneral, prompt, HandleContent, CancellationToken.None, "GPT4-LongContext");

            // Loop until the task is completed or inactivity timeout is reached
            while (!responseTask.IsCompleted)
            {
                if (DateTime.Now - lastReceived > inactivityTimeout)
                {
                    // If inactivity timeout is reached, consider the task complete
                    break;
                }

                await Task.Delay(500); // Wait for a short period before checking again
            }

            return responseBuilder.ToString();
        }
    }
}
*/

namespace UnakinShared.Helpers.Classes
{
    internal class AutonomousAgentHelper : BaseChatHelper, IChatHelper
    {
        public void RefreshChat(OpenAIService llm)
        {
            ChatMaster = new ChatMaster();
            ChatDetails = new List<ChatDetail>();
            ChatItems = new List<ChatItemDTO>();
            ChatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
            ChatMaster.CreatedTime = DateTime.Now;
            ChatMaster.UpdatedTime = DateTime.Now;
            ChatMaster.ChatType = Constants.AUTONOMOUSAGENT_ID;
        }

        public override void AppendUserInput(OpenAIService llm, string prompt)
        {
            CommandText = TurboChatHelper.GetChatName(Constants.AUTONOMOUSAGENT_NAME);
            var chatItem = (AddChatItem(AuthorEnum.Me, prompt, true, ChatItems.Count));
            chatItem.ActionButtonVisiblity = Visibility.Collapsed;
            chatItem.SecondTagVisiblity = Visibility.Visible;
            chatItem.SecondTag = CommandText;
            ChatItems.Add(chatItem);
        }
        public override ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        {
            ChatItemDTO chat = base.AddChatItem(author, message, firstSegment, index, isDirty, imagePath);

            if (author == AuthorEnum.ChatGPT)
            {
                if (firstSegment)
                {
                    chat.FirstTag = Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                }
                else
                {
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
            }
            else if (author == AuthorEnum.ChatGPTCode)
            {
                if (firstSegment)
                {
                    chat.FirstTag = Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                }
                else
                {
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
            }
            chat.ActionButtonVisiblity = Visibility.Collapsed;
            return chat;
        }

        public async Task<string> GetUnakinResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            var isComplete = false;
            AuthorEnum aurthor = AuthorEnum.ChatGPT;
            ChatItemDTO chatItem = null;
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            //System.Threading.Tasks.Task responseTask;
            AppendUserInput(OpenAIService.UNAKIN, prompt);

            DateTime lastReceived = DateTime.Now;
            ResponseBuilder = new StringBuilder();
            //Agent = agent;

            System.Threading.Tasks.Task responseTask;
            IsFirst = true;

            if (option.SingleResponse)
            {
                responseTask = Unakin.Utils.Unakin.CallWebSocket(UnakinPackage.Instance.OptionsGeneral, prompt, HandleSingleContent, cancellationToken, "Action Hub");
                await CommonUtils.HasTimedOutAsync(responseTask, lastReceived);
                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments(ResponseBuilder.ToString());
                IsFirst = true;
                HandleSegmentsAsync(segments);
                isComplete = true;
            }
            else
            {
                void HandlestreamContent(string content)
                {
                    ResponseBuilder.Append(content);
                    LastReceived = DateTime.Now;

                    if (chatItem == null)
                    {
                        chatItem = AddChatItem(aurthor, string.Empty, true, ChatItems.Count);
                        ChatItems.Add(chatItem);
                        ChangeResponseBtnVisiblity();
                    }

                    var item = TurboChatHelper.SaprateCode(ChatItems, chatItem, aurthor, this);
                    chatItem = item.Item1;
                    aurthor = item.Item2;

                    chatItem.Message = chatItem.Message + content;
                    chatItem.UpdateDoccument();
                    RefreshChatItem();
                }
                responseTask = Unakin.Utils.Unakin.GetUnakinChatResponseAsync(UnakinPackage.Instance.OptionsGeneral, ChatItems, HandlestreamContent, cancellationToken, "Chat");

                LastReceived = DateTime.Now;
                int initialWaitTime = 5, finalWaitTime = 2;
                if (inactivityTimeout == TimeSpan.FromSeconds(initialWaitTime))
                    inactivityTimeout = TimeSpan.FromSeconds(finalWaitTime);
                else
                    inactivityTimeout = TimeSpan.FromSeconds(initialWaitTime);
                while (!responseTask.IsCompleted)
                {
                    if (DateTime.Now - LastReceived > inactivityTimeout)
                        break;
                    await System.Threading.Tasks.Task.Delay(1000);
                }
                isComplete = true;
            }

            RefreshChatItem();
            OnRequestComplete(true);

            while (isComplete == false)
            {
                await Task.Delay(1000);
            }
            await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);

            return ResponseBuilder.ToString();
        }

        public async Task<string> GetChatGPTResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            string result;
            var options = Unakin.UnakinPackage.Instance.OptionsGeneral;
            StringBuilder responseBuilder = new StringBuilder();
            //Agent = agent;
            IsFirst = true;

            if (!options.SingleResponse)
            {
                AuthorEnum aurthor = AuthorEnum.ChatGPT;
                ChatItemDTO chatItem = null;
                chatItem = AddChatItem(aurthor, string.Empty, IsFirst, ChatItems.Count);
                chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                ChatItems.Add(chatItem);

                void HandlestreamContent(string content)
                {
                    responseBuilder.Append(content);
                    var item = TurboChatHelper.SaprateCode(ChatItems, chatItem, aurthor, this);
                    chatItem = item.Item1;
                    aurthor = item.Item2;

                    chatItem.Message = chatItem.Message + content;
                    chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                    chatItem.UpdateDoccument();
                    RefreshChatItem();
                }
                await ChatGPT.GetResponseAsync(options, "", prompt, options.StopSequences.Split(','), HandlestreamContent, cancellationToken);
                await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);
                return responseBuilder.ToString();
            }
            else
            {
                Chat.AppendUserInput(prompt);

                Task<string> task = Chat.GetResponseFromChatbotAsync();
                await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();

                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments((await task));
                HandleSegmentsAsync(segments);
                RefreshChatItem();
                await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);
                return await task;
            }
        }



        public async System.Threading.Tasks.Task SaveChatAsync()
        {
            if (ChatMaster == null)
                return;

            var detailsToUpdate = new List<ChatItemDTO>(ChatItems.Where(x => x.IsDirty == true).ToList());
            if (ChatDetails == null || ChatDetails == null || (detailsToUpdate.Count() == 0))
                return;

            var title = ChatItems.OrderBy(x => x.Index).First()?.Message;
            title = title.Truncate(40);
            if (string.IsNullOrEmpty(ChatMaster.Desc) || ChatMaster.Desc != title)
            {
                ChatMaster.Desc = title;
                ChatMaster.ChatType = Constants.AUTONOMOUSAGENT_ID;

                if (AppDatabase.database != null && ChatMaster.Id == 0)
                {
                    await AppDatabase.database.InsertAsync(ChatMaster);
                }
            }
            ChatMaster.UpdatedTime = DateTime.Now;
            if (AppDatabase.database != null)
            {
                await AppDatabase.database.UpdateAsync(ChatMaster);
            }
            List<ChatDetail> inserted = new List<ChatDetail>();
            List<ChatDetail> updated = new List<ChatDetail>();

            foreach (var item in ChatItems.Where(x => x.IsDirty == true))
            {
                var detail = ChatDetails.FirstOrDefault(x => x.Index == item.Index);
                var tag = item.ActionButtonTag.Split('|')[0];
                int aurthor = 0;
                if (tag == "R") aurthor = 1; else if (tag == "C") aurthor = 2;

                if (detail == null)
                {
                    detail = new ChatDetail();
                    detail.ChatMasterId = ChatMaster.Id;
                    detail.Index = item.Index;
                    detail.Aurthor = aurthor;
                    detail.IsFirstSegment = item.IsFirstSegment;
                    detail.Content = item.Message;
                    detail.Syntax = item.Syntax;
                    inserted.Add(detail);
                }
                else
                {
                    detail.Aurthor = aurthor;
                    detail.IsFirstSegment = item.IsFirstSegment;
                    detail.Content = item.Message;
                    detail.Syntax = item.Syntax;
                    updated.Add(detail);
                }
            }

            if (AppDatabase.database != null)
            {
                await AppDatabase.database.RunInTransactionAsync((SQLite.SQLiteConnection tran) =>
                {
                    if (inserted.Count > 0)
                        inserted.ForEach(added => tran.Insert(added));
                    if (updated.Count > 0)
                        updated.ForEach(update => tran.Update(update));
                });

                foreach (var item in ChatItems.Where(x => x.IsDirty == true))
                {
                    if (detailsToUpdate.Where(x => x.Index == item.Index && x.Message == item.Message).Any())
                        item.IsDirty = false;
                }
            }
        }
        public async System.Threading.Tasks.Task LoadChatAsync(OpenAIService llm, int index)
        {
            var detailList = await AppDatabase.database.Table<ChatDetail>().Where(x => x.ChatMasterId == index).OrderBy(x => x.Index).ToListAsync();

            ChatItems.Clear();
            ChatMaster = await AppDatabase.database.Table<ChatMaster>().Where(x => x.Id == index).FirstOrDefaultAsync();

            foreach (var item in detailList)
            {

                AuthorEnum aurthor = AuthorEnum.Me;
                if (item.Aurthor == 1) aurthor = AuthorEnum.ChatGPT;
                if (item.Aurthor == 2) aurthor = AuthorEnum.ChatGPTCode;
                var chatItem = AddChatItem(aurthor, item.Content, item.IsFirstSegment, item.Index, false);
                if (ChatItems.Count == 2)
                {
                    chatItem.IsItemVisible = Visibility.Collapsed;
                }
                ChatItems.Add(chatItem);
                RefreshChatItem();

                if (aurthor == AuthorEnum.Me)
                {
                    chatItem.FirstTagVisiblity = Visibility.Visible;
                    chatItem.SecondTagVisiblity = Visibility.Visible;
                    //item.Content = item.Content.TrimEnd('\n');

                    //var Commands = OptionPageGridCommands.Commands.Where(x => item.Content.Trim().StartsWith(new string(x.Value.Item2.Take(30).ToArray())));
                    //var commandType = Commands.First().Value;

                    chatItem.SecondTag = "Autonomous Agent";
                }
            }
        }
    }
}
