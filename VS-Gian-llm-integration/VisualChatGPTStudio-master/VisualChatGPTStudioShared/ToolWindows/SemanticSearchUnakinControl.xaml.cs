using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Shell.Interop;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Community.VisualStudio.Toolkit;
using Unakin.Options;
using OpenAI_API.Chat;
using Unakin.Commands;
using UnakinShared.Utils;
using Microsoft.Internal.VisualStudio.Shell;
using Constants = Unakin.Utils.Constants;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnakinShared.Utils;
using UnakinShared.DTO;
using Unakin.Options;

namespace Unakin.ToolWindows
{
    /// <summary>
    /// Interaction logic for SemanticSearchUnakinControl.
    /// </summary>
    public partial class SemanticSearchUnakinControl : UserControl
    {
        private OptionPageGridGeneral options;
        private Package package;
        private Conversation chat;
        private List<ChatItemDTO> chatItems;
        private CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool selectedContextFilesCodeAppended = false;
        private SyncWatcher syncWatcher;
        ResponseDTO responseDTO;
        ObservableCollection<Response> lstResponses;
        internal SemanticSearchServerHelper serverHelper;

        internal List<String> workingFiles = new List<String>();
        string userToken;
        bool isSyncing = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticSearchToolControl"/> class.
        /// </summary>
        public SemanticSearchUnakinControl()
        {
            this.InitializeComponent();
        }

        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            this.options = options;
            this.package = package;
            CommonUtils.UserName = options.UserName;
            CommonUtils.Password = options.Password;

            //TODO - Working directory not setting at this point
            setDefaultdirectory();
            setWorkingDirDisplay();
            CommonUtils.IsDirectoryChanged = true;
            serverHelper = new SemanticSearchServerHelper();
            syncWatcher = new SyncWatcher(this);
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
                    workingFiles = Directory.GetFiles(CommonUtils.WorkingDir, "*.*",SearchOption.AllDirectories).ToList(); 
                    UnakinLogger.LogInfo(String.Concat("Working directory changed to - ", CommonUtils.WorkingDir));

