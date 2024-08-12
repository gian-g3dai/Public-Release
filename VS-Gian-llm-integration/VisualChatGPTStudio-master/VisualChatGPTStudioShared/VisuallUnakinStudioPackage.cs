using Community.VisualStudio.Toolkit;
using Unakin.Commands;
using Unakin.Options;
using Unakin.ToolWindows;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unakin.Utils;
using Unakin.Options.Commands;
using UnakinShared.Utils;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace Unakin
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.UnakinString)]
    [ProvideOptionPage(typeof(OptionPageGridGeneral), "Visual Unakin Studio", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionPageGridGeneral), "Visual Unakin Studio", "General", 0, 0, true)]
    //[ProvideOptionPage(typeof(OptionPageGridCommands), "Visual Unakin Studio", "Commands", 1, 1, true)]
    [ProvideProfile(typeof(OptionPageGridCommands), "Visual Unakin Studio", "Commands", 1, 1, true)]
    //[ProvideToolWindow(typeof(SemanticSearchUnakin), Orientation = ToolWindowOrientation.Right, Window = EnvDTE.Constants.vsWindowKindOutput, Style = VsDockStyle.Tabbed,Width =100)]
    //[ProvideToolWindow(typeof(TerminalWindow), Orientation = ToolWindowOrientation.Right, Window = EnvDTE.Constants.vsWindowKindOutput, Style = VsDockStyle.Tabbed, Width = 100)]
    [ProvideToolWindow(typeof(ChatToolWindow), Orientation = ToolWindowOrientation.Right, Window = EnvDTE.Constants.vsWindowKindOutput, Style = VsDockStyle.Tabbed, Width = 100)]
    //[ProvideToolWindow(typeof(AgentToolWindow), Orientation = ToolWindowOrientation.Right, Window = EnvDTE.Constants.vsWindowKindOutput, Style = VsDockStyle.Tabbed, Width = 100)]
    public sealed class UnakinPackage : ToolkitPackage
    {
        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static UnakinPackage Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public ChatControl TurboWindowInstance
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the DB Instance.
        /// </summary>
        public AppDatabase AppData
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Gets the OptionPageGridGeneral object.
        /// </summary>
        public OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return (OptionPageGridGeneral)GetDialogPage(typeof(OptionPageGridGeneral));
            }
        }

        /// <summary>
        /// Gets the OptionPageGridCommands object.
        /// </summary>
        public OptionPageGridCommands OptionsCommands
        {
            get
            {
                return (OptionPageGridCommands)GetDialogPage(typeof(OptionPageGridCommands));
            }
        }

        ///// <summary>
        ///// Gets the OptionPageGridCommands object.
        ///// </summary>
        //public OptionCommands OptionsCommands
        //{
        //    get
        //    {
        //        return (OptionCommands)GetDialogPage(typeof(OptionCommands));
        //    }
        //}

        /// <summary>
        /// Initializes the terminal window commands.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //ClearCredentialsForTesting();

            Logger.Initialize(this, Unakin.Utils.Constants.EXTENSION_NAME); 
            Instance = this;

            AppData = new AppDatabase();
            await AppData.UpdateDatabase();

            await this.RegisterCommandsAsync();
            //await TerminalWindowCommand.InitializeAsync(this);
            await TerminalWindowTurboCommand.InitializeAsync(this);
            //await SemanticSearchUnakinCommand.InitializeAsync(this);
            //await Unakin.Commands.AutomatedTesting.InitializeAsync(this);
            //await Unakin.Commands.ProjectSummary.InitializeAsync(this);
            //await Unakin.Commands.WebSearch.InitializeAsync(this);
            await Unakin.Commands.Tutorial.InitializeAsync(this);
            //ShowLoginPromptIfNeeded();
            //await Unakin.Commands.AgentsUnakinCommand.InitializeAsync(this);

            ChangeHighlighting();

            // Ensure we are on the main thread (UI thread) and then show the login prompt
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Wait for the solution to be fully loaded before showing the login prompt
            /*
            KnownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(async () =>
            {
                ShowLoginPromptIfNeeded();
            });
            */
        }

        static void  ChangeHighlighting()
        { 
            var highLighter = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
            highLighter.GetNamedColor("String").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#D69973"));
            highLighter.GetNamedColor("Char").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FFFFEF00"));
            highLighter.GetNamedColor("Preprocessor").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FFFF8080"));
            highLighter.GetNamedColor("Punctuation").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            highLighter.GetNamedColor("ValueTypeKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#5198D5"));
            highLighter.GetNamedColor("ReferenceTypeKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#5198D5"));
            highLighter.GetNamedColor("MethodCall").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#DCDBA7"));
            highLighter.GetNamedColor("NumberLiteral").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FFFFCF00"));
            highLighter.GetNamedColor("ThisOrBaseReference").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#5198D5"));
            highLighter.GetNamedColor("NullOrValueKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF0080FF"));
            highLighter.GetNamedColor("Keywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF00E0FF"));
            highLighter.GetNamedColor("GotoKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF00E0FF"));
            highLighter.GetNamedColor("ContextKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF0080FF"));
            highLighter.GetNamedColor("ExceptionKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#5198D5"));
            highLighter.GetNamedColor("CheckedKeyword").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF0040FF"));
            highLighter.GetNamedColor("UnsafeKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF00C0FF"));
            highLighter.GetNamedColor("OperatorKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF00E0FF"));
            highLighter.GetNamedColor("ParameterModifiers").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF0080FF"));
            highLighter.GetNamedColor("Modifiers").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#5198D5"));
            highLighter.GetNamedColor("Visibility").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#5198D5"));
            highLighter.GetNamedColor("NamespaceKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF0040FF"));
            highLighter.GetNamedColor("GetSetAddRemove").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF00C0FF"));
            highLighter.GetNamedColor("TrueFalse").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF0040FF"));
            highLighter.GetNamedColor("TypeKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#FF00C0FF"));
            highLighter.GetNamedColor("SemanticKeywords").Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString("#D39FDF"));
        }


        //Login Block
        public void ShowLoginPromptIfNeeded()
        {
            if (!CredentialsExist())
            {
                LoginWindow loginWindow = new LoginWindow();
                bool? dialogResult = loginWindow.ShowDialog();

                if (dialogResult == true)
                {
                    // User clicked "Login" and dialog closed
                    StoreCredentials(loginWindow.Username, loginWindow.Password);
                    // Assuming you have a method to decrypt and apply credentials to OptionPageGridGeneral
                    UpdateOptionPageGridWithCredentials();
                }
            }
            else
            {
                // Credentials exist, decrypt and set them
                UpdateOptionPageGridWithCredentials();
            }
        }

        private void UpdateOptionPageGridWithCredentials()
        {
            var optionPage = Unakin.UnakinPackage.Instance.OptionsGeneral;
            if (optionPage != null)
            {
                // Assuming you have methods to retrieve these values directly since encryption isn't used
                optionPage.UserName = Properties.Settings.Default.Username;
                optionPage.Password = Properties.Settings.Default.Password;
            }
        }

        private bool CredentialsExist()
        {
            // Check if both username and password have been saved in the settings
            // Replace with your actual settings keys if they are different
            return !string.IsNullOrEmpty(Properties.Settings.Default.Username) && !string.IsNullOrEmpty(Properties.Settings.Default.Password);
        }

        public void StoreCredentials(string username, string password)
        {
            // Save the credentials directly into the application's settings
            Properties.Settings.Default.Username = username;
            Properties.Settings.Default.Password = password;
            Properties.Settings.Default.Save(); // Make sure to save the changes
        }

        public void ClearCredentialsForTesting()
        {
            Properties.Settings.Default.Username = string.Empty;
            Properties.Settings.Default.Password = string.Empty;
            Properties.Settings.Default.Save(); // Save the changes immediately
        }


    }


}