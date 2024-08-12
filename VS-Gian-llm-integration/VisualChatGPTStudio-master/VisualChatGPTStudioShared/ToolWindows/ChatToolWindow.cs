using Unakin.Options;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System;

namespace Unakin.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("fa0ff3cd-c95a-4898-9d29-2d8f8f672965")]
    public class ChatToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatToolWindow"/> class.
        /// </summary>
        public ChatToolWindow() : base(null)
        {
            this.Caption = "Unakin-Chat";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new ChatControl();
        }

        /// <summary>
        /// Sets the terminal window properties.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void SetTerminalWindowProperties(OptionPageGridGeneral options, Package package)
        {
            ((ChatControl)this.Content).StartControl();
        }

        public async System.Threading.Tasks.Task RequestToWindowAsync(string command, string selectedText)
        {
            await ((ChatControl)this.Content).RequestToWindowAsync(command, selectedText);
        }
    }
}