                    setWorkingDirDisplay();

                }
            }

        }


        /// <summary>
        /// Sync
        /// </summary>
        public async void Sync(Object sender, RoutedEventArgs e)
        {
            var syncButton = (Button)sender;

            if (!isSyncing)
            {
                if (await ValidateConfig(false) == false)
                    return;
                else
                {
                    lblError.Content = "";
                    lblError.Visibility = Visibility.Hidden;
                }

                syncWatcher.AddFileWatch(CommonUtils.WorkingDir);
                syncButton.Content = "Syncing...";
                syncButton.ToolTip = "Stop Syncing...";
                isSyncing = true;
                UnakinLogger.LogInfo("Started Syncing...");

            }
            else
            {
                syncWatcher.RemoveFileWatch();
                syncButton.Content = "Sync";
                syncButton.ToolTip = "Start Syncing...";
                isSyncing = false;
                UnakinLogger.LogInfo("Stopped Syncing...");
            }

        }

        /// <summary>
        /// Options
        /// </summary>
        public void openOptions(Object sender, RoutedEventArgs e)
        {
            UnakinPackage.Instance.ShowOptionPage(typeof(OptionPageGridGeneral));
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

        void setWorkingDirDisplay()
        {
            if (string.IsNullOrEmpty(CommonUtils.WorkingDir))
                return;

            string[] splits = CommonUtils.WorkingDir.Split('\\');
            if (splits.Length > 3)
            {
                lblName.Text = splits[0] + "\\" + splits[1] + "\\...\\" + splits[splits.Length - 2] + "\\" + splits[splits.Length - 1];
                lblName.ToolTip = CommonUtils.WorkingDir;
            }
            else
            {
                lblName.Text = CommonUtils.WorkingDir;
                lblName.ToolTip = CommonUtils.WorkingDir;
            }

            if (lblError.Content != null && lblError.Content.ToString() == Constants.MESSAGE_SELECTDIR)
            {
                lblError.Content = string.Empty;
            }
        }

        public override void EndInit()
        {
            base.EndInit();
        }

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void SendRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            if (await ValidateConfig() == false)
                return;
            else
            {
                lblError.Content = "";
                lblError.Visibility = Visibility.Hidden;
            }

            if (lstResponses != null)
                lstResponses.Clear();
            else
                lstResponses = new ObservableCollection<Response>();
            try
            {
                EnableDisableButtons(false, true);
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
                    responseDTO = await this.serverHelper.SendSearchCodeBlocksMessageAsync(txtRequest.Text, cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {    
                UnakinLogger.LogError("Error while processing request");
                UnakinLogger.HandleException(ex);
            }
            finally
            {
                EnableDisableButtons(true, false);
            }


            if (responseDTO != null)
            {
                try
                {
                    if (lstResponses != null)
                        lstResponses.Clear();
                    else
                        lstResponses = new ObservableCollection<Response>();

                    foreach (var res in responseDTO.results.hits)
                    {
                        var response = new Response();
                        response.Doc = new ICSharpCode.AvalonEdit.Document.TextDocument();
                        response.Doc.Text = res.body;
                        if (res.full_path != null)
                        {
                            var path1 = res.full_path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                            response.FilePath = CommonUtils.WorkingDir;
                            foreach (var str in path1)
                            {
                                response.FilePath += "\\" + str;

                                if (str.Contains("."))
                                    break;
                            }
                        }
                        lstResponses.Add(response);
                    }

                    lstResponseCtr.ItemsSource = lstResponses;

                }
                catch (Exception ex)
                {
                    UnakinLogger.LogError("Error while sending request");
                    UnakinLogger.HandleException(ex);
                }
            }
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public void CancelRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            EnableDisableButtons(true, false);
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
            if (lstResponses != null)
                lstResponses.Clear();
        }

        /// <summary>
        /// Copy the content of the Response TextBox to the Clipboard.
        /// </summary>
        /// <param name="sender">The button that invokes the event.</param>
        /// <param name="e">Event arguments.</param>
        private void btnResponseCopy_Click(object sender, RoutedEventArgs e)
        {
            var parent = CommonUtils.GetAncestorOfType<Border>(sender as Button);
            var txtbox = CommonUtils.GetVisualChildInDataTemplate<ICSharpCode.AvalonEdit.TextEditor>(parent);
            CommonUtils.Copy((Button)sender, txtbox.Document.Text);
        }

        /// <summary>
        /// Copy the content of the Response TextBox to the Clipboard.
        /// </summary>
        /// <param name="sender">The button that invokes the event.</param>
        /// <param name="e">Event arguments.</param>
        private async void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var parent = CommonUtils.GetAncestorOfType<Border>(sender as Button);
            var txtbox = CommonUtils.GetVisualChildInDataTemplate<Label>(parent);

            await VS.Documents.OpenAsync(txtbox.Content.ToString());

        }

        /// <summary>
        /// Open Response in Ginie Window.
        /// </summary>
        /// <param name="sender">The button that invokes the event.</param>
        /// <param name="e">Event arguments.</param>
        private async void btnOpenGinie_Click(object sender, RoutedEventArgs e)
        {
            var parent = CommonUtils.GetAncestorOfType<Border>(sender as Button);
            var txtbox = CommonUtils.GetVisualChildInDataTemplate<ICSharpCode.AvalonEdit.TextEditor>(parent);

            IVsUIShell vsUIShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
            Guid guid = typeof(ChatToolWindow).GUID;
            IVsWindowFrame windowFrame;
            int result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref guid, out windowFrame);   // Find MyToolWindow

            if (result != VSConstants.S_OK)
            {
                await TerminalWindowTurboCommand.InitializeAsync(UnakinPackage.Instance);
                result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref guid, out windowFrame);   // Find MyToolWindow
            }
            //result = vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guid, out windowFrame); // Crate MyToolWindow if not found

            if (result == VSConstants.S_OK)                                                                           // Show MyToolWindow
                ErrorHandler.ThrowOnFailure(windowFrame.Show());

            UnakinPackage.Instance.TurboWindowInstance.txtRequest.Text = txtbox.Document.Text;
            UnakinPackage.Instance.TurboWindowInstance.txtRequest.SyntaxHighlighting = txtbox.SyntaxHighlighting;
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
                await CommonUtils.ShowErrorAsync(Unakin.Utils.Constants.MESSAGE_SET_PROJECT_NAME);
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
                    lblError.Content = "Pleae enter a prompt!";
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
    }
}