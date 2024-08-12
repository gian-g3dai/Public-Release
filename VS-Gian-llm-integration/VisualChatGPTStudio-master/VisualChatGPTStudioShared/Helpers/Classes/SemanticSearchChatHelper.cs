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
using Unakin.Options;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using UnakinShared.Helpers.Base;
using System.Collections.ObjectModel;
using System.IO;

namespace UnakinShared.Helpers.Classes
{
    internal class SemanticSearchChatHelper : BaseChatHelper, IChatHelper
    {
        private SemanticSearchServerHelper serverHelper;
        public void RefreshChat(OpenAIService llm)
        {
            ChatMaster = new ChatMaster();
            ChatDetails = new List<ChatDetail>();
            ChatItems = new List<ChatItemDTO>();
            ChatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
            ChatMaster.CreatedTime = DateTime.Now;
            ChatMaster.UpdatedTime = DateTime.Now;
            ChatMaster.ChatType = 3;
            serverHelper = new SemanticSearchServerHelper();
        }

        public override void AppendUserInput(OpenAIService llm, string prompt)
        {
            CommandText = TurboChatHelper.GetChatName(Unakin.Utils.Constants.SEMANTICSEARCH_NAME);
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

            return chat;
        }

        public async Task<string> GetUnakinResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            string substringToRemove = "//Search in Code:";
            if (prompt.StartsWith(substringToRemove))
            {
                prompt = prompt.Substring(substringToRemove.Length).TrimStart();
            }
            ResponseDTO responseDTO = null;
            AppendUserInput(OpenAIService.UNAKIN, prompt);
            ChatItems.Add(AddChatItem(AuthorEnum.ChatGPT, "", true, ChatItems.Count));
            RefreshChatItem();
            try
            {
                if (await this.serverHelper.ConnectAsync())
                {
                    if (CommonUtils.IsDirectoryChanged == true)
                    {
                        var files = Directory.GetFiles(CommonUtils.WorkingDir, "*.cs", SearchOption.AllDirectories).Select(x => Path.GetFullPath(x)).ToList();
                        if (await this.serverHelper.SendInitialFilesMessageAsync(CommonUtils.WorkingDir, files, cancellationToken))
                        {
                            CommonUtils.IsDirectoryChanged = false;
                        }
                    }
                    responseDTO = await this.serverHelper.SendSearchCodeBlocksMessageAsync(prompt, cancellationToken);
                }

                if (responseDTO != null && responseDTO.results != null)
                {
                    foreach (var res in responseDTO.results.hits)
                    {

                        var filePath = string.Empty;
                        if (res.full_path != null)
                        {
                            var path1 = res.full_path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                            filePath = CommonUtils.WorkingDir;
                            foreach (var str in path1)
                            {
                                filePath += "\\" + str;

                                if (str.Contains("."))
                                    break;
                            }
                        }
                        ChatItems.Add(AddChatItem(AuthorEnum.ChatGPT, string.Concat("File:", filePath), false, ChatItems.Count));
                        var item = AddChatItem(AuthorEnum.ChatGPTCode, res.body, false, ChatItems.Count);
                        item.Syntax = TextFormat.DetectCodeLanguage(res.body);
                        ChatItems.Add(item);
                        RefreshChatItem();
                    }
                    OnRequestComplete(false);
                    await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while sending request");
                UnakinLogger.HandleException(ex);
            }
            return string.Empty;
        }

        public async Task<string> GetChatGPTResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            return await GetUnakinResponseAsync(prompt, null, cancellationToken);
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
                ChatMaster.ChatType = 3;

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
                        //item.Content = item.Content.TrimEnd('\n');

                        //var Commands = OptionPageGridCommands.Commands.Where(x => item.Content.Trim().StartsWith(new string(x.Value.Item2.Take(30).ToArray())));
                        //var commandType = Commands.First().Value;

                        chatItem.SecondTag = "Search in Code";
                    }

                    if (item.IsFirstSegment && string.IsNullOrEmpty(item.Content))
                        chatItem.IsItemVisible = Visibility.Collapsed;
                }
                ChatItems.Add(chatItem);
                RefreshChatItem();
            }

            /*
            if (llm == OpenAIService.OpenAI)
            {
                ChatGPT.RegerateChat(UnakinPackage.Instance.OptionsGeneral, ChatItems, Chat);
            }
            */
        }
    }
}
