using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Unakin.Utils;
using Task = System.Threading.Tasks.Task;
using Unakin.Options;
using Constants = Unakin.Utils.Constants;
using Span = Microsoft.VisualStudio.Text.Span;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualBasic;
using System.Windows.Input;
using OpenAI_API.Chat;
using System.Drawing;
using System.Text;

namespace Unakin.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    public class Tutorial
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0903;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1e5e84f8-48b1-4c7e-8634-13a1fc0bd1ba");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tutorial"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Tutorial(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Tutorial Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        /// 
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((UnakinPackage)this.package).OptionsGeneral;
            }
        }
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in UnitTesting's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Tutorial(package, commandService);
        }

        private void ShowTutorialWindow()
        {
            // Create and configure the tutorial form
            Form tutorialForm = new Form()
            {
                Width = 1300,
                Height = 600,
                Text = "Tutorial"
            };

            // Use RichTextBox for formatted tutorial text
            RichTextBox tutorialRichText = new RichTextBox()
            {
                Dock = DockStyle.Fill,
                Text = @"
                To effectively leverage the Unakin extension in Visual Studio for enhanced coding efficiency, follow these steps:

                1. **Registration and Installation:**
                - Create an account .

                2. **Accessing Unakin Chat:**
                - Right-click in the IDE and select 'Unakin Chat' to open the tool window where you can interact with Unakin.

                3. **Utilizing Special Commands:**
                - Enter '//Search in Code' to find code snippets, '//Project Summary' to summarize projects, and '//Automated Testing' to generate unit tests.

                4. **Enhancing Code with Editor Features:**
                - Right-click a code snippet and select from options like Complete, Add Tests, Find Bugs, Optimize, etc., to directly interact with your code through Unakin.

                5. **Interacting with the Agent Hub:**
                - Toggle 'Agent Hub' to 'On' to use agents for scripting assistance, and apply agent steps to scripts in a local folder with '//Local Workflow'.

                6. **Authentication and Settings Adjustment:**
                - Enter your login credentials in the settings section, accessible by clicking 'Settings' in the Unakin Chat window.

                For a full guide, visit the documentation.
                ",

                // Enable basic formatting and scrolling
                Multiline = true,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                ReadOnly = true  // Make it read-only if you don't want users to edit it
            };

            // Set some basic formatting
            tutorialRichText.SelectAll();
            tutorialRichText.SelectionFont = new Font("Consolas", 10, FontStyle.Regular);
            tutorialRichText.DeselectAll();

            // Add the RichTextBox to the form
            tutorialForm.Controls.Add(tutorialRichText);

            // Show the form as a dialog
            tutorialForm.ShowDialog();
        }




        private async void Execute(object sender, EventArgs e)
        {
            // Show the tutorial window first
            ShowTutorialWindow();

            // Existing code...
            // Rest of the Execute method code goes here...
        }


    }
}
