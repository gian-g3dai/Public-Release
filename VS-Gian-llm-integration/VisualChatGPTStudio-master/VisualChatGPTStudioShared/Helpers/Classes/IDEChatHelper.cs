﻿using System;
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
using Unakin.Options;
using UnakinShared.Enums;
using System.Threading.Tasks;
using OpenAI_API.Chat;
using Unakin;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using UnakinShared.Helpers.Base;
using System.Windows.Threading;
using EnvDTE;

namespace UnakinShared.Helpers.Classes
{
    internal class IDEChatHelper : BaseChatHelper, IChatHelper
    {
        public override void AppendUserInput(OpenAIService llm, string prompt)
        {
            if (prompt.Contains("```"))
            {
                var segments = prompt.Split(new[] { "```" }, StringSplitOptions.None);
                bool isCode = false;
                foreach (var segment in segments)
                {
                    ChatItemDTO chatItem;
                    if (isCode == false)
                    {
                        CommandText = TurboChatHelper.GetChatName(segment);
                        chatItem = (AddChatItem(AuthorEnum.Me, segment, true, ChatItems.Count));
                        chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                        chatItem.SecondTagVisiblity = Visibility.Visible;
                        chatItem.SecondTag = CommandText;
                        isCode = true;
                        
                    }
                    
                    else
                    {
             
                        var code = segment.Replace("```", string.Empty);
                        chatItem = (AddChatItem(AuthorEnum.ChatGPTCode, code, true, ChatItems.Count));
                        //chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                        chatItem.FirstTagVisiblity = Visibility.Collapsed;
                        chatItem.SecondTagVisiblity = Visibility.Collapsed;
                        chatItem.ActionButtonTag = "P|" + ChatItems.Count.ToString();
                        
                    }
                    ChatItems.Add(chatItem);
                    
                    
                }
            }
            else
            {
                ChatItems.Add(AddChatItem(AuthorEnum.Me, prompt, true, ChatItems.Count));
            }

            if (llm == OpenAIService.OpenAI)
            {
                var options = UnakinPackage.Instance.OptionsGeneral;
                CommandText = "Command";
                //string request = options.MinifyRequests ? TextFormat.MinifyText(prompt) : prompt;
                //request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));
                Chat.AppendUserInput(prompt);
            }
        }
        public override ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        {
            ChatItemDTO chat = base.AddChatItem(author, message, firstSegment, index, isDirty, imagePath);
            chat.ActionButtonVisiblity = Visibility.Collapsed;
            if (author == AuthorEnum.ChatGPT)
            {
                if (firstSegment)
                {
                    chat.FirstTag = Unakin.Utils.Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                    chat.TagColor = new SolidColorBrush(Unakin.Utils.Constants.TAG_COLOR_UNAKIN_B);
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
                    chat.FirstTag = Unakin.Utils.Constants.UNAKIN_COMMENT_FIRST;
                    chat.FirstTagVisiblity = Visibility.Visible;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                    chat.TagColor = new SolidColorBrush(Unakin.Utils.Constants.TAG_COLOR_UNAKIN_B);
                }
                else
                {
                    chat.FirstTagVisiblity = Visibility.Collapsed;
                    chat.SecondTagVisiblity = Visibility.Collapsed;
                }
            }
            /*
            else if (author == AuthorEnum.Me)
            {
                chat.FirstTag = Constants.SELF_COMMENT;
                chat.SecondTag = "Command";
                chat.FirstTagVisiblity = Visibility.Visible;
                chat.SecondTagVisiblity = Visibility.Visible;
                chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_ME_B);
            }
            */
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
            ChatMaster.ChatType = 1;
        }

        public async Task<string> GetUnakinResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5);
            var isComplete = false;
            AuthorEnum aurthor = AuthorEnum.ChatGPT;
            ChatItemDTO chatItem = null;
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            System.Threading.Tasks.Task responseTask;
            AppendUserInput(OpenAIService.UNAKIN, prompt);

            ResponseBuilder = new StringBuilder();
            if (option.SingleResponse)
            {
                responseTask = Unakin.Utils.Unakin.GetUnakinChatResponseAsync(UnakinPackage.Instance.OptionsGeneral, ChatItems, HandleSingleContent, cancellationToken, "Chat");
                await CommonUtils.HasTimedOutAsync(responseTask, LastReceived, 5);
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

                    if (chatItem == null)
                    {
                        chatItem = AddChatItem(aurthor, string.Empty, true, ChatItems.Count);
                        ChatItems.Add(chatItem);
                    }

                    var item = TurboChatHelper.SaprateCode(ChatItems, chatItem, aurthor, this);
                    chatItem = item.Item1;
                    aurthor = item.Item2;
                    chatItem.ActionButtonVisiblity = Visibility.Collapsed;

                    chatItem.Message = chatItem.Message + content;
                    chatItem.UpdateDoccument();
                    RefreshChatItem();
                }
                responseTask = Unakin.Utils.Unakin.GetUnakinChatResponseAsync(UnakinPackage.Instance.OptionsGeneral, ChatItems, HandlestreamContent, cancellationToken, "Chat");

