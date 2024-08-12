using Community.VisualStudio.Toolkit;
using EnvDTE;
using Unakin.Options;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using OpenAI_API.Moderation;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using Constants = Unakin.Utils.Constants;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using UnakinShared.Utils;
using System.Threading.Tasks;
using UnakinShared.DTO;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using OpenAI_API.Chat;

namespace Unakin.ToolWindows
{
    /// <summary>
    /// Interaction logic for TerminalWindowControl.
    /// </summary>
    public partial class TerminalWindowControl : UserControl
    {
        ResponseDTO responseDTO;
        internal SemanticSearchServerHelper serverHelper;
        private Conversation chat;
        private List<ChatItemDTO> chatItems;
        private bool hasAPIAuthFailed = false;

        #region Properties

        private OptionPageGridGeneral options;
        private Package package;
        private bool firstIteration;
        private bool responseStarted;
        private bool replaceInContext;
        private bool replaceStart;
        private bool replaceEnd;
        Timer ServerStatusTimer;

        private CancellationTokenSource cancellationTokenSource;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowControl"/> class.
        /// </summary>
        public TerminalWindowControl()
        {
            this.InitializeComponent();
            serverHelper = new SemanticSearchServerHelper();
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        /// 
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((UnakinPackage)this.package).OptionsGeneral;
            }
        }
        public async void SendRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            if (await ValidateConfig() == false)
            {
                hasAPIAuthFailed = true;
                return;
            }
            else
            {
                lblError.Content = "";
                lblError.Visibility = Visibility.Hidden;
            }
            if (hasAPIAuthFailed == true)
            {
                chat = ChatGPT.CreateConversation(options);
                hasAPIAuthFailed = false;
            }

            try
            {
                firstIteration = true;
                responseStarted = false;

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    await CommonUtils.ShowErrorAsync(Constants.MESSAGE_WRITE_REQUEST);
                    return;
                }

                EnableDisableButtons(false, true);

                txtResponse.Text = string.Empty;

                cancellationTokenSource = new CancellationTokenSource();

