using Community.VisualStudio.Toolkit;
using Unakin.Options;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using System.Runtime.ConstrainedExecution;
using System.Windows.Media;
using System.Reflection;
using UnakinShared.Models;
using Microsoft.VisualStudio.Text.Editor;
using UnakinShared.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using ICSharpCode.AvalonEdit.Document;
using System.Data.SqlClient;
using OpenAI_API.Chat;
using System.Globalization;
using UnakinShared.Enums;
using UnakinShared.DTO;
using UnakinShared.Helpers.Classes;
using System.Collections.ObjectModel;
using System.IO;
using ShellInterop = Microsoft.VisualStudio.Shell.Interop;
using ICSharpCode.AvalonEdit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Microsoft.VisualStudio.Threading;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using Unakin;
using Microsoft.Extensions.Options;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using System.Runtime.Remoting.Channels;

namespace Unakin.ToolWindows
{
    public partial class ChatControl : UserControl
    {
        public ObservableCollection<AgentDTO> Agents { get; set; }
        //public ObservableCollection<AgentDTO> AutonomousAgents { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        private List<String> workingFiles = new List<String>();
        private bool shiftKeyPressed;
        bool isEditRunning = false;
        private bool hasAPIAuthFailed = false;
        private ChatVM currentVM;
        private bool contextFlag = false; // Default to false or true based on your initial state
        private bool clearRequested = false;



        public ChatControl()
        {
            InitializeComponent();
            UnakinPackage.Instance.TurboWindowInstance = this;
            currentVM = new ChatVM(ChatType.Chat, EnableDisableButtons, RefreshChatItem, cancellationTokenSource);
            //ShowAgents.IsEnabled = false;
            CheckboxAgentsVisible.IsChecked = true; // For example, start with agents visible
            CheckboxAgentsHidden.IsChecked = false;
            AgentHeader.Visibility = Visibility.Collapsed;
            scrollAgent.Visibility = Visibility.Collapsed;
            // Hide the tab when the Agents hub is deactivated
            GeneralAgentWorkflowTab.Visibility = Visibility.Collapsed;
            currentVM.ChatType = ChatType.Chat;
            // Assign KeyDown event handler to txtRequest text box
            txtRequest.PreviewKeyDown += TxtRequest_PreviewKeyDown;
            LoadCustomSyntaxHighlighting();
        }


        private void LoadCustomSyntaxHighlighting()
        {
            var resourceName = "Unakin.Utils.CustomLanguage.xshd";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                // Load the custom highlighting definition from the .xshd file
                var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

                // Register the highlighting definition with the HighlightingManager
                HighlightingManager.Instance.RegisterHighlighting("CustomLanguage", new string[] { ".ext" }, highlighting);

                // Apply the highlighting to the AvalonEdit instances by name
                //txtRequest.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CustomLanguage");
                // Apply to other AvalonEdit instances as needed
                //normChat.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CustomLanguage");
            }
        }


        #region Public Methods
        public void StartControl()
        {
            this.DataContext = currentVM;

            var options = UnakinPackage.Instance.OptionsGeneral;
            CommonUtils.UserName = options.UserName;
            CommonUtils.Password = options.Password;

            setDefaultdirectory();
            setWorkingDirDisplay();
            CommonUtils.IsDirectoryChanged = true;

            RefreshAgentsAsync();
        }

        #endregion

