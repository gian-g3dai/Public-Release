using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json.Linq;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Unakin;
using Unakin.Options;
using Unakin.Utils;
using UnakinShared.DTO;
using UnakinShared.Enums;
using UnakinShared.Helpers.Classes;
using UnakinShared.Helpers.Interfaces;
using UnakinShared.Models;
using UnakinShared.Utils;

namespace UnakinShared.Helpers.Classes
{
    internal class ChatVM : Base.ViewModelBase
    {
        private HistoryHelper historyHelper;

        public ChatVM(ChatType chatType, Action<Boolean> onRequestComplete, Action refreshChat, CancellationTokenSource cancellationTokenSource) {
            this.OnRequestComplete = onRequestComplete;
            this.RefreshChatItem = refreshChat;
            this.ChatType = chatType; 
        }

        #region "Properties"
        private ChatType _chatType;
        internal ChatType ChatType
        {
            get
            {
                return _chatType;
            }
            set
            {
                _chatType = value;
                RefreshChat(_chatType);

            }
        }
        internal OpenAIService LLM { get; set; }
        internal IChatHelper ChatHelper { get; set; }
        //internal IChatHelper AgentChatHelper { get; set; }
        internal Action<Boolean> OnRequestComplete { get; set; }
        internal Action RefreshChatItem { get; set; }
      
        internal Conversation Chat {
            get
            {
                return ChatHelper.Chat;
            }
        }
        ChatMaster ChatMaster {
            get
            {
                return ChatHelper.ChatMaster;
            }
        }
        List<ChatDetail> ChatDetails
        {
            get
            {
                return ChatHelper.ChatDetails;
            }
        }
        public List<ChatItemDTO> ChatItems
        {
            get
            {
                return ChatHelper.ChatItems;
            }
        }



        public List<ChatItemDTO> HistoryItems { get; set; }
        public List<string> Commmands { get; set; }
        public string ChatName { get; set; }
        public Visibility CommandVisiblity { get; set; }
        public Visibility HistoryVisiblity { get; set; }
        public Visibility AgentHubVisiblity { get; set; }
        public Visibility ChatVisiblity { get; set; }
        public bool CanShowEvents { get; set; }
        public bool CanRequest { get; set; }

        #endregion

        #region "Methods"
        public void AppendUserInput(string prompt)
        {
            ChatHelper.AppendUserInput(LLM, prompt);
        }
        //public ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        //{
        //    return ChatHelper.AddChatItem(author, message, firstSegment, index, isDirty);
        //}
        internal void RefreshChat(ChatType chatType)
        {
            ChangeCommandVisiblity(false);

            if (chatType == ChatType.Chat)
            {
                ChatHelper = new RegularChatHelper();
            }
            else if (chatType == ChatType.Agents)
            {
                ChatHelper = new AgentChatHelper();
            }
            else if (chatType == ChatType.IDE)
            {
                ChatHelper = new IDEChatHelper();
            }
            else if (chatType == ChatType.SemanticSearch)
            {
                ChatHelper = new SemanticSearchChatHelper();
            }
            else if (chatType == ChatType.ProjectSummary)
            {
                ChatHelper = new ProjectSummaryChatHelper();
            }
            else if (chatType == ChatType.AutomatedTesting)
            {
                ChatHelper = new AutomatedTestingChatHelper();
            }
            else if (chatType == ChatType.AutonomousAgent)
            {
                ChatHelper = new AutonomousAgentHelper();
            }
            else
            {
                ChatHelper = new RegularChatHelper();
            }

            LLM = UnakinPackage.Instance.OptionsGeneral.Service;
            ChatHelper.RefreshChat(LLM);
            SetDefaultVisiblity();
            ChatHelper.OnRequestComplete = OnRequestComplete;
            ChatHelper.RefreshChatItem = RefreshChatItem;
            NotifyPropertyChanged("ChatItems");
            RefreshChatItem();

        }
        public async Task<string> GetLLMResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {
            if (LLM== OpenAIService.UNAKIN)
            {
                return await ChatHelper.GetUnakinResponseAsync(prompt, agent, cancellationToken);
            }
            else if (LLM == OpenAIService.OpenAI)
            {
                return await ChatHelper.GetChatGPTResponseAsync(prompt,agent, cancellationToken);    
            }
            NotifyPropertyChanged("ChatItems");
            return null;
        }

