using Community.VisualStudio.Toolkit;
using Unakin.Options;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Media;
using UnakinShared.Utils;
using OpenAI_API.Chat;
using System.Collections.ObjectModel;
using UnakinShared.DTO;
using UnakinShared.Models;
using System.Text;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using ICSharpCode.AvalonEdit.Document;
using UnakinShared.Enums;

namespace Unakin.ToolWindows
{
    public partial class AgentControl : UserControl
    {
        public AgentControl()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        #region Properties

        private OptionPageGridGeneral options;
        private Microsoft.VisualStudio.Shell.Package package;
        private Conversation chat;
        private List<ChatItemDTO> chatItems;
        private CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool isFirst = false;
        private bool hasAPIAuthFailed = false;

        #region Agent Management Properties

        // ObservableCollection to hold the agents
        public ObservableCollection<AgentDTO> Agents { get; set; }


        #endregion

        #endregion Properties

        #region Event Handlers

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grdRoot.Height = AgentsWindow.ActualHeight - 10;
            grdRoot.Width = AgentsWindow.ActualWidth - 25;
            colFunctionality.Width = AgentsWindow.ActualWidth > 400 ? AgentsWindow.ActualWidth - 350 : 100;
        }

        /// <summary>
        /// Handles the Click event of the btnRequestCode control.
        /// </summary>
        public async void SendCode(Object sender, ExecutedRoutedEventArgs e)
        {
            await RequestCHATGptAsync(CommandType.Code);
        }

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void SendRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            await RequestCHATGptAsync(CommandType.Request);
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public async void CancelRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            EnableDisableButtons(true);
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Sends a request asynchronously based on the command type.
        /// </summary>
        /// <param name="commandType">The type of command to execute.</param>
        private async System.Threading.Tasks.Task RequestCHATGptAsync(CommandType commandType)
        {
            if (!await AuthHelper.ValidateAPIAsync())
            {
                hasAPIAuthFailed = true;
                return;
            }

            if (hasAPIAuthFailed == true)
            {
                chatMaster = new ChatMaster();
                chatDetails = new List<ChatDetail>();

                chatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
                chatMaster.CreatedTime = DateTime.Now;
                chatMaster.UpdatedTime = DateTime.Now;
                chatMaster.ChatType = 2;

                chat = ChatGPT.CreateConversation(options);

                hasAPIAuthFailed = false;
            }

            try
            {
                shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string requestText = options.MinifyRequests ? TextFormat.MinifyText(txtRequest.Text) : txtRequest.Text;
                txtRequest.Text = string.Empty;
                EnableDisableButtons(false);
                requestText = TextFormat.RemoveCharactersFromText(requestText, options.CharactersToRemoveFromRequests.Split(','));
                chat.AppendUserInput(requestText);
          
                chatItems.Add(new ChatItemDTO(AuthorEnum.Me, requestText, true, chatItems.Count));
                string combinedResponse = requestText;

                // Process the response for agent interaction if valid
                try
                {

                    cancellationTokenSource = new CancellationTokenSource();
                    foreach (var agent in Agents)
                    {
                        if (!agent.Active)
                            continue;

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                            continue;

                        string agentImage = agent.Image;
                        var agentItem = new ChatItemDTO(AuthorEnum.ChatGPT, String.Concat(" ", agent.Name, " - ", agent.Functionality), true, chatItems.Count, imagePath: agentImage);
                        agentItem.AgentSequence = agent.Sequence;
                        agentItem.IsHeaderVisible = Visibility.Visible;
                        agentItem.IsItemVisible = Visibility.Hidden;
                        agentItem.ActionButtonVisiblity = Visibility.Collapsed;
                        chatItems.Add(agentItem);
                        agentItem.ActionButtonVisiblity = Visibility.Collapsed;
                        string requestWithAgentFunctionality = agent.Functionality + Environment.NewLine + combinedResponse;
                        combinedResponse = await GetChatResponseAsync(requestWithAgentFunctionality);
                        agentItem.ActionButtonVisiblity = Visibility.Collapsed;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }


                chatList.Items.Refresh();
                scrollViewer.ScrollToEnd();
                EnableDisableButtons(true);
                await System.Threading.Tasks.Task.Run(async () => await SaveChatAsync()).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true);
            }
            catch (Exception ex)
            {
                EnableDisableButtons(true);
                if (ex.Source.ToLower().Trim() == "openai_api" && ex.Message.Contains("Please reduce the length of the messages or completion"))
                {
                    MessageBox.Show("The chat conversation has reached its limit. Please clear the conversation and send your question again.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        /// <summary>
        /// Copies the text of the chat item at the given index to the clipboard.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The event arguments.</param>
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int index = (int)button.Tag;

            TerminalWindowHelper.Copy(button, chatItems[index].Document.Text);
        }

        /// <summary>
        /// Handles the click event for the Clear button, which clears the conversation.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            //MessageBoxResult result = MessageBox.Show("Clear the conversation?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            //if (result == MessageBoxResult.No)
            //{
            //    return;
            //}
            chatMaster = new ChatMaster();
            chatDetails = new List<ChatDetail>();

            chatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
            chatMaster.CreatedTime = DateTime.Now;
            chatMaster.UpdatedTime = DateTime.Now;
            chatMaster.ChatType = 2;

            chat = ChatGPT.CreateConversation(options);
            chatItems.Clear();
            chatList.Items.Refresh();
        }

        /// <summary>
        /// Handles the mouse wheel event for the text editor by scrolling the view.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The mouse wheel event arguments.</param>
        private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is AgentDTO agent)
            {
                agent.Active = chk.IsChecked.Value;
                agent.IsDirty = true;
                SaveAgentsAsync();
            }
        }