        #region Events

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grdRoot.Height = ChatWindow.ActualHeight - 10;
            grdRoot.Width = ChatWindow.ActualWidth - 25;
            colFunctionality.Width = ChatWindow.ActualWidth > 400 ? ChatWindow.ActualWidth - 350 : 100;
        }

        public async void SendRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            string[] commands = { Unakin.Utils.Constants.SEMANTICSEARCH_NAME, Constants.PROJECTSUMMARY_NAME, Constants.AUTOMATEDTESTING_NAME, Constants.DATAGEN_NAME };
            var commndArray = txtRequest.Text.Split(':');
            string command = null;
            if (commndArray.Length > 1)
            {
                command = commndArray[0].Replace("//", string.Empty).Trim();
            }
            if (!string.IsNullOrEmpty(command) && commands.Contains(command))
            {
                currentVM.ChatType = TurboChatHelper.GetChatType(command);
                currentVM.RefreshChat(currentVM.ChatType);

                if (currentVM.ChatType == ChatType.SemanticSearch)
                {
                    await GetSemanticResponseAsync(txtRequest.Text.Replace("//" + command + " - ", string.Empty));
                    txtRequest.Text = string.Empty;
                }
                else if (currentVM.ChatType == ChatType.ProjectSummary)
                {
                    txtRequest.Text = string.Empty;
                    await GetProjectSummResponseAsync(string.Empty);
                }
                else if (currentVM.ChatType == ChatType.AutomatedTesting)
                {
                    txtRequest.Text = string.Empty;
                    await GetAutomatedTestingResponseAsync(string.Empty);
                }
                else if (currentVM.ChatType == ChatType.DataGeneration)
                {
                    txtRequest.Text = string.Empty;
                    await GetDataGenerationResponseAsync();
                }


                return;
            }
            else if (currentVM.ChatType == ChatType.SemanticSearch || currentVM.ChatType == ChatType.ProjectSummary || currentVM.ChatType == ChatType.AutomatedTesting || currentVM.ChatType == ChatType.DataGeneration)
            {
                currentVM.ChatType = ChatType.Chat;
                currentVM.RefreshChat(currentVM.ChatType);
            }


            if (currentVM.ChatType == ChatType.Chat || currentVM.ChatType == ChatType.IDE)
            {
                await GetChatResponseAsync();
            }
            else if (currentVM.ChatType == ChatType.Agents)
            {
                await GetAgentsResponseAsync();
            }
            else if (currentVM.ChatType == ChatType.AutonomousAgent)
            {
                await GetAutonomousAgentResponseAsync();
            }
        }

        public async void CancelRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            //clearRequested = true;
            EnableDisableButtons(true);
            cancellationTokenSource?.Cancel();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            //MessageBoxResult result = MessageBox.Show("Clear the conversation?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            //if (result == MessageBoxResult.No) return;
            //clearRequested = true;
            currentVM.RefreshChat(currentVM.ChatType);
        }

        private void btnChatStory_Click(object sender, RoutedEventArgs e)
        {
            if (ShowAgents.IsChecked == true)
                ShowAgents.IsChecked = false;

            currentVM.LoadHistoryAsync();
        }

        private async void ChatDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as TextEditor;
            var listBoxItem = GetElementFromPoint<TextEditor>(listBox, e.GetPosition(listBox));

            if (listBoxItem != null && listBoxItem.DataContext is ChatItemDTO hist)
            {
                var dataObject = hist.Id;
                if (dataObject > 0)
                {
                    await currentVM.LoadChatAsync(dataObject);
                    if (currentVM.ChatType == ChatType.Agents)
                    {
                        ShowAgents.IsChecked = true;
                        scrollAgent.Visibility = Visibility.Visible;
                        RefreshAgentsAsync();
                        lvAgents.ItemsSource = Agents;
                        lvAgents.Items.Refresh();
                    }
                   
                }
            }
        }

        private void RequestTextChanged(object sender, EventArgs e)
        {
            if (txtRequest.Text.StartsWith("//"))
            {
                currentVM.LoadCommands(txtRequest.Text.Trim());
                if (currentVM.Commmands != null && currentVM.Commmands.Count > 0)
                {
                    currentVM.ChangeCommandVisiblity(true);
                }               
            }
            else
            {
                currentVM.ChangeCommandVisiblity(false);
            }
        }

        private void ListBox_InitCommand(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            var listBoxItem = GetElementFromPoint<ListBoxItem>(listBox, e.GetPosition(listBox));

            if (listBoxItem != null)
            {
                txtRequest.Text = string.Concat("//", listBoxItem.Content.ToString(), ": ");
                currentVM.ChangeCommandVisiblity(false);

                if (txtRequest.Text.Contains(Constants.PROJECTSUMMARY_NAME) || (txtRequest.Text.Contains(Constants.AUTOMATEDTESTING_NAME) || (txtRequest.Text.Contains(Constants.DATAGEN_NAME))))
                {
                    SendRequest(null, null);
                }
                else if (txtRequest.Text.Contains(Constants.LOCALWORKFLOW_NAME))
                {
                    SearchLocally();
                }
            }
        }

        /// <summary>
        /// Select Directory.
        /// </summary>
        public void SelectDirectory(Object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CommonUtils.WorkingDir))
                setDefaultdirectory();

            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(CommonUtils.WorkingDir))
                    fbd.SelectedPath = CommonUtils.WorkingDir;
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    CommonUtils.WorkingDir = fbd.SelectedPath;
                    CommonUtils.IsDirectoryChanged = true;
                    workingFiles = Directory.GetFiles(CommonUtils.WorkingDir, "*.*", SearchOption.AllDirectories).ToList();
                    UnakinLogger.LogInfo(String.Concat("Working directory changed to - ", CommonUtils.WorkingDir));

                    setWorkingDirDisplay();

                }
            }
            btnSelect.ToolTip = "Selected Directory: " + CommonUtils.WorkingDir;

        }
        private void setDefaultdirectory()
        {
            try
            {
                ShellInterop.IVsSolution solution = (ShellInterop.IVsSolution)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(ShellInterop.IVsSolution));
                solution.GetSolutionInfo(out string solutionDirectory, out string solutionName, out string solutionDirectory2);
                if (!string.IsNullOrEmpty(solutionDirectory))
                {
                    CommonUtils.WorkingDir = solutionDirectory;
                    UnakinLogger.LogInfo(String.Concat("Working directory set to - ", solutionDirectory));
                }

            }
            catch { /*Do Nothing*/}
        }
        private async void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (button != null)
            {
                // Temporarily change the button's opacity to make it appear darker
                button.Opacity = 0.5;

                // Wait for 1 second
                await System.Threading.Tasks.Task.Delay(1000);

                // Revert the button's opacity back to fully opaque
                button.Opacity = 1.0;
            }

            int index = (int)button.Tag;
            TerminalWindowHelper.Copy(button, currentVM.ChatItems[index].Document.Text);

        }

        private async void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            // Assuming setup similar to your original method
            Button button = (Button)sender;

            if (button != null)
            {
                // Temporarily change the button's opacity to make it appear darker
                button.Opacity = 0.5;

                // Wait for 1 second
                await System.Threading.Tasks.Task.Delay(1000);

                // Revert the button's opacity back to fully opaque
                button.Opacity = 1.0;
            }

            int index = (int)button.Tag;
            string newText = currentVM.ChatItems[index].Document.Text; // The text you want to replace with

            await TerminalWindowHelper.ReplaceSelectedTextInActiveDocumentAsync(newText);
        }

        private async void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (button != null)
            {
                // Temporarily change the button's opacity to make it appear darker
                button.Opacity = 0.5;

                // Wait for 1 second
                await System.Threading.Tasks.Task.Delay(1000);

                // Revert the button's opacity back to fully opaque
                button.Opacity = 1.0;
            }

            int index = (int)button.Tag;
            string codeToDisplay = currentVM.ChatItems[index].Document.Text;

            // Create the window instance
            CodeDisplayWindow displayWindow = new CodeDisplayWindow();

            // Set the code text to be displayed
            displayWindow.SetCodeText(codeToDisplay);

            // Show the window
            displayWindow.Show();
        }


        private async void btnAction_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string[] tag = ((string)button.Tag).Split('|');
            int index = 0;
            int wait = -1;
            var chatItems = chatList.ItemsSource as List<ChatItemDTO>;

            if (tag[0] == "P")
            {
                var isEdit = true;
                if (int.TryParse(tag[1], out index))
                {
                    isEdit = chatItems[index].ActionImageSource.EndsWith("edit.png");
                    if (isEdit)
                    {
                        chatItems[index].ActionImageSource = "pack://application:,,,/Unakin;component/Resources/OkBlue.png";
                        chatItems[index].ActionButtonTooltip = "Send";
                        chatList.Items.Refresh();

                    }
                }
                if (isEdit)
                {
                    await System.Threading.Tasks.Task.Delay(200).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() =>
                        {

                            var lvItem = chatList.ItemContainerGenerator.Items[index];
                            var parent = chatList.ItemContainerGenerator.ContainerFromItem(lvItem);
                            var txtbox = CommonUtils.GetVisualChildInDataTemplate<ICSharpCode.AvalonEdit.TextEditor>(parent);

                            if (txtbox == null)
                                return;
                            if (String.IsNullOrEmpty(txtbox.Tag.ToString()))
                                return;
                            if (!txtbox.Tag.ToString().StartsWith("P|"))
                                return;

                            txtbox.Focus();
                            txtbox.IsReadOnly = false;
                            txtbox.Background = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                            txtbox.KeyDown += new KeyEventHandler(MoniterRefresh);
                        });
                    });
                }
                else
                {
                    var parent = CommonUtils.GetAncestorOfType<Grid>(sender as Button);
                    var txtbox = CommonUtils.GetVisualChildInDataTemplate<ICSharpCode.AvalonEdit.TextEditor>(parent);
                    SendPromptAsync(txtbox, null);
                }
            }

            if (tag[0] == "R" || tag[0] == "C")
            {
                if (int.TryParse(tag[1], out index))
                {
                    string prompt = string.Empty;

                    for (int ctr = index; ctr >= 0; ctr--)
                    {
                        if (chatItems[ctr].ActionButtonTag.StartsWith("P"))
                        {
                            prompt = chatItems[ctr].Document.Text;
                            if (prompt != string.Empty)
                            {
                                break;
                            }
                        }
                        chatItems.RemoveAt(ctr);
                        await currentVM.DeleteChatAsync(ctr);
                    }
                    if (prompt != string.Empty)
                    {
                        chatItems.RemoveAll(x => x.Index >= index);
                        await currentVM.DeleteChatAsync(index);
                        EnableDisableButtons(false);

                        if (chatItems.Count > 0 && chatItems[chatItems.Count - 1].ActionButtonTag.StartsWith("P|"))
                        {
                            chatItems.RemoveAll(x => x.Index == chatItems.Count - 1);
                            await currentVM.DeleteChatAsync(index - 1);
                        }
                        cancellationTokenSource = new CancellationTokenSource();
                        await currentVM.GetLLMResponseAsync(prompt, null, cancellationTokenSource.Token);
                    }
                }
            }
        }
        async void MoniterRefresh(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && (e.Key == Key.Enter))
            {
                SendPromptAsync(sender, e);
            }
        }
        async void SendPromptAsync(object sender, KeyEventArgs e)
        {
            var chatItems = chatList.ItemsSource as List<ChatItemDTO>;
            try
            {
                isEditRunning = true;
                var textbox = (ICSharpCode.AvalonEdit.TextEditor)sender;
                string[] tag = ((string)textbox.Tag).Split('|');

                int index = 0;
                if (tag[0] == "P")
                {
                    if (int.TryParse(tag[1], out index))
                    {
                        string prompt = textbox.Text; ;
                        if (prompt != string.Empty)
                        {
                            chatItems.RemoveAll(x => x.Index >= index);
                            await currentVM.DeleteChatAsync(index);
                            EnableDisableButtons(false);

                            if (index > 0 && chatItems[index - 1].ActionButtonTag.StartsWith("P|"))
                            {
                                chatItems.RemoveAll(x => x.Index == index - 1);
                                await currentVM.DeleteChatAsync(index - 1);
                            }

                            cancellationTokenSource = new CancellationTokenSource();
                            await currentVM.GetLLMResponseAsync(prompt, null, cancellationTokenSource.Token);
                        }
                    }
                }
                txtRequest.Focus();
            }
            finally
            {
                isEditRunning = false;
                string[] tag;
                if (sender.GetType() == typeof(Button))
                {
                    Button button = (Button)sender;
                    tag = ((string)button.Tag).Split('|');
                }
                else
                {
                    ICSharpCode.AvalonEdit.TextEditor editor = (ICSharpCode.AvalonEdit.TextEditor)sender;
                    tag = ((string)editor.Tag).Split('|');
                }

                int index = 0;
                if (int.TryParse(tag[1], out index))
                {
                    chatItems[index].ActionImageSource = "pack://application:,,,/Unakin;component/Resources/edit.png";
                }
            }
        }
        void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
        }
        private void chatItem_LostFocus(object sender, RoutedEventArgs e)
        {
            var txtbox = (ICSharpCode.AvalonEdit.TextEditor)sender;
            if (txtbox == null)
                return;
            if (String.IsNullOrEmpty(txtbox.Tag.ToString()))
                return;
            if (!txtbox.Tag.ToString().StartsWith("P|"))
                return;

            txtbox.IsReadOnly = true;
            txtbox.Background = new SolidColorBrush(Color.FromRgb(153, 187, 255));
            txtbox.KeyDown -= new KeyEventHandler(MoniterRefresh);
        }
        private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        /*
        private void ToggleSettingsPanel()
        {
            // Assuming the settingsPanel's width is 250
            double panelWidth = 250; // Update this if your panel width is different
            double hiddenPosition = panelWidth; // Position when the panel is hidden
            double visiblePosition = 0; // Position when the panel is visible

            // Determine the target position based on the current state
            double targetPosition = settingsPanelTransform.X == visiblePosition ? hiddenPosition : visiblePosition;

            var animation = new DoubleAnimation
            {
                To = targetPosition,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase()
            };

            settingsPanelTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
        

        private void openOptions(object sender, RoutedEventArgs e)
        {
            ToggleSettingsPanel();
        }

        */

        private void openOptions(object sender, RoutedEventArgs e)
        {
            UnakinPackage.Instance.ShowOptionPage(typeof(OptionPageGridGeneral));
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Example implementation - replace with your actual logic
            MessageBox.Show("Settings saved!");

            // Assuming you have UsernameTextBox and PasswordBox defined in your XAML,
            // you might want to save their values here.
            // For example:
            // var username = UsernameTextBox.Text;
            // var password = PasswordBox.Password;
            // Save username and password settings...

            // After saving, you might want to hide the settings panel
            // settingsPanel.Visibility = Visibility.Collapsed;
        }
        private async System.Threading.Tasks.Task GetChatResponseAsync()
        {
            Dictionary<string, string> filesContentMap = new Dictionary<string, string>();
            var options = UnakinPackage.Instance.OptionsGeneral;

            try
            {
                if (isEditRunning)
                {
                    isEditRunning = false;
                    return;
                }

                if (!await AuthHelper.ValidateAPIAsync())
                {
                    return;
                }

                shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string task = txtRequest.Text;
                string summary = "";

                if (contextFlag)
                {
                    if (string.IsNullOrEmpty(CommonUtils.WorkingDir))
                    {
                        MessageBox.Show("Please select a working directory first.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                        // Optionally, invoke a method that allows the user to select a directory
                        // For example, you might call a method here that opens a FolderBrowserDialog or sets CommonUtils.WorkingDir
                        return; // Exit the method as we cannot proceed without a directory
                    }

                    // Collect .cs files from the directory and subdirectories
                    var files = Directory.GetFiles(CommonUtils.WorkingDir, "*.cs", SearchOption.AllDirectories)
                        .Select(filePath => new
                        {
                            Path = filePath,
                            Content = File.ReadAllText(filePath)
                        })
                        .ToList();

                    // Assuming SendInitialFilesMessageAsync is designed to take a directory path and a dictionary of file paths and their content
                    filesContentMap = files.ToDictionary(file => file.Path, file => file.Content);

                    // Construct a prompt for the summary of the full project
                    string promptForSummary = "Provide a summary of the following project based on its code files. Also give me the most relevant code snippets for thisrequest: " + txtRequest.Text;
                    EnableDisableButtons(false);
                    // Concatenate the contents for the summary request. Note: Adjust this part based on how the server expects to receive the content
                    foreach (var fileContent in filesContentMap.Values)
                    {
                        promptForSummary += fileContent + "\n\n"; // Simple concatenation, consider a more sophisticated method
                    }
                    cancellationTokenSource = new CancellationTokenSource();
                    // Make a call to the server for a summary of the full project
                    //summary = await currentVM.GetLLMResponseAsync(promptForSummary, null, cancellationTokenSource.Token);
                    
                    summary = await GetResponseFromWebSocketSummary(promptForSummary);
                    
                    // Append the generated summary to the task
                    task = "This is the project summary: " + summary + ". " + " \n\n Considering this summary and relevant snippets of code please do the following: "+txtRequest.Text;
                }

                EnableDisableButtons(false);
                txtRequest.Text = string.Empty;
                cancellationTokenSource = new CancellationTokenSource();
                await currentVM.GetLLMResponseAsync(task, null, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    EnableDisableButtons(true); // Enable UI elements in case of exception
                });
                UnakinLogger.HandleException(ex);
            }
        }

        private async System.Threading.Tasks.Task GetAgentsResponseAsync()
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            if (!await AuthHelper.ValidateAPIAsync())
            {
                hasAPIAuthFailed = true;
                return;
            }

            if (hasAPIAuthFailed == true)
            {
                currentVM.RefreshChat(currentVM.ChatType);
                hasAPIAuthFailed = false;
            }

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string combinedResponse = txtRequest.Text;
                currentVM.AppendUserInput(combinedResponse);
                txtRequest.Text = string.Empty;
                EnableDisableButtons(false);

                // Process the response for agent interaction if valid
                try
                {
                    foreach (var agent in Agents)
                    {
                        if (!agent.Active)
                            continue;

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                            continue;

                        EnableDisableButtons(false);
                        string requestWithAgentFunctionality = agent.Functionality + Environment.NewLine + combinedResponse;
                        cancellationTokenSource = new CancellationTokenSource();
                        combinedResponse = await currentVM.GetLLMAgentResponseAsync(requestWithAgentFunctionality, agent, cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }


                chatList.Items.Refresh();
                scrollViewer.ScrollToEnd();
                EnableDisableButtons(true);
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
        private async System.Threading.Tasks.Task GetAutonomousAgentResponseAsync()
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            var orchestrator_agent = new UnakinShared.DTO.AgentDTO { Name = "Orchestrator Agent" };
            var engineer_agent = new UnakinShared.DTO.AgentDTO { Name = "Engineer Agent" };
            string initialRequest = ""; // Add this to store the initial request

            try
            {
                while (true) // Changed to an indefinite loop, will break out on user satisfaction or cancellation
                {
                 

                    if (isEditRunning == true)
                    {
                        isEditRunning = false;
                        return;
                    }

                    if (!await AuthHelper.ValidateAPIAsync())
                    {
                        return;
                    }

                    shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                    // Only fetch and clear the initial request if it's not already set (i.e., not regenerating)
                    if (string.IsNullOrWhiteSpace(initialRequest) && !string.IsNullOrWhiteSpace(txtRequest.Text))
                    {
                        initialRequest = txtRequest.Text;
                    }
                    else if (string.IsNullOrWhiteSpace(initialRequest))
                    {
                        MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    string task = initialRequest;
                    EnableDisableButtons(false);
                    txtRequest.Text = ""; // Clearing txtRequest as part of UI state management
                    cancellationTokenSource = new CancellationTokenSource();
                    string promptAppend = "Please provide the steps solely related to scripting in Unity-C# that I need to follow. Each instruction should be clear and concise, focused only on the scripting tasks within my IDE. Avoid mentioning interactions with Unity's graphical interface or any external software. Instructions should not include non-scripting tasks like file creation since I am already using my IDE. Ensure that each step is meant for a separate script. Do not give me code just instructions and do not add any text or sentece beyond the intructions.";
                    string request = $"I need a list of step-by-step scripting instructions for Unity-C#, specifically tailored for my IDE. The task at hand is: {task}. {promptAppend}";

                    //string stepsResponse = await currentVM.GetLLMResponseAsync(request, orchestrator_agent, cancellationTokenSource.Token);
                    string stepsResponse = await ChatGPT.GetResponseAsync(UnakinPackage.Instance.OptionsGeneral, string.Empty, request, UnakinPackage.Instance.OptionsGeneral.StopSequences.Split(','), cancellationTokenSource.Token); ;
                    
                    //MessageBoxResult userDecision = MessageBox.Show("Do you want to proceed with these steps, regenerate them, or quit?\n\nProceed: Click 'Yes'\nRegenerate: Click 'No'\nQuit: Click 'Cancel'", Constants.EXTENSION_NAME, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    
                    var stepsDialog = new StepsDialog();
                    stepsDialog.SetStepsText(stepsResponse); // Set the steps text
                    var dialogResult = stepsDialog.ShowDialog();

                    EnableDisableButtons(false);

                
                    if (stepsDialog.DialogState == "Proceed")
                    {
                        // Process the stepsResponse
                        string[] separators = stepsResponse.Contains("\n\n") ? new[] { "\n\n" } : new[] { "\n" };
                        string[] steps = stepsResponse.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var step in steps)
                        {
                            if (cancellationTokenSource.Token.IsCancellationRequested)
                                break;
                            await currentVM.GetLLMResponseAsync(step + " Give me the script in Unity-C#.", engineer_agent, cancellationTokenSource.Token);
                            EnableDisableButtons(false);
                        }

                        // Exit the loop on user satisfaction
                        break;
                    }
                    else if (stepsDialog.DialogState == "Regenerate")
                    {
                        // The loop continues, effectively restarting the process
                        continue;
                    }
                    else if (stepsDialog.DialogState == "Quit")
                    {
                        EnableDisableButtons(true); // Prepare UI for quit state
                                                    // Safety net or cleanup actions before exiting
                        return; // Exit the function or handling block
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    EnableDisableButtons(true); // Re-enable UI elements in case of exception
                });
                UnakinLogger.HandleException(ex);
            }
            finally
            {
                // Reset initial request for future invocations if needed
                initialRequest = "";
            }
            EnableDisableButtons(true);
        }



        public async System.Threading.Tasks.Task RequestToWindowAsync(string command, string selectedText)
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            try
            {
                if (!await AuthHelper.ValidateAPIAsync())
                {
                    hasAPIAuthFailed = true;
                    return;
                }

                if (hasAPIAuthFailed == true)
                {
                    currentVM.RefreshChat(currentVM.ChatType);
                    hasAPIAuthFailed = false;
                }

                currentVM.ChatType = TurboChatHelper.GetChatType(command);
                //currentVM.RefreshChat(currentVM.ChatType);

                // Combine command and selected text into a single prompt
                
                //string fullPrompt = $"{command}\n{"```"}{selectedText}".Trim();
                string fullPrompt = $"{command}\n{"```"}{selectedText}";
                

                if (string.IsNullOrWhiteSpace(fullPrompt))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await GetIDEResponseAsync(fullPrompt);
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                EnableDisableButtons(true);
            }
        }
        public async System.Threading.Tasks.Task GetIDEResponseAsync(string prompt)
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            if (string.IsNullOrEmpty(prompt))
                return;
            EnableDisableButtons(false);
            cancellationTokenSource = new CancellationTokenSource();
            await currentVM.GetLLMResponseAsync(prompt, null, cancellationTokenSource.Token);
        }
        public async System.Threading.Tasks.Task GetSemanticResponseAsync(string prompt)
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            if (!await ValidateConfig(true))
                return;

            EnableDisableButtons(false);
            cancellationTokenSource = new CancellationTokenSource();
            await currentVM.GetLLMResponseAsync(prompt, null, cancellationTokenSource.Token);
            EnableDisableButtons(true);
        }

        public async System.Threading.Tasks.Task GetProjectSummResponseAsync(string prompt)
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            if (!await ValidateConfig(false, false))
                return;

            EnableDisableButtons(false);
            cancellationTokenSource = new CancellationTokenSource();

            using (var folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = folderBrowserDialog1.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath))
                {
                    // Include C++ files in the search pattern
                    string[] csharpFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.cs", SearchOption.AllDirectories);
                    string[] cppFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.cpp", SearchOption.AllDirectories);
                    string[] headerFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.h", SearchOption.AllDirectories);

                    // Combine file arrays
                    string[] files = csharpFiles.Concat(cppFiles).Concat(headerFiles).ToArray();
                    StringBuilder allContents = new StringBuilder();

                    foreach (var file in files)
                    {
                        allContents.AppendLine(File.ReadAllText(file));
                    }
                   prompt = String.Concat("Summarize contents for folder - ", folderBrowserDialog1.SelectedPath, "Summarize the content of these scripts:\n" + allContents.ToString());
                   await currentVM.GetLLMResponseAsync(prompt, null, cancellationTokenSource.Token);
                        
                }
            }

            EnableDisableButtons(true);
        }
        public async System.Threading.Tasks.Task GetAutomatedTestingResponseAsync(string prompt)
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            if (!await ValidateConfig(false, false))
                return;

            EnableDisableButtons(false);
            cancellationTokenSource = new CancellationTokenSource();

            using (var folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = folderBrowserDialog1.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath))
                {
                    // Include C++ files in the search pattern
                    string[] csharpFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.cs", SearchOption.AllDirectories);
                    string[] cppFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.cpp", SearchOption.AllDirectories);
                    string[] headerFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.h", SearchOption.AllDirectories);

                    // Combine file arrays
                    string[] files = csharpFiles.Concat(cppFiles).Concat(headerFiles).ToArray();

                    foreach (var file in files)
                    {
                        string response = string.Empty;

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            break; // To exit from the outer loop
                        }

                        try
                        {
                            string content = File.ReadAllText(file);
                            StringBuilder sbPrompt = new StringBuilder();
                            //sbPrompt.AppendLine("Code:");
                            //sbPrompt.AppendLine(Constants.GENERIC_DELIMETER);
                            sbPrompt.AppendLine("Write unit tests for the following code: ");
                            //sbPrompt.AppendLine(Constants.GENERIC_DELIMETER);
                            sbPrompt.AppendLine(content);
                            
                            await currentVM.GetLLMResponseAsync(sbPrompt.ToString(), null, cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                                EnableDisableButtons(true); // Enable UI elements in case of exception
                            });
                            UnakinLogger.HandleException(ex);
                        }

                    }

                }
            }

            EnableDisableButtons(true);
        }
        public async System.Threading.Tasks.Task GetDataGenerationResponseAsync()
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            if (!await ValidateConfig(false, false))
                return;

            EnableDisableButtons(false);
            cancellationTokenSource = new CancellationTokenSource();

            // Updated to use the new dialog that also asks for the number of rows
            var columnInputDialog = new Unakin.ColumnInputDialog("Enter column headers separated by commas");
            if (columnInputDialog.ShowDialog() == true)
            {
                EnableDisableButtons(false);
                List<string> columnHeaders = columnInputDialog.ColumnHeaders;
                int columns = columnHeaders.Count;
                int rows = columnInputDialog.Rows; // Get the number of rows directly from the dialog now

                StringBuilder resultBuilder = new StringBuilder();
                var quotedColumnHeaders = columnHeaders.Select(header => "\"" + header.Replace("\"", "\"\"") + "\"");
                resultBuilder.AppendLine(string.Join(",", quotedColumnHeaders));

                // Initialize a list to hold the data for all columns
                List<List<string>> columnsData = new List<List<string>>();

                foreach (string columnHeader in columnHeaders)
                {
                    // Craft the prompt for the server
                    string prompt = $"You are creating well-ordered sets of data for our customers. Give me data now for {rows} {columnHeader}. Remember I only wanted a numbered list, no other information.";

                    try
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                            break;

                        //string serverResponse = await currentVM.GetLLMResponseAsync(prompt, null, cancellationTokenSource.Token);
                        string serverResponse = await GetResponseFromWebSocket(prompt);
                        // Parse the server response to extract the numbered list
                        List<string> columnData = serverResponse.Split('\n')
                                                                 .Where(line => char.IsDigit(line.FirstOrDefault()))
                                                                 .Select(line => "\"" + line.Substring(line.IndexOf('.') + 1).Trim().Replace("\"", "\"\"") + "\"")
                                                                 .ToList();

                        // Ensure the list has exactly 'rows' elements, trimming or padding as necessary
                        if (columnData.Count > rows)
                            columnData = columnData.Take(rows).ToList();
                        else while (columnData.Count < rows)
                                columnData.Add("\"\"");

                        columnsData.Add(columnData);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        UnakinLogger.HandleException(ex);
                        break;
                    }
                }

                // Construct the table rows from the columnsData
                for (int row = 0; row < rows; row++)
                {
                    List<string> rowValues = columnsData.Select(columnData => columnData[row]).ToList();
                    resultBuilder.AppendLine(string.Join(",", rowValues));
                }

                
                TableDisplayWindow previewWindow = new TableDisplayWindow();
                previewWindow.DisplayData(resultBuilder.ToString()); // Implement this method in DataPreviewWindow to display the data

                previewWindow.ShowDialog();
                
            }

            EnableDisableButtons(true);
        }



        private async System.Threading.Tasks.Task<string> GetResponseFromWebSocketSummary(string prompt)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(10); // Example: 10 seconds timeout for inactivity
            StringBuilder responseBuilder = new StringBuilder();

            DateTime lastReceived = DateTime.Now; // Timestamp of the last received data

            void HandleContent(string content)
            {
                responseBuilder.Append(content);
                lastReceived = DateTime.Now; // Update the timestamp when new data is received
            }

            System.Threading.Tasks.Task responseTask = Unakin.Utils.Unakin.CallWebSocketSingleAnswer(UnakinPackage.Instance.OptionsGeneral, prompt, HandleContent, CancellationToken.None, "big-very-expensive");

            // Loop until the task is completed or inactivity timeout is reached
            while (!responseTask.IsCompleted)
            {
                if (DateTime.Now - lastReceived > inactivityTimeout)
                {
                    // If inactivity timeout is reached, consider the task complete
                    break;
                }

                await System.Threading.Tasks.Task.Delay(500); // Wait for a short period before checking again
            }

            return responseBuilder.ToString();
        }

        private async System.Threading.Tasks.Task<string> GetResponseFromWebSocketAutonomousAgents(string prompt)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            StringBuilder responseBuilder = new StringBuilder();

            DateTime lastReceived = DateTime.Now; // Timestamp of the last received data

            void HandleContent(string content)
            {
                responseBuilder.Append(content);
                lastReceived = DateTime.Now; // Update the timestamp when new data is received
            }

            System.Threading.Tasks.Task responseTask = Unakin.Utils.Unakin.CallWebSocketAutonomousAgents(UnakinPackage.Instance.OptionsGeneral, prompt, HandleContent, CancellationToken.None, "big-very-expensive");

            // Loop until the task is completed or inactivity timeout is reached
            while (!responseTask.IsCompleted)
            {
                if (DateTime.Now - lastReceived > inactivityTimeout)
                {
                    // If inactivity timeout is reached, consider the task complete
                    break;
                }

                await System.Threading.Tasks.Task.Delay(500); // Wait for a short period before checking again
            }

            return responseBuilder.ToString();
        }

        private async System.Threading.Tasks.Task<string> GetResponseFromWebSocket(string prompt)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(5); // Example: 10 seconds timeout for inactivity
            StringBuilder responseBuilder = new StringBuilder();

            DateTime lastReceived = DateTime.Now; // Timestamp of the last received data

            void HandleContent(string content)
            {
                responseBuilder.Append(content);
                lastReceived = DateTime.Now; // Update the timestamp when new data is received
            }

            System.Threading.Tasks.Task responseTask = Unakin.Utils.Unakin.CallWebSocket(UnakinPackage.Instance.OptionsGeneral, prompt, HandleContent, CancellationToken.None, "big-very-expensive");

            // Loop until the task is completed or inactivity timeout is reached
            while (!responseTask.IsCompleted)
            {
                if (DateTime.Now - lastReceived > inactivityTimeout)
                {
                    // If inactivity timeout is reached, consider the task complete
                    break;
                }

                await System.Threading.Tasks.Task.Delay(500); // Wait for a short period before checking again
            }

            return responseBuilder.ToString();
        }



        // Note: You'll need to create or adjust dialogs such as `ColumnInputDialog` and `SimpleInputDialog`
        // to collect user inputs as required above.


        #region Event - Agent
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
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is AgentDTO agent)
            {
                agent.Active = chk.IsChecked.Value;
                agent.IsDirty = true;
                SaveAgentsAsync();
            }
        }

        /*
        private void ChatType_Changed(object sender, RoutedEventArgs e)
        {
            if (ShowAgents.IsChecked == true)
            {
                currentVM.ChatType = ChatType.Agents;
                scrollAgent.Visibility = Visibility.Visible;
                RefreshAgentsAsync();
                lvAgents.ItemsSource = Agents;
            }
            else
            {
                currentVM.ChatType = ChatType.Chat;
                scrollAgent.Visibility = Visibility.Collapsed;
            }
            currentVM.RefreshChat(currentVM.ChatType);
        }
        */

        private void ChatType_Changed(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton.IsChecked == true)
            {

                currentVM.ChatType = ChatType.Agents;
                AgentHeader.Visibility = Visibility.Visible;
                scrollAgent.Visibility = Visibility.Visible;
                // Ensure the tab and toggle button are visible
                GeneralAgentWorkflowTab.Visibility = Visibility.Visible; // Make sure this matches your control's name

                RefreshAgentsAsync();
                lvAgents.ItemsSource = Agents;
                CheckboxAgentsVisible.IsChecked = true;
            }
            else 
            {
                currentVM.ChatType = ChatType.Chat;
                AgentHeader.Visibility = Visibility.Collapsed;
                scrollAgent.Visibility = Visibility.Collapsed;
                // Hide the tab when the Agents hub is deactivated
                GeneralAgentWorkflowTab.Visibility = Visibility.Collapsed; // Make sure this matches your control's name
                                                                           // Reset checkboxes to initial state
                //CheckboxAgentsVisible.IsChecked = true; // Set to your initial value
                //CheckboxAgentsHidden.IsChecked = true; // Set to your initial value

            }
            currentVM.RefreshChat(currentVM.ChatType);
        }


        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (checkBox == null) return;

            if (checkBox.Name == "CheckboxAgentsVisible")
            {
                CheckboxAgentsHidden.IsChecked = false;
                AgentHeader.Visibility = Visibility.Visible;
                scrollAgent.Visibility = Visibility.Visible;
                GeneralAgentWorkflowTab.Visibility = Visibility.Visible;
                currentVM.ChatType = ChatType.Agents;
            }
            else if (checkBox.Name == "CheckboxAgentsHidden")
            {
                CheckboxAgentsVisible.IsChecked = false;
                AgentHeader.Visibility = Visibility.Collapsed;
                scrollAgent.Visibility = Visibility.Collapsed;
                // Keep the GeneralAgentWorkflowTab visible or hide based on your requirement
                currentVM.ChatType = ChatType.AutonomousAgent;
                OverlayContainer.Visibility = Visibility.Visible;
                StartTextStreaming("Let's conquer the world!");
                HideAfterDelay();
            }
        }

        private void Checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Optionally handle the unchecked state if needed, for example:
            // If both are unchecked, decide if you want to hide or show the agents and scroll view.
            // This is more relevant if you allow both checkboxes to be unchecked at the same time.

        }

        private void Context_Checked(object sender, RoutedEventArgs e)
        {
            contextFlag = true;
        }

        private void Context_Unchecked(object sender, RoutedEventArgs e)
        {
            contextFlag = false;
        }

        private void StartImageAnimation()
        {
            MyImageControl.Visibility = Visibility.Visible;

            var widthAnimation = new DoubleAnimation
            {
                From = 100, // Initial width
                To = 300, // Final width
                Duration = TimeSpan.FromSeconds(2)
            };

            var heightAnimation = new DoubleAnimation
            {
                From = 100, // Initial height
                To = 300, // Final height
                Duration = TimeSpan.FromSeconds(2)
            };

            MyImageControl.BeginAnimation(WidthProperty, widthAnimation);
            MyImageControl.BeginAnimation(HeightProperty, heightAnimation);
        }

        private void StartTextStreaming(string text)
        {
            MyTextBlock.Visibility = Visibility.Visible;
            MyTextBlock.Text = string.Empty;
            int currentIndex = 0;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Tick += (sender, args) =>
            {
                if (currentIndex < text.Length)
                {
                    MyTextBlock.Text += text[currentIndex++];
                }
                else
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private void HideAfterDelay()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) }; // Adjust time as necessary
            timer.Tick += (sender, args) =>
            {
                timer.Stop(); // Stop the timer to prevent it from ticking again
                OverlayContainer.Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }


        #endregion

        #endregion

        #region Private Methods

        private void EnableDisableButtons(bool enable)
        {
            Dispatcher.Invoke(() =>
            {
                grdProgress.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
                //btnClear.IsEnabled = enable;
                //btnRequestCode.IsEnabled = enable;
                btnRequestSend.IsEnabled = enable;
                btnCancel.IsEnabled = !enable;
                //btnClear.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                //btnRequestCode.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                btnRequestSend.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                btnCancel.Visibility = !enable ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void RefreshChatItem()
        {
            chatList.Items.Refresh();
            scrollViewer.ScrollToEnd();
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
                                string response = await ChatGPT.GetResponseAsync(UnakinPackage.Instance.OptionsGeneral, string.Empty, requestWithAgentFunctionality, UnakinPackage.Instance.OptionsGeneral.StopSequences.Split(','), cancellationTokenSource.Token);

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

        void setWorkingDirDisplay()
        {
            if (string.IsNullOrEmpty(CommonUtils.WorkingDir))
            {
                btnSelect.ToolTip = string.Empty;
                return;
            }

            string[] splits = CommonUtils.WorkingDir.Split('\\');
            if (splits.Length > 3)
            {
                btnSelect.ToolTip = CommonUtils.WorkingDir;
            }
            else
            {
                btnSelect.ToolTip = CommonUtils.WorkingDir;
            }
        }

        async Task<bool> ValidateConfig(bool validateQuery = true, bool validateDirectory = true)
        {
            if (!await AuthHelper.ValidateAPIAsync())
                return false;

            var options = UnakinPackage.Instance.OptionsGeneral;

            if (string.IsNullOrWhiteSpace(options.ProjectName))
            {
                await CommonUtils.ShowErrorAsync(Unakin.Utils.Constants.MESSAGE_SET_PROJECT_NAME);
                UnakinPackage.Instance.ShowOptionPage(typeof(OptionPageGridGeneral));
                return false;
            }
            else
            {
                CommonUtils.ProjectName = options.ProjectName;
            }

            if (validateQuery)
                if (String.IsNullOrEmpty(txtRequest.Text))
                {
                    await CommonUtils.ShowErrorAsync("Please enter prompt!");
                    return false;
                }
            if (validateDirectory)
                if (String.IsNullOrEmpty(CommonUtils.WorkingDir))
                {
                    await CommonUtils.ShowErrorAsync("Please select working directory!");
                    return false;
                }

            return true;
        }

        #endregion

        private void ToggleSettingsPanel(object sender, RoutedEventArgs e)
        {
            settingsPopup.IsOpen = !settingsPopup.IsOpen;

            // Only populate the settings panel if we're opening it.
            if (settingsPopup.IsOpen)
            {
                PopulateSettingsPanel();
            }
        }


        //private OptionPageGridGeneral options; // Assume this is properly instantiated or accessed

        private void PopulateSettingsPanel()
        {
            var options = UnakinPackage.Instance.OptionsGeneral;
            // Assuming options is your OptionPageGridGeneral instance loaded with current settings
            var border = settingsPopup.Child as Border;
            if (border != null)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(0xA0, 0x28, 0x28, 0x28));

                var stackPanel = border.Child as StackPanel;
                if (stackPanel != null)
                {
                    stackPanel.Children.Clear(); // Clear existing controls

                    // Add "Settings" title at the top with white foreground
                    var settingsTitle = new TextBlock
                    {
                        Text = "Settings",
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 20,
                        Margin = new Thickness(5) // Adjust margin as needed
                    };
                    stackPanel.Children.Add(settingsTitle);

                    // Dynamically add UI elements for each setting
                    stackPanel.Children.Add(CreateSettingsEntry("Username", options.UserName));
                    stackPanel.Children.Add(CreateSettingsEntry("Password", "", isPassword: true)); // Password not pre-filled for security
                    stackPanel.Children.Add(CreateSettingsEntry("Project Name", options.ProjectName));
                    stackPanel.Children.Add(CreateSettingsEntryForLLMProvider("LLM Provider", options.Service));
                    stackPanel.Children.Add(CreateSettingsEntry("OpenAI API Key", options.OpenAIApiKey));
                    // Inside PopulateSettingsPanel method, add the model language ComboBox
                    stackPanel.Children.Add(CreateSettingsEntryForEnum("OpenAI Model", options.Model));
                    // Inside PopulateSettingsPanel method, add the LLM provider ComboBox

                    // Add Save Settings Button
                    var saveButton = new Button { Content = "Save Settings", Margin = new Thickness(5) };
                    saveButton.Click += SaveSettingsButton_Click;
                    stackPanel.Children.Add(saveButton);
                }
            }
        }



        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var optionPage = UnakinPackage.Instance.OptionsGeneral;

            var border = settingsPopup.Child as Border;
            if (border != null)
            {
                var stackPanel = border.Child as StackPanel;
                if (stackPanel != null)
                {
                    foreach (var container in stackPanel.Children.OfType<StackPanel>())
                    {
                        foreach (var control in container.Children)
                        {
                            if (control is TextBox textBox && textBox.Tag is string tag)
                            {
                                switch (tag)
                                {
                                    case "Username":
                                        optionPage.UserName = textBox.Text;
                                        Properties.Settings.Default.Username = textBox.Text;
                                        Properties.Settings.Default.Save();
                                        break;
                                    case "Project Name":
                                        optionPage.ProjectName = textBox.Text;
                                        break;
                                    case "OpenAI API Key":
                                        optionPage.OpenAIApiKey = textBox.Text;
                                        break;
                                        // Handle other TextBox fields as necessary
                                }
                            }
                            else if (control is PasswordBox passwordBox && passwordBox.Tag as string == "Password")
                            {
                                if (passwordBox.Password != "*****") // Check if the placeholder text is not the current value
                                {
                                    optionPage.Password = passwordBox.Password; // Only update if the user entered a new password
                                    Properties.Settings.Default.Password = passwordBox.Password;
                                    Properties.Settings.Default.Save();

                                }
                                // If the placeholder text is still there, don't update the password.
                            }

                            else if (control is ComboBox comboBox)
                            {
                                if (comboBox.Tag as string == "OpenAI Model" && comboBox.SelectedItem is ComboBoxItem modelSelectedItem && modelSelectedItem.Tag is ModelLanguageEnum modelSelectedValue)
                                {
                                    optionPage.Model = modelSelectedValue; // Save Model Language
                                }
                                else if (comboBox.Tag as string == "LLM Provider" && comboBox.SelectedItem is ComboBoxItem llmSelectedItem && llmSelectedItem.Tag is OpenAIService llmSelectedValue)
                                {
                                    optionPage.Service = llmSelectedValue; // Save LLM Provider
                                }
                            }
                        }
                    }

                    // Optionally, invoke a method to explicitly save or apply changes to optionPage
                    settingsPopup.IsOpen = false; // Close the popup after saving
                }
            }
 
            RefreshSettingsUI();
            optionPage.SaveSettingsToStorage();// or similar method if needed
        }
        private void RefreshSettingsUI()
        {
            PopulateSettingsPanel(); // Or a more specific method to reload and update UI elements
        }


        private UIElement CreateSettingsEntry(string labelContent, string initialValue, bool isPassword = false)
        {
            var optionPage = UnakinPackage.Instance.OptionsGeneral;

            var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            var label = new TextBlock { Text = labelContent, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.White), Width = 120 };
            container.Children.Add(label);

            if (labelContent == "Model Language")
            {
                var comboBox = new ComboBox { Width = 200, Tag = labelContent };
                foreach (ModelLanguageEnum value in Enum.GetValues(typeof(ModelLanguageEnum)))
                {
                    var enumType = typeof(ModelLanguageEnum);
                    var memberInfos = enumType.GetMember(value.ToString());
                    var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
                    var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(EnumStringValueAttribute), false);
                    var description = ((EnumStringValueAttribute)valueAttributes[0]).Value;

                    comboBox.Items.Add(new ComboBoxItem { Content = description, Tag = value });
                    if (description == initialValue)
                    {
                        comboBox.SelectedItem = comboBox.Items[comboBox.Items.Count - 1];
                    }
                }
                container.Children.Add(comboBox);
            }
            else if (isPassword)
            {
                var passwordBox = new PasswordBox { Width = 200, Tag = labelContent };
                // Set the placeholder password (assuming you have a way to check if a password exists)
                if (!string.IsNullOrEmpty(optionPage.Password)) // Check if an actual password is stored
                {
                    passwordBox.Password = "*****"; // Placeholder text indicating a password is set
                }
                container.Children.Add(passwordBox);
            }

            else
            {
                var textBox = new TextBox { Width = 200, Text = initialValue, Tag = labelContent };
                container.Children.Add(textBox);
            }

            return container;
        }

        private UIElement CreateSettingsEntryForEnum(string labelContent, ModelLanguageEnum initialValue)
        {
            var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            var label = new TextBlock { Text = labelContent, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.White), Width = 120 };
            container.Children.Add(label);

            var comboBox = new ComboBox { Width = 200, Tag = labelContent };
            foreach (var value in Enum.GetValues(typeof(ModelLanguageEnum)))
            {
                var memInfo = typeof(ModelLanguageEnum).GetMember(value.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(EnumStringValueAttribute), false);

                if (attributes.Length > 0) // Check if the attribute exists
                {
                    var description = ((EnumStringValueAttribute)attributes[0]).Value;
                    var comboItem = new ComboBoxItem { Content = description, Tag = value };
                    comboBox.Items.Add(comboItem);

                    if ((ModelLanguageEnum)value == initialValue)
                    {
                        comboBox.SelectedItem = comboItem;
                    }
                }
                else
                {
                    // Handle the case where the attribute is not found
                    // For example, you can use the enum's name as a fallback
                    var comboItem = new ComboBoxItem { Content = value.ToString(), Tag = value };
                    comboBox.Items.Add(comboItem);

                    if ((ModelLanguageEnum)value == initialValue)
                    {
                        comboBox.SelectedItem = comboItem;
                    }
                }
            }

            container.Children.Add(comboBox);

            return container;
        }

        private UIElement CreateSettingsEntryForLLMProvider(string labelContent, OpenAIService initialValue)
        {
            var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            var label = new TextBlock { Text = labelContent, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.White), Width = 120 };
            container.Children.Add(label);

            var comboBox = new ComboBox { Width = 200, Tag = labelContent };
            foreach (var value in Enum.GetValues(typeof(OpenAIService)))
            {
                var memInfo = typeof(OpenAIService).GetMember(value.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(EnumStringValueAttribute), false);

                string description = value.ToString(); // Default to the enum name
                if (attributes.Length > 0)
                {
                    description = ((EnumStringValueAttribute)attributes[0]).Value;
                }

                var comboItem = new ComboBoxItem { Content = description, Tag = value };
                comboBox.Items.Add(comboItem);

                if ((OpenAIService)value == initialValue)
                {
                    comboBox.SelectedItem = comboItem;
                }
            }

            container.Children.Add(comboBox);

            return container;
        }
        

        [AttributeUsage(AttributeTargets.Field)]
        public class EnumStringValueAttribute : Attribute
        {
            public string Value { get; private set; }

            public EnumStringValueAttribute(string value)
            {
                Value = value;
            }
        }


        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            // Implement your logic for when a toggle is turned on
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            // Implement your logic for when a toggle is turned off
        }

        private void TxtRequest_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true; // Prevent further processing, avoiding a newline in the editor

                // Check if the sendCommand can execute and then execute it
                var sendCommand = this.Resources["sendCommand"] as RoutedUICommand;
                if (sendCommand != null && sendCommand.CanExecute(null, this))
                {
                    sendCommand.Execute(null, this);
                }
            }
        }




    }
}