        public async Task<string> GetLLMAgentResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken)
        {

            if (LLM == OpenAIService.UNAKIN)
            {
                return await ChatHelper.GetUnakinResponseAsync(prompt, agent, cancellationToken);
            }
            else if (LLM == OpenAIService.OpenAI)
            {
                return await ChatHelper.GetChatGPTResponseAsync(prompt, agent, cancellationToken);
            }
            NotifyPropertyChanged("ChatItems");
            return null;
        }
        public async System.Threading.Tasks.Task SaveChatAsync()
        {
            await ChatHelper.SaveChatAsync();
        }
        public async System.Threading.Tasks.Task DeleteChatAsync(int index)
        {
            await ChatHelper.DeleteChatAsync(index);
        }
        public async System.Threading.Tasks.Task LoadChatAsync(int index)
        {
            var ChatMaster = await AppDatabase.database.Table<ChatMaster>().Where(x => x.Id == index).FirstOrDefaultAsync();

            if (ChatMaster == null)
                return;

            ChatType = TurboChatHelper.GetChatType(ChatMaster.ChatType);

            await ChatHelper.LoadChatAsync(LLM, index);
            SetDefaultVisiblity();
            NotifyPropertyChanged("ChatItems");
        }
        public void ChangeCommandVisiblity(bool makeVisible)
        {
            CommandVisiblity = makeVisible? Visibility.Visible: Visibility.Collapsed;
            NotifyPropertyChanged("CommandVisiblity");
            NotifyPropertyChanged("Commmands");
        }
        public void LoadCommands(string filter)
        {
            if (ChatType == ChatType.Agents)
            {
                Commmands = OptionPageGridCommands.Commands.Where(x => x.Value.Item3 == true && x.Key == 13).Select(x => x.Value.Item1).ToList();
            }
            else if (ChatType == ChatType.AutonomousAgent)
            {
                Commmands = null;
            }
            else
            {
                Commmands = OptionPageGridCommands.Commands.Where(x => x.Value.Item3 == true && x.Key != 13).Select(x => x.Value.Item1).ToList();

            }
            filter = filter.Replace("//", string.Empty);
            if (Commmands != null && !String.IsNullOrEmpty(filter)) 
                Commmands = Commmands.Where(x => x.StartsWith(filter)).ToList();

            NotifyPropertyChanged("Commmands");
        }
        public async void LoadHistoryAsync()
        {
            var chatName = ChatName;
            ChatType = ChatType.Chat;
            if (chatName == "Unakin Chat")
                return;
            
            if (historyHelper == null)
            {
                historyHelper = new HistoryHelper();
            }
           
            var masterList = await AppDatabase.database.Table<ChatMaster>().OrderByDescending(x => x.UpdatedTime).ToListAsync();
            HistoryItems = new List<ChatItemDTO>();

            foreach (var item in masterList)
            {
                ChatItemDTO histItem;
                switch (item.ChatType)
                {
                    case 1: 
                        histItem = historyHelper.regularChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible; 
                        histItem.SecondTag = "Chat";
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case 2:
                        histItem = historyHelper.agentChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = "Agents";
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case 3:
                        histItem = historyHelper.iDEChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = Unakin.Utils.Constants.SEMANTICSEARCH_NAME;
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case 4: case 5: case 6: case 7: case 8: case 9: case 10: case 11: case 12:
                        histItem = historyHelper.iDEChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        var commandText = TurboChatHelper.GetChatName(item.Desc);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = commandText;
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case Constants.PROJECTSUMMARY_ID:
                        histItem = historyHelper.projectSummaryChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = Constants.PROJECTSUMMARY_NAME;
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case Constants.AUTOMATEDTESTING_ID:
                        histItem = historyHelper.automatedTestingChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = Constants.AUTOMATEDTESTING_NAME;
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case Constants.AUTONOMOUSAGENT_ID:
                        histItem = historyHelper.autonomousAgentChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = Constants.AUTONOMOUSAGENT_NAME;
                        histItem.CreationTime = item.CreatedTime;
                        break;
                    case Constants.DATAGEN_ID:
                        histItem = historyHelper.dataGenerationHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.SecondTagVisiblity = Visibility.Visible;
                        histItem.SecondTag = Constants.DATAGEN_NAME;
                        histItem.CreationTime = item.CreatedTime;
                        break;

                    default:
                        histItem = historyHelper.regularChatHelper.AddChatItem(AuthorEnum.Me, item.Desc, true, 1);
                        histItem.CreationTime = item.CreatedTime;
                        break;

                }
                if (histItem != null)
                {
                    histItem.Id  = item.Id;
                    HistoryItems.Add(histItem);
                }
                    
            }



            HistoryVisiblity = Visibility.Visible;
            ChatVisiblity = Visibility.Collapsed;
            AgentHubVisiblity = Visibility.Collapsed;
            CanShowEvents = false;
            CanRequest = false;
            ChatName = "Unakin Chat";

            NotifyPropertyChanged(nameof(HistoryVisiblity));
            NotifyPropertyChanged(nameof(ChatVisiblity));
            NotifyPropertyChanged(nameof(AgentHubVisiblity));
            NotifyPropertyChanged(nameof(ChatName));
            NotifyPropertyChanged(nameof(CanShowEvents));
            NotifyPropertyChanged(nameof(CanRequest));

            NotifyPropertyChanged(nameof(HistoryItems));
        }

        public async void SetDefaultVisiblity()
        {
            HistoryVisiblity = Visibility.Collapsed;
            ChatVisiblity = Visibility.Visible;
            AgentHubVisiblity = ChatType == ChatType.Agents? Visibility.Visible: Visibility.Collapsed;
            CanShowEvents = true;
            CanRequest = true;
            ChatName = "Chat History";

            NotifyPropertyChanged(nameof(HistoryVisiblity));
            NotifyPropertyChanged(nameof(ChatVisiblity));
            NotifyPropertyChanged(nameof(AgentHubVisiblity));
            NotifyPropertyChanged(nameof(ChatName));
            NotifyPropertyChanged(nameof(CanShowEvents));
            NotifyPropertyChanged(nameof(CanRequest));
        }

        private class HistoryHelper()
        {
            internal RegularChatHelper regularChatHelper = new RegularChatHelper();
            internal AgentChatHelper agentChatHelper = new AgentChatHelper();
            internal IDEChatHelper iDEChatHelper = new IDEChatHelper();
            internal SemanticSearchChatHelper semanticSearchChatHelper = new SemanticSearchChatHelper();
            internal ProjectSummaryChatHelper projectSummaryChatHelper = new ProjectSummaryChatHelper();
            internal AutomatedTestingChatHelper automatedTestingChatHelper =  new AutomatedTestingChatHelper();
            internal AutonomousAgentHelper autonomousAgentChatHelper = new AutonomousAgentHelper();
            internal DataGenerationHelper dataGenerationHelper = new DataGenerationHelper();


        }

        #endregion




        }
}