                LastReceived = DateTime.Now;
                int initialWaitTime = 8, finalWaitTime = 2;
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
            while(isComplete== false)
            {
                await Task.Delay(1000);
            }
            await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);
            return ResponseBuilder.ToString();
        }

        public async Task<string> GetChatGPTResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            StringBuilder responseBuilder = new StringBuilder();

            StringBuilder contextBuilder = new StringBuilder();
            foreach (var item in ChatItems)
            {
                contextBuilder.AppendLine(item.Message);
            }
            // Append the current prompt to the context
            string fullPrompt = contextBuilder.ToString() + prompt;

            AppendUserInput(OpenAIService.OpenAI, prompt);
            RefreshChatItem();

            if (option.SingleResponse)
            {
                Task<string> task = Chat.GetResponseFromChatbotAsync();
                await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();

                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments((await task));
                IsFirst = true;
                HandleSegmentsAsync(segments);

                return await task;
            }
            else
            {
                AuthorEnum aurthor = AuthorEnum.ChatGPT;
                ChatItemDTO chatItem = null;
                ChatItemDTO chatItem_temp = null;
                chatItem = AddChatItem(aurthor, string.Empty, true, ChatItems.Count);
                ChatItems.Add(chatItem);
                ChangeResponseBtnVisiblity();

                void HandlestreamContent(string content)
                {
                    responseBuilder.Append(content);

                    chatItem_temp = chatItem;
                    chatItem_temp.Message = chatItem_temp.Message + content;

                    var item = TurboChatHelper.SaprateCode(ChatItems, chatItem_temp, aurthor, this);
                    chatItem = item.Item1;
                    aurthor = item.Item2;

                    //chatItem.Message = chatItem.Message + content;
                    chatItem.UpdateDoccument();
                    RefreshChatItem();
                }
                await ChatGPT.GetResponseAsync(option, "", fullPrompt, option.StopSequences.Split(','), HandlestreamContent, cancellationToken);
                OnRequestComplete(true);
                await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);
                return responseBuilder.ToString();
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
            //title = title.Truncate(40);
            title = title.Replace("\n", "");

            if (string.IsNullOrEmpty(ChatMaster.Desc) || ChatMaster.Desc != title)
            {
                ChatMaster.Desc = title;
                ChatMaster.ChatType = TurboChatHelper.GetChatNum(title);

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
                //if (string.IsNullOrEmpty(item.Message))
                //{
                //    continue;
                //}
                    
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
                ChatItemDTO chatItem;
                if (ChatItems.Count == 1)
                {
                    chatItem = AddChatItem(AuthorEnum.ChatGPTCode, item.Content, item.IsFirstSegment, item.Index, false);
                    chatItem.Syntax = item.Syntax;
                    chatItem.IsHeaderVisible = Visibility.Collapsed;
                    //chatItem.ActionButtonVisiblity = Visibility.Collapsed;


                }
                else
                {
                    AuthorEnum aurthor = AuthorEnum.Me;
                    if (item.Aurthor == 1) aurthor = AuthorEnum.ChatGPT;
                    if (item.Aurthor == 2) aurthor = AuthorEnum.ChatGPTCode;

                    chatItem = AddChatItem(aurthor, item.Content, item.IsFirstSegment, item.Index, false);

                    if (aurthor == AuthorEnum.Me && ChatItems.Count == 0)
                    { 
                        chatItem.FirstTagVisiblity = Visibility.Visible;
                        chatItem.SecondTagVisiblity = Visibility.Visible;
                        item.Content = item.Content.TrimEnd('\n');

                        var Commands = OptionPageGridCommands.Commands.Where(x => item.Content.Trim().StartsWith(new string(x.Value.Item2.Take(30).ToArray())));
                        var commandType = Commands.First().Value;
                     
                        chatItem.SecondTag = commandType.Item1;
                    }

                    if (item.IsFirstSegment && string.IsNullOrEmpty(item.Content))
                        chatItem.IsItemVisible = Visibility.Collapsed;
                }
                ChatItems.Add(chatItem);
                RefreshChatItem();
            }
            if (llm == OpenAIService.OpenAI)
            {
                ChatGPT.RegerateChat(UnakinPackage.Instance.OptionsGeneral, ChatItems, Chat);
            }
        }
    }
}
