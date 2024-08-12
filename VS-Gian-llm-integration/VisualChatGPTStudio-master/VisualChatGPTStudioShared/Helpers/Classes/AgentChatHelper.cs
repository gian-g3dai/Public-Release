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
using UnakinShared.Models;
using OpenAI_API.Chat;
using UnakinShared.Utils;
using System.Threading.Tasks;
using System.Threading;
using Unakin;
using System.Linq;
using UnakinShared.Helpers.Base;
using System.Runtime.Remoting.Messaging;
using System.Reflection.Metadata;

namespace UnakinShared.Helpers.Classes
{
    internal class AgentChatHelper : BaseChatHelper,IChatHelper
    {
        private AgentDTO Agent { get; set; }
        public override ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        {

            ChatItemDTO chat = base.AddChatItem(author, message, firstSegment, index, isDirty, imagePath);
            chat.ActionButtonVisiblity = Visibility.Collapsed;
            if (Agent != null)
                chat.AgentSequence = Agent.Sequence;
            if (author == AuthorEnum.ChatGPT)
            {
                if (firstSegment)
                {
                    chat.FirstTag = Agent.Name;
                    chat.SecondTag = Agent.Sequence.ToString();
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Visible;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                    chat.CreationTime = DateTime.Now;
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
                    chat.FirstTag = Agent.Name;
                    chat.SecondTag = Agent.Sequence.ToString();
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Visible;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                }
                else
                {
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
            }

            return chat;
        }
        public void RefreshChat(OpenAIService llm)
        {
            ChatMaster = new ChatMaster();
            ChatDetails = new List<ChatDetail>();
            ChatItems = new List<ChatItemDTO>();
            if (llm == OpenAIService.OpenAI)
            {
                Chat = ChatGPT.CreateConversation(UnakinPackage.Instance.OptionsGeneral);
            }

            ChatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
            ChatMaster.CreatedTime = DateTime.Now;
            ChatMaster.UpdatedTime = DateTime.Now;
            ChatMaster.ChatType = 2;
        }
        public async Task<string> GetUnakinResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            var isComplete = false;
            AuthorEnum aurthor = AuthorEnum.ChatGPT;
            ChatItemDTO chatItem = null;
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            //System.Threading.Tasks.Task responseTask;
            //AppendUserInput(OpenAIService.UNAKIN, prompt);

            DateTime lastReceived = DateTime.Now;
            ResponseBuilder = new StringBuilder();
            Agent = agent;

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

                    if (aurthor == AuthorEnum.ChatGPT || aurthor == AuthorEnum.ChatGPTCode)
                    {
                        chatItem.Message = chatItem.Message + content;
                        chatItem.UpdateDoccument();
                        RefreshChatItem();
                    }
                }
                responseTask = Unakin.Utils.Unakin.CallWebSocket(UnakinPackage.Instance.OptionsGeneral, prompt, HandlestreamContent, cancellationToken, "Chat");

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
                    await System.Threading.Tasks.Task.Delay(500);
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
            Agent = agent;
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
                    var item = TurboChatHelper.SaprateCode(ChatItems, chatItem, aurthor,this);
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
                ChatMaster.ChatType = 2;

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
                    detail.Image = item.ImageSource;
                    detail.Sequence = item.AgentSequence;
                    inserted.Add(detail);
                }
                else
                {
                    detail.Aurthor = aurthor;
                    detail.IsFirstSegment = item.IsFirstSegment;
                    detail.Content = item.Message;
                    detail.Syntax = item.Syntax;
                    detail.Image = item.ImageSource;
                    detail.Sequence = item.AgentSequence;
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
        public async Task LoadChatAsync(OpenAIService llm, int index)
        {
            var detailList = await AppDatabase.database.Table<ChatDetail>().Where(x => x.ChatMasterId == index).OrderBy(x => x.Index).ToListAsync();

            ChatItems.Clear();
            ChatMaster = await AppDatabase.database.Table<ChatMaster>().Where(x => x.Id == index).FirstOrDefaultAsync();
            foreach (var item in detailList)
            {
                AuthorEnum aurthor = AuthorEnum.Me;
                if (item.Aurthor == 1) aurthor = AuthorEnum.ChatGPT;
                if (item.Aurthor == 2) aurthor = AuthorEnum.ChatGPTCode;
                if (!string.IsNullOrEmpty(item.Syntax) && item.Aurthor == 1) aurthor = AuthorEnum.ChatGPTCode;
                ChatItemDTO chat = base.AddChatItem(aurthor, item.Content, item.IsFirstSegment, item.Index, item.IsFirstSegment, item.Image);
                if (ChatItems.Count == 0)
                {
                    chat.IsFirstSegment = true;
                    chat.FirstTag = Constants.SELF_COMMENT;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
                if (item.Sequence > 0 && item.IsFirstSegment)
                {
                    chat.IsFirstSegment = true;
                    chat.FirstTag = Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTag = item.Sequence.ToString();
                    chat.SecondTagVisiblity = Visibility.Visible;
                    chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_UNAKIN_B);
                    chat.IsItemVisible = Visibility.Collapsed;
                    chat.IsHeaderVisible = Visibility.Visible;
                }
                

                ChatItems.Add(chat);
                RefreshChatItem();
                ChangeResponseBtnVisiblity();
            }
            if (llm == OpenAIService.OpenAI)
            {
                ChatGPT.RegerateChat(UnakinPackage.Instance.OptionsGeneral, ChatItems, Chat);
            }
        }
        public override void HandleSegmentsAsync(List<ChatTurboResponseSegment> segments)
        {
            foreach (var segment in segments)
            {
                AuthorEnum author = segment.IsCode ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;
                var chatItem = AddChatItem(author, segment.Content, IsFirst, ChatItems.Count);
                ChatItems.Add(chatItem);
                IsFirst = false;
            }
            RefreshChatItem();
            OnRequestComplete(true);
        }
    }
}