        private async void btnChatStory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Unakin.ToolWindows.ChatHistoryDialog();
            dialog.ChatType = 2;
            dialog.ChatListLoad();
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
            if (dialog.Id > 0)
            {
                var detailList = await AppDatabase.database.Table<ChatDetail>().Where(x => x.ChatMasterId == dialog.Id).OrderBy(x => x.Index).ToListAsync();

                chatItems.Clear();
                chatMaster = await AppDatabase.database.Table<ChatMaster>().Where(x => x.Id == dialog.Id).FirstOrDefaultAsync();
                foreach (var item in detailList)
                {
                    AuthorEnum aurthor = AuthorEnum.Me;
                    if (item.Aurthor == 1) aurthor = AuthorEnum.ChatGPT;
                    if (item.Aurthor == 2) aurthor = AuthorEnum.ChatGPTCode;
                    ChatItemDTO agentItem;
                    if (item.Sequence > 0)
                    {
                        string agentImage = item.Image;
                        agentItem = new ChatItemDTO(AuthorEnum.ChatGPT, item.Content, true, chatItems.Count, imagePath: agentImage);
                        agentItem.AgentSequence = item.Sequence;
                        agentItem.IsHeaderVisible = Visibility.Visible;
                        agentItem.IsItemVisible = Visibility.Hidden;
                    }
                    else
                    {
                        agentItem = new ChatItemDTO(aurthor, item.Content, item.IsFirstSegment, item.Index, false);
                    }
                    chatItems.Add(agentItem);

                }
                chatList.Items.Refresh();
                if (Unakin.UnakinPackage.Instance.OptionsGeneral.Service == OpenAIService.OpenAI)
                {
                    ChatGPT.RegerateChat(options, chatItems, chat);
                }
            }
            dialog.Close();
        }

        #endregion Event Handlers

        #region Methods

        private async System.Threading.Tasks.Task<string> GetChatResponseAsync(string prompt)
        {
            string response;
            if (UnakinPackage.Instance.OptionsGeneral.Service == OpenAIService.OpenAI)
            {
                response = await GetResponseFromChatGPTAsync(prompt);
            }
            else
            {
                response = await GetResponseFromUNAKINAsync(prompt);
            }
            return response;
        }

        private async Task<string> GetResponseFromUNAKINAsync(string prompt)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            StringBuilder responseBuilder = new StringBuilder();
            AuthorEnum aurthor =  AuthorEnum.ChatGPT;
            ChatItemDTO chatItem = null;
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            DateTime lastReceived = DateTime.Now; // Timestamp of the last received data
            System.Threading.Tasks.Task responseTask;

            if (option.SingleResponse)
            {
                void HandleSingleContent(string content)
                {
                    responseBuilder.Append(content);
                    lastReceived = DateTime.Now; // Update the timestamp when new data is received         
                }
                responseTask = Unakin.Utils.Unakin.CallWebSocket(UnakinPackage.Instance.OptionsGeneral, prompt, HandleSingleContent, CancellationToken.None, "Agent Hub");

                await CommonUtils.HasTimedOutAsync(responseTask, lastReceived);

                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments(responseBuilder.ToString());
                foreach (var segment in segments)
                {
                    AuthorEnum author = segment.IsCode ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;
                    chatItem = new ChatItemDTO(author, segment.Content, false, chatItems.Count);
                    chatItems.Add(chatItem);
                    isFirst = false;
                }
            }
            else
            {  
                chatItem = new ChatItemDTO(aurthor, string.Empty, false, chatItems.Count);
                chatItems.Add(chatItem);
                void HandlestreamContent(string content)
                {
                    responseBuilder.Append(content);
                    lastReceived = DateTime.Now; // Update the timestamp when new data is received

                    var item = TurboChatHelper.SaprateCode(chatItems, chatItem, aurthor);
                    chatItem = item.Item1;
                    aurthor = item.Item2;

                    chatItem.Message = chatItem.Message + content;
                    chatItem.UpdateDoccument();
                    chatList.Items.Refresh();
                    scrollViewer.ScrollToEnd();
                }
                responseTask = Unakin.Utils.Unakin.CallWebSocket(UnakinPackage.Instance.OptionsGeneral, prompt, HandlestreamContent, CancellationToken.None, "Agent Hub");
                await CommonUtils.HasTimedOutAsync(responseTask, lastReceived);
            }

            chatList.Items.Refresh();
            scrollViewer.ScrollToEnd();
            return responseBuilder.ToString();
        }

        /// <summary>
        /// Sends a request to the chatbot asynchronously and waits for a response.
        /// </summary>
        /// <returns>The response from the chatbot.</returns>
        private async System.Threading.Tasks.Task<string> GetResponseFromChatGPTAsync(string prompt)
        {
            string result;
            var option = Unakin.UnakinPackage.Instance.OptionsGeneral;
            StringBuilder responseBuilder = new StringBuilder();

            if (!options.SingleResponse)
            {
                AuthorEnum aurthor = AuthorEnum.ChatGPT;
                ChatItemDTO chatItem = null;
                chatItem = new ChatItemDTO(aurthor, string.Empty, false, chatItems.Count);
                chatItems.Add(chatItem);

                void HandlestreamContent(string content)
                {
                    responseBuilder.Append(content);
                    var item = TurboChatHelper.SaprateCode(chatItems, chatItem, aurthor);
                    chatItem = item.Item1;
                    aurthor = item.Item2;

                    chatItem.Message = chatItem.Message + content;
                    chatItem.UpdateDoccument();
                    chatList.Items.Refresh();
                    scrollViewer.ScrollToEnd();
                }
                await ChatGPT.GetResponseAsync(option, "", prompt, options.StopSequences.Split(','), HandlestreamContent, cancellationTokenSource.Token);
                return responseBuilder.ToString();
            }
            else
            {
                chat.AppendUserInput(prompt);
                Task<string> task = chat.GetResponseFromChatbotAsync();
                await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments((await task));
                foreach (var segment in segments)
                {
                    AuthorEnum author = segment.IsCode ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;

                    var chatItem = new ChatItemDTO(author, segment.Content, false, chatItems.Count);
                    chatItems.Add(chatItem);
                    isFirst = false;
                }
                chatList.Items.Refresh();
                scrollViewer.ScrollToEnd();

                return await task;
            }
        }

        /// <summary>
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Microsoft.VisualStudio.Shell.Package package)
        {
            this.options = options;
            this.package = package;

            chatMaster = new ChatMaster();
            chatDetails = new List<ChatDetail>();

            chatMaster.Name = DateTime.Now.ToString("ddMMYYYYhhmmss");
            chatMaster.CreatedTime = DateTime.Now;
            chatMaster.UpdatedTime = DateTime.Now;
            chatMaster.ChatType = 2;

            chat = ChatGPT.CreateConversation(options);
            chatItems = new();
            chatList.ItemsSource = chatItems;

            RefreshAgentsAsync();
            lvAgents.ItemsSource = Agents;
        }

        /// <summary>
        /// Enables or disables the buttons based on the given boolean value.
        /// </summary>
        /// <param name="enable">Boolean value to enable or disable the buttons.</param>
        private void EnableDisableButtons(bool enable)
        {
            grdProgress.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            btnClear.IsEnabled = enable;
            btnRequestCode.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
            btnCancel.IsEnabled = !enable;

            btnClear.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnRequestCode.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnRequestSend.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = !enable ? Visibility.Visible : Visibility.Collapsed;
        }

        ChatMaster chatMaster;
        List<ChatDetail> chatDetails;
        async System.Threading.Tasks.Task SaveChatAsync()
        {
            if (chatMaster == null)
                return;

            var detailsToUpdate = new List<ChatItemDTO>(chatItems.Where(x => x.IsDirty == true).ToList());

            if (chatDetails == null || chatDetails == null || (detailsToUpdate.Count() == 0))
                return;

            var title = chatItems.OrderBy(x => x.Index).First()?.Message;
            title = title.Truncate(40);
            if (string.IsNullOrEmpty(chatMaster.Desc) || chatMaster.Desc != title)
            {
                chatMaster.Desc = title;
                chatMaster.ChatType = 2;

                if (AppDatabase.database != null && chatMaster.Id == 0)
                {
                    await AppDatabase.database.InsertAsync(chatMaster);
                }
            }
            chatMaster.UpdatedTime = DateTime.Now;
            if (AppDatabase.database != null)
            {
                await AppDatabase.database.UpdateAsync(chatMaster);
            }


            List<ChatDetail> inserted = new List<ChatDetail>();
            List<ChatDetail> updated = new List<ChatDetail>();
            foreach (var item in chatItems.Where(x => x.IsDirty == true))
            {
                var detail = chatDetails.FirstOrDefault(x => x.Index == item.Index);
                var tag = item.ActionButtonTag.Split('|')[0];
                int aurthor = 0;
                if (tag == "R") aurthor = 1; else if (tag == "C") aurthor = 2;

                if (detail == null)
                {
                    detail = new ChatDetail();
                    detail.ChatMasterId = chatMaster.Id;
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

                foreach (var item in chatItems.Where(x => x.IsDirty == true))
                {
                    if (detailsToUpdate.Where(x => x.Index == item.Index && x.Message == item.Message).Any())
                        item.IsDirty = false;
                }
            }
        }

        // Field to track the dragged item
        private AgentDTO _draggedItem;
        private static readonly string AgentDataFormat = "MyCustomAgentFormat";
        private int _draggedItemIndex = -1;
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource != null && (e.OriginalSource.ToString().Contains("Image") || e.OriginalSource.ToString().Contains("Rectangle")))
            {

                e.Handled = false; return;
            }

            var listBox = sender as ListView;
            var listBoxItem = GetElementFromPoint<ListViewItem>(listBox, e.GetPosition(listBox));

            if (listBoxItem != null && listBoxItem.DataContext is AgentDTO agent)
            {
                var dataObject = new DataObject(DataFormats.StringFormat, agent.Name);
                DragDrop.DoDragDrop(listBoxItem, dataObject, DragDropEffects.Move);
            }
        }
        private static T GetElementFromPoint<T>(UIElement parent, Point point) where T : UIElement
        {
            var element = parent.InputHitTest(point) as UIElement;
            while (element != null)
            {
                if (element is T correctlyTyped)
                {
                    return correctlyTyped;
                }

                element = VisualTreeHelper.GetParent(element) as UIElement;
            }
            return null;
        }
        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var agentName = e.Data.GetData(DataFormats.StringFormat) as string;
                var agent = Agents.FirstOrDefault(a => a.Name == agentName);

                var listBox = sender as ListBox;
                var targetItem = GetElementFromPoint<ListBoxItem>(listBox, e.GetPosition(listBox));

                if (targetItem != null && targetItem.DataContext is AgentDTO targetAgent)
                {
                    var originalIndex = Agents.IndexOf(agent);
                    var targetIndex = Agents.IndexOf(targetAgent);

                    if (originalIndex != targetIndex)
                    {
                        Agents.Move(originalIndex, targetIndex);
                    }
                }
            }
            for (var ctr = 0; ctr < Agents.Count(); ctr++)
            {
                Agents[ctr].Sequence = ctr + 1;
                Agents[ctr].IsDirty = true;
            }
            e.Handled = true;
            SaveAgentsAsync();
        }
        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
        private void RemoveAgent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AgentDTO agent)
            {
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to remove {agent.Name}?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Agents.Remove(agent);
                    DeleteAgentAsync(agent.Id);
                }
            }
        }


        #region Agent Management Methods

        public void AddAgent_Click(object sender, RoutedEventArgs e)
        {
            var newAgent = ShowAddAgentDialog();
            if (newAgent == null)
                return;

            newAgent.Sequence = Agents.Count() + 1;
            newAgent.Active = true;
            newAgent.IsDirty = true;
            var imageName = Path.GetFileName(newAgent.Image);
            var finalImageName = imageName;
            var ctr = 0;
            var path = Unakin.UnakinPackage.Instance.UserDataPath;
            var imageDir = path + @"\Images\Agents";
           
            if (!Directory.Exists(imageDir))
            {
                Directory.CreateDirectory(imageDir);
            }
            while (File.Exists(imageDir + "/" + finalImageName))
            {
                ctr += 1;
                finalImageName = imageName + ctr.ToString();
            }
            File.Copy(newAgent.Image, imageDir + "/" + finalImageName);
            newAgent.Image = imageDir + "/" + finalImageName;
            if (newAgent != null)
            {
                Agents.Add(newAgent); // Add new agent to ObservableCollection
            }
            SaveAgentsAsync();
        }

        private AgentDTO ShowAddAgentDialog()
        {
            var dialog = new Unakin.ToolWindows.AddAgentDialog();
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                return dialog.NewAgent;
            }
            else
            {
                return null; // User cancelled the dialog
            }

            return null;
        }

        async void SaveAgentsAsync()
        {
            if (Agents == null)
                return;

            var detailsToUpdate = new List<AgentDTO>(Agents.Where(x => x.IsDirty == true).ToList());

            if (detailsToUpdate.Count() == 0)
                return;

            List<Agent> inserted = new List<Agent>();
            List<Agent> updated = new List<Agent>();
            foreach (var item in detailsToUpdate.Where(x => x.IsDirty == true))
            {
                var detail = await AppDatabase.database.Table<Agent>().Where(x => x.Id == item.Id).FirstOrDefaultAsync();
                if (detail == null)
                {
                    detail = new Agent();
                    detail.Name = item.Name;
                    detail.Functionality = item.Functionality;
                    detail.Image = item.Image;
                    detail.Sequence = item.Sequence;
                    detail.Active = item.Active;
                    inserted.Add(detail);
                }
                else
                {
                    detail.Name = item.Name;
                    detail.Functionality = item.Functionality;
                    detail.Image = item.Image;
                    detail.Sequence = item.Sequence;
                    detail.Active = item.Active;
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
                RefreshAgentsAsync();
                lvAgents.Items.Refresh();
            }
        }

        async void DeleteAgentAsync(int index)
        {
            if (AppDatabase.database != null)
            {
                var qryDt = String.Concat("DELETE FROM Agent WHERE Id = ", index.ToString());
                await AppDatabase.database.QueryAsync<Agent>(qryDt);
            }
        }

        async void RefreshAgentsAsync()
        {
            if (Agents == null)
                Agents = new ObservableCollection<AgentDTO>();
            var details = await AppDatabase.database.Table<Agent>().ToListAsync();
            var recordstoAdd = details.Select(x => new AgentDTO
            {
                Id = x.Id,
                Name = x.Name,
                Functionality = x.Functionality,
                Image = x.Image,
                Sequence = x.Sequence,
                Active = x.Active
            }).OrderBy(x => x.Sequence);
            Agents.Clear();
            foreach (var item in recordstoAdd)
            {
                Agents.Add(item);
            }

            if (Agents.Count() == 0)
            {
                var qryDt = String.Concat("SELECT SEQ FROM SQLITE_SEQUENCE WHERE NAME='Agent' LIMIT 1;");
                var identity = (await AppDatabase.database.QueryAsync<Int32>(qryDt)).FirstOrDefault();
                if (identity <= 1)
                {
                    Agents.Add(new AgentDTO { Name = "Genie", Functionality = "Create a script", Image = "pack://application:,,,/Unakin;component/Resources/genie.png", Sequence = 1, Active = true, IsDirty = true });
                    Agents.Add(new AgentDTO { Name = "Dante", Functionality = "Add comments to the script", Image = "pack://application:,,,/Unakin;component/Resources/dante.png", Sequence = 2, Active = true, IsDirty = true });
                    Agents.Add(new AgentDTO { Name = "Optimus", Functionality = "Optimize the script", Image = "pack://application:,,,/Unakin;component/Resources/optimus.png", Sequence = 3, Active = true, IsDirty = true });

                    SaveAgentsAsync();
                }
            }

        }

        #endregion

        #region Local Workflow Methods
        /// <summary>
        /// Event handler for the Local Workflow button click.
        /// </summary>
        private void LocalWorkflow_Click(object sender, RoutedEventArgs e)
        {
            SearchLocally();
        }


        private async void SearchLocally()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = dialog.SelectedPath;
                EnableDisableButtons(false);


                foreach (string file in Directory.GetFiles(folderPath, "*.*", searchOption: SearchOption.AllDirectories).Where(x => x.ToLower().EndsWith(".cs") || x.ToLower().EndsWith(".cpp") || x.ToLower().EndsWith(".h")))
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        string modifiedContent = content;
                        string laug = file.ToLower().EndsWith(".cs") ? "C#" : "C++";

                        foreach (var agent in Agents)
                        {
                            if (agent.Active == false)
                                continue;

                            cancellationTokenSource = new CancellationTokenSource();


                            string requestWithAgentFunctionality = String.Concat("Considering that this is a ", laug, " scripts in the local project. Script starts at ", Constants.startComment, " and ends at ", Constants.endComment, ".");
                            requestWithAgentFunctionality += String.Concat("Please send releted code script enclosed between ", Constants.startComment, "  and ", Constants.endComment, " comments in response. Note: Don't delete ", Constants.startComment, "  and ", Constants.endComment, " comments.");
                            requestWithAgentFunctionality += Environment.NewLine + $"{agent.Functionality + Environment.NewLine + " give me the full script back"}\n{Constants.startComment + modifiedContent + Constants.endComment}";
                            try
                            {
                                string response = await ChatGPT.GetResponseAsync(options, string.Empty, requestWithAgentFunctionality, options.StopSequences.Split(','), cancellationTokenSource.Token);

                                // Extract content between "```" and add non-code parts as comments
                                modifiedContent = ExtractContentAndComments(response);

                                // Optional: Validate the extracted content
                                // bool isContentValid = ...; // Implement validation logic if needed
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            finally
                            {
                                cancellationTokenSource.Dispose();
                            }
                        }
                        string newFileName = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file) + "-unakin" + Path.GetExtension(file));
                        File.WriteAllText(newFileName, modifiedContent);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                EnableDisableButtons(true);
                MessageBox.Show("Processing completed.", "Local Workflow", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string ExtractContentAndComments(string input)
        {
            int start, end = -1;
            string val = string.Empty;

            start = input.IndexOf(Constants.startComment);
            end = input.IndexOf(Constants.endComment);

            if (start > -1 && end > -1)
                val = input.Substring(start, (end - start) + Constants.endComment.Length);
            else
                val = input;

            return val.Replace(Constants.startComment, string.Empty).Replace(Constants.endComment, string.Empty);
        }
        #endregion

        #endregion Methods   


    }

}

