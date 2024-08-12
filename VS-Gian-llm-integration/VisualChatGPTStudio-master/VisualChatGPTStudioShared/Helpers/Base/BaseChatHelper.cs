using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Unakin;
using Unakin.Utils;
using UnakinShared.DTO;
using UnakinShared.Models;
using UnakinShared.Utils;
using ICSharpCode.AvalonEdit.Document;
using System.Linq;

namespace UnakinShared.Helpers.Base
{
    internal class BaseChatHelper
    {
        public ChatMaster ChatMaster { get; set; }
        public List<ChatDetail> ChatDetails { get; set; }
        public List<ChatItemDTO> ChatItems { get; set; }
        public Action<Boolean> OnRequestComplete { get; set; }
        public Action RefreshChatItem { get; set; }
        public Conversation Chat { get; set; }
        public string CommandText { get; set; }
        public bool IsFirst { get; set; } = false;
        public DateTime LastReceived { get; set; }
        public StringBuilder ResponseBuilder { get; set; }

        public virtual void AppendUserInput(OpenAIService llm, string prompt)
        {

            ChatItems.Add(AddChatItem(AuthorEnum.Me, prompt, true, ChatItems.Count));

            if (llm == OpenAIService.OpenAI)
            {
                var options = UnakinPackage.Instance.OptionsGeneral;
                //string request = options.MinifyRequests ? TextFormat.MinifyText(prompt) : prompt;
                //request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));
                Chat.AppendUserInput(prompt);
            }
        }

        public virtual ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        {
            ChatItemDTO chat = new ChatItemDTO();

            if (author == AuthorEnum.ChatGPTCode)
            {
                if (firstSegment==false)
               message = message.TrimStart().TrimEnd();
            }
            chat.Document = new TextDocument(message);
            chat.Message = message;
            chat.ChatType = 1;
            chat.IsDirty = isDirty;
            chat.Index = index;

            if (author == AuthorEnum.Me)
            {
                chat.BackgroundColor = new SolidColorBrush(Constants.CHAT_COLOR_B);
                chat.Syntax = string.Empty;
                chat.Margins = new Thickness(0, 0, 0, 0);

                chat.ButtonCopyVisibility = Visibility.Collapsed;
                chat.ButtonInsertVisibility = Visibility.Collapsed;
                chat.ButtonZoomVisibility = Visibility.Collapsed;

                chat.ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
                chat.ActionImageSource = "pack://application:,,,/Unakin;component/Resources/edit.png";
                chat.ActionButtonVisiblity = Visibility.Visible;
                chat.ActionButtonTooltip = "Edit Prompt";
                chat.ActionButtonTag = "P|" + index.ToString();
                chat.IsFirstSegment = true;

                chat.FirstTag = Constants.SELF_COMMENT;
                chat.FirstTagVisiblity = Visibility.Visible;
                chat.SecondTagVisiblity = Visibility.Collapsed;
                chat.TagColor = new SolidColorBrush(Constants.TAG_COLOR_ME_B);

            }
            else if (author == AuthorEnum.ChatGPT)
            {
                chat.BackgroundColor = new SolidColorBrush(Constants.CHAT_COLOR_B);
                chat.Syntax = string.Empty;
                chat.Margins = new Thickness(0, 0, 0, 0);

                chat.ButtonCopyVisibility = Visibility.Collapsed;
                chat.ButtonInsertVisibility = Visibility.Collapsed;
                chat.ButtonZoomVisibility = Visibility.Collapsed;

                chat.ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
                chat.ActionImageSource = "pack://application:,,,/Unakin;component/Resources/refresh.png";
                chat.ActionButtonVisiblity = Visibility.Collapsed;
                chat.ActionButtonTooltip = "Refresh Response";
                chat.ActionButtonTag = "R|" + index.ToString();
                chat.IsFirstSegment = firstSegment;
            }
            else if (author == AuthorEnum.ChatGPTCode)
            {
                chat.BackgroundColor = new SolidColorBrush(Constants.CODE_COLOR_B);
                chat.Syntax = TextFormat.DetectCodeLanguage(message);
                chat.Margins = new Thickness(0, 0, 0, 0);

                chat.ButtonCopyVisibility = Visibility.Visible;
                chat.ButtonInsertVisibility = Visibility.Visible;
                chat.ButtonZoomVisibility = Visibility.Visible;

                chat.ShowHorizontalScrollBar = ScrollBarVisibility.Auto;
                chat.ActionImageSource = "pack://application:,,,/Unakin;component/Resources/refresh.png";
                chat.ActionButtonVisiblity = Visibility.Collapsed;
                chat.ActionButtonTag = "C|" + index.ToString();
                chat.IsFirstSegment = firstSegment;
            }

            if (firstSegment)
                chat.IsHeaderVisible = Visibility.Visible;  

            return chat;
        }
        public async System.Threading.Tasks.Task DeleteChatAsync(int index)
        {
            if (AppDatabase.database != null)
            {
                var qryDt = String.Concat("DELETE FROM ChatDetail WHERE ChatMasterId = ", ChatMaster.Id.ToString(), " AND [Index] >= ", index.ToString());
                await AppDatabase.database.QueryAsync<ChatDetail>(qryDt);
            }
        }
        public virtual void ChangeResponseBtnVisiblity()
        {
            if (ChatItems[ChatItems.Count() - 1].ActionButtonTag.Split('|')[0] == "P")
                return;

            for (int ctr = ChatItems.Count() - 1; ctr >= 0; ctr--)
            {
                if (ChatItems[ctr].ActionButtonTag.Split('|')[0] == "P")
                    break;

                ChatItems[ctr].ActionButtonVisiblity = Visibility.Collapsed;
            }
            ChatItems[ChatItems.Count() - 1].ActionButtonVisiblity = Visibility.Visible;
        }
        public void HandleSingleContent(string content)
        {
            ResponseBuilder.Append(content);
            LastReceived = DateTime.Now;
        }
        public virtual async void HandleSegmentsAsync(List<ChatTurboResponseSegment> segments)
        {
            foreach (var segment in segments)
            {
                // Determine the author based on whether the segment is code
                var author = segment.IsCode ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;

                var previousItem = ChatItems[ChatItems.Count() - 1];
                var previousTag = previousItem.ActionButtonTag.Substring(0, 2);
                string res = string.Empty;
                if (!String.IsNullOrEmpty(previousTag))
                {
                    if ((previousTag == "R|" && author == AuthorEnum.ChatGPT) || (previousTag == "C|" && author == AuthorEnum.ChatGPTCode))
                    {
                        res = previousItem.Document.Text;
                        if (previousItem.IsFirstSegment)
                            IsFirst = true;

                        await DeleteChatAsync(ChatItems.Count() - 1);
                        ChatItems.Remove(previousItem);
                    }
                }

                if (!string.IsNullOrEmpty(res))
                    res = string.Concat(res, " ", segment.Content);
                else
                    res = segment.Content;
                ChatItems.Add(AddChatItem(author, res, IsFirst, ChatItems.Count()));
                RefreshChatItem();

                IsFirst = false;
                ChangeResponseBtnVisiblity();
                RefreshChatItem();
            }
            RefreshChatItem();
            OnRequestComplete(true);
        }
    }
}