                if (UnakinPackage.Instance.OptionsGeneral.Service == OpenAIService.OpenAI)
                {
                    await ChatGPT.GetResponseAsync(options, string.Empty, txtRequest.Text, options.StopSequences.Split(','), ResultHandler, cancellationTokenSource.Token);
                }
                else
                {
                    await Unakin.Utils.Unakin.CallWebSocket(OptionsGeneral, txtRequest.Text, ResultHandler, cancellationTokenSource.Token, "Window Command");
                }
            }

            catch (OperationCanceledException)
            {
                EnableDisableButtons(true, false);
            }
            catch (Exception ex)
            {
                EnableDisableButtons(true, false);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void InContextRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            if (await ValidateConfig() == false)
            {
                hasAPIAuthFailed = true;
                return;
            }
            else
            {
                lblError.Content = "";
                lblError.Visibility = Visibility.Hidden;
            }
            if (hasAPIAuthFailed == true)
            {
                chat = ChatGPT.CreateConversation(options);
                hasAPIAuthFailed = false;
            }


            try
            {
                firstIteration = true;
                responseStarted = false;
                EnableDisableButtons(false, true);
                txtResponse.Text = string.Empty;

                if (await this.serverHelper.ConnectAsync())
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    if (CommonUtils.IsDirectoryChanged == true)
                    {
                        if (string.IsNullOrEmpty(CommonUtils.WorkingDir))
                            setDefaultdirectory();

                        var files = Directory.GetFiles(CommonUtils.WorkingDir, "*", SearchOption.AllDirectories).Select(x => Path.GetFullPath(x)).ToList();
                        if (await this.serverHelper.SendInitialFilesMessageAsync(CommonUtils.WorkingDir, files, cancellationTokenSource.Token))
                        {
                            CommonUtils.IsDirectoryChanged = false;
                        }
                    }
                    responseDTO = await this.serverHelper.SendSearchCodeBlocksMessageAsync(txtRequest.Text,cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                EnableDisableButtons(true, false);
                UnakinLogger.LogError("Error while processing request");
                UnakinLogger.HandleException(ex);
            }


            if (responseDTO != null && responseDTO.results != null && responseDTO.results.hits.Count > 0)
            {
                chatItems = new List<ChatItemDTO>();
                var sb = new StringBuilder();
                try
                {
                    sb.AppendLine(String.Concat("Considering that there are these C# scripts in the local project. Each script starts at " , Constants.startComment, " and ends at ", Constants.endComment, "."));
                    sb.AppendLine(String.Concat("Please send releted code script enclosed between ", Constants.startComment,"  and ", Constants.endComment," comments in response."));
                    foreach (var res in responseDTO.results.hits)
                    {
                        sb.AppendLine(Constants.startComment);
                        sb.AppendLine(string.Empty);
                        sb.Append(res.body);
                        sb.AppendLine(string.Empty);
                        sb.AppendLine(Constants.endComment);

                        sb.AppendLine(string.Empty);

                    }

                    sb.AppendLine(txtRequest.Text);

                    //string request = options.MinifyRequests ? TextFormat.MinifyText(sb.ToString()) : sb.ToString();
                    //request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));
                    //chat.AppendUserInput(request);

                    txtResponse.Text = string.Empty;
                    cancellationTokenSource = new CancellationTokenSource();
                    replaceInContext = true;
                    await Unakin.Utils.Unakin.CallWebSocket(options, sb.ToString(), ResultHandler, cancellationTokenSource.Token, "InContext");
                }
                catch (Exception ex)
                {
                    EnableDisableButtons(true, false);
                    UnakinLogger.LogError("Error while sending request");
                    UnakinLogger.HandleException(ex);
                }
            }
            else
            {
                EnableDisableButtons(true, false);
                txtResponse.Text = Constants.MESSAGE_NORESPONSE;
            }
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public async void CancelRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            btnCancel.IsEnabled = false;

            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Handles the Click event of the btnRequestPast control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        [STAThread]
        private void btnRequestPast_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                txtRequest.AppendText(Clipboard.GetText());
            }
        }

        /// <summary>
        /// Clears the request text document.
        /// </summary>
        /// <param name="sender">The button click sender.</param>
        /// <param name="e">Routed event args.</param>
        private void btnRequestClear_Click(object sender, RoutedEventArgs e)
        {
            txtRequest.Text = string.Empty;
        }

        /// <summary>
        /// Copy the content of the Response TextBox to the Clipboard.
        /// </summary>
        /// <param name="sender">The button that invokes the event.</param>
        /// <param name="e">Event arguments.</param>
        private void btnResponseCopy_Click(object sender, RoutedEventArgs e)
        {
            TerminalWindowHelper.Copy((Button)sender, txtResponse.Text);
        }

        /// <summary>
        /// This method changes the syntax highlighting of the textbox based on the language detected in the text.
        /// </summary>
        private void txtRequest_TextChanged(object sender, EventArgs e)
        {
            txtRequest.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtRequest.Text));
        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            this.options = options;
            this.package = package;

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(5);
            #pragma warning disable VSTHRD101 // Avoid unsupported async delegates
            ServerStatusTimer = new System.Threading.Timer(async (e) =>
            {
                var isServerUp = await Unakin.Utils.Unakin.IsServerUpAsync();
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (isServerUp)
                    {
                        indicatorUp.Visibility = Visibility.Visible;
                        indicatorDown.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        indicatorUp.Visibility = Visibility.Hidden;
                        indicatorDown.Visibility = Visibility.Visible;
                    }
                }));

            }, null, startTimeSpan, periodTimeSpan);
            #pragma warning restore VSTHRD101 // Avoid unsupported async delegates
        }

        /// <summary>
        /// Handles the result of an operation and appends it to the end of the txtResponse control.
        /// </summary>
        /// <param name="result">The result of the operation.</param>
        private async void ResultHandler(string result)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (firstIteration)
            {
                EnableDisableButtons(true, true);

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_RECEIVING_UNAKIN, 2, 2);

                firstIteration = false;
                responseStarted = false;
            }

            if (!responseStarted && (result.Equals("\n") || result.Equals("\r") || result.Equals(Environment.NewLine)))
            {
                //Do nothing when API send only break lines on response begin
                return;
            }

            responseStarted = true;

            txtResponse.AppendText(result);

            txtResponse.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtResponse.Text));

            txtResponse.ScrollToEnd();

            if (replaceInContext)
            {
                var inContextReplace = String.Concat("(Working Directory - ", CommonUtils.WorkingDir, ")");
                txtResponse.Text = inContextReplace + Environment.NewLine + Environment.NewLine + txtResponse.Text;
                replaceInContext = false;
                replaceStart=true; 
            }

            if (replaceStart)
            {
                if (txtResponse.Text.Contains(Constants.startComment))
                {
                    txtResponse.Text = txtResponse.Text.Replace(Constants.startComment, string.Empty);
                    replaceStart = false;
                    replaceEnd = true;
                }
            }

            if (replaceEnd)
            {
                if (txtResponse.Text.Contains(Constants.endComment))
                {
                    txtResponse.Text = txtResponse.Text.Replace(Constants.endComment, string.Empty);
                    replaceEnd = false;
                }
            }
        }

        /// <summary>
        /// Requests to the chatGPT window with the given command and selected text.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        /// <param name="selectedText">The selected text to be sent.</param>
        public async System.Threading.Tasks.Task RequestToWindowAsync(string command, string selectedText)
        {
            try
            {
                firstIteration = true;

                string result = "";

                await VS.StatusBar.ShowProgressAsync("Requesting Unakin", 1, 2);

                EnableDisableButtons(false, true);

                txtRequest.Text = command + Environment.NewLine + Environment.NewLine + selectedText;

                txtResponse.Text = string.Empty;

                cancellationTokenSource = new CancellationTokenSource();

                if (UnakinPackage.Instance.OptionsGeneral.Service == OpenAIService.OpenAI)
                {
                    if (options.SingleResponse)
                    {
                        result = await ChatGPT.GetResponseAsync(options, command, selectedText, options.StopSequences.Split(','), cancellationTokenSource.Token);
                        ResultHandler(result);
                    }
                    else
                    {
                        await ChatGPT.GetResponseAsync(options, command, selectedText, options.StopSequences.Split(','), ResultHandler, cancellationTokenSource.Token);
                    }
                }
                else
                {
                    void HandleContent(string content)
                    {
                        ResultHandler(content);// Process each content piece
                    }

                    string task = command + selectedText;
                    var cancellationToken = new CancellationToken();
                    await Unakin.Utils.Unakin.CallWebSocket(OptionsGeneral, task, HandleContent, cancellationToken, command);

                    //ResultHandler(result);
                }
                
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true, false);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                EnableDisableButtons(true, false);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Enables or disables the send button and cancel button based on the provided parameters.
        /// </summary>
        /// <param name="enableSendButton">A boolean value indicating whether to enable or disable the send button.</param>
        /// <param name="enableCancelButton">A boolean value indicating whether to enable or disable the cancel button.</param>
        private void EnableDisableButtons(bool enableSendButton, bool enableCancelButton)
        {
            grdProgress.Visibility = enableSendButton ? Visibility.Collapsed : Visibility.Visible;

            btnRequestSend.IsEnabled = enableSendButton;
            btnCancel.IsEnabled = enableCancelButton;
        }

        async Task<bool> ValidateConfig(bool validateQuery = true)
        {
            if (!await AuthHelper.ValidateAPIAsync())
                return false;

            if (string.IsNullOrWhiteSpace(options.ProjectName))
            {
                await VS.MessageBox.ShowAsync(Unakin.Utils.Constants.EXTENSION_NAME, Unakin.Utils.Constants.MESSAGE_SET_PROJECT_NAME, buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                UnakinPackage.Instance.ShowOptionPage(typeof(OptionPageGridGeneral));
                return false;
            }
            else
            {
                CommonUtils.ProjectName = options.ProjectName;
                serverHelper.ProjectName = CommonUtils.ProjectName;
            }

            if (validateQuery)
                if (String.IsNullOrEmpty(txtRequest.Text))
                {
                    lblError.Content = "Pleae ask valid question!";
                    lblError.Visibility = Visibility.Visible;
                    return false;
                }

            if (String.IsNullOrEmpty(CommonUtils.WorkingDir))
            {
                lblError.Content = Constants.MESSAGE_SELECTDIR;
                lblError.Visibility = Visibility.Visible;
                return false;
            }
            return true;
        }
        private void setDefaultdirectory()
        {
            try
            {
                IVsSolution solution = (IVsSolution)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(IVsSolution));
                solution.GetSolutionInfo(out string solutionDirectory, out string solutionName, out string solutionDirectory2);
                if (!string.IsNullOrEmpty(solutionDirectory))
                {
                    CommonUtils.WorkingDir = solutionDirectory;
                    UnakinLogger.LogInfo(String.Concat("Working directory set to - ", solutionDirectory));
                }

            }
            catch { /*Do Nothing*/}
        }
        #endregion Methods        
    }
}