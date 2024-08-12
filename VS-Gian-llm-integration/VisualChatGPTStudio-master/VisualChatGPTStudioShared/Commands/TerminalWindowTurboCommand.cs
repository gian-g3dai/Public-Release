using Unakin.ToolWindows;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Unakin.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TerminalWindowTurboCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0706;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1e5e84f8-48b1-4c7e-8634-13a1fc0bd1ba");

        private static ChatToolWindow window;
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowTurboCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TerminalWindowTurboCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            CommandID menuCommandID = new CommandID(CommandSet, CommandId);
            MenuCommand menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TerminalWindowTurboCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
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
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in TerminalWindowTurboCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new TerminalWindowTurboCommand(package, commandService);

            await InitializeToolWindowAsync(package);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            _ = this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                await InitializeToolWindowAsync(this.package);
            });
        }

        /// <summary>
        /// Initializes the ToolWindow with the specified <paramref name="package"/>. 
        /// </summary>
        /// <param name="package">The AsyncPackage to be initialized.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async System.Threading.Tasks.Task InitializeToolWindowAsync(AsyncPackage package)
        {
            ToolWindowPane windowPane = await package.ShowToolWindowAsync(typeof(ChatToolWindow), 0, true, package.DisposalToken);

            if ((null == windowPane) || (null == windowPane.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            window = (ChatToolWindow)windowPane;
            window.SetTerminalWindowProperties(((UnakinPackage)package).OptionsGeneral, package);
        }


        public async System.Threading.Tasks.Task RequestToWindowAsync(string command, string selectedText)
        {
            if (window == null)
            {
                throw new Exception("Please, open the tool window first.");
            }
            ((IVsWindowFrame)window.Frame).Show();
            await window.RequestToWindowAsync(command, selectedText);
        }


    }
}
