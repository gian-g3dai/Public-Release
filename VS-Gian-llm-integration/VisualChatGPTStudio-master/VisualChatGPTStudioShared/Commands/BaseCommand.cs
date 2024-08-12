using Community.VisualStudio.Toolkit;
using EnvDTE;
using Unakin.Options;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;
using Constants = Unakin.Utils.Constants;
using Unakin.Utils;

namespace Unakin.Commands
{
    /// <summary>
    /// Base abstract class for commands
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    internal abstract class BaseCommand<TCommand> : Community.VisualStudio.Toolkit.BaseCommand<TCommand> where TCommand : class, new()
    {
        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return ((UnakinPackage)this.Package).CancellationTokenSource;
            }
            set
            {
                ((UnakinPackage)this.Package).CancellationTokenSource = value;
            }
        }

        /// <summary>
        /// Gets the OptionsGeneral property of the UnakinPackage.
        /// </summary>
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((UnakinPackage)this.Package).OptionsGeneral;
            }
        }

        /// <summary>
        /// Gets the OptionsCommands property of the UnakinPackage.
        /// </summary>
        protected OptionPageGridCommands OptionsCommands
        {
            get
            {
                return ((UnakinPackage)this.Package).OptionsCommands;
            }
        }
        

        /// <summary>
        /// Gets the DTE object.
        /// </summary>
        /// <returns>The DTE object.</returns>
        protected async System.Threading.Tasks.Task<DTE> GetDTEAsync()
        {
            return await VS.GetServiceAsync<DTE, DTE>();
        }

        /// <summary>
        /// Validates the code selected by the user.
        /// </summary>
        /// <param name="selectedCode">The selected code.</param>
        /// <returns>True if the code is valid, false otherwise.</returns>
        protected async Task<bool> ValidateCodeSelectedAsync(string selectedCode)
        {
            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "Please select the code.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes the blank lines from the begin of result.
        /// </summary>
        /// <param name="result">The string to remove blank lines from.</param>
        /// <returns>The string with blank lines removed.</returns>
        protected string RemoveBlankLinesFromResult(string result)
        {
            while (result.StartsWith("\r\n") || result.StartsWith("\n") || result.StartsWith("\r"))
            {
                if (result.StartsWith("\r\n"))
                {
                    result = result.Substring(4);
                }
                else
                {
                    result = result.Substring(2);
                }
            }

            return result;
        }

        /// <summary>
        /// Formats the document.
        /// </summary>
        protected async System.Threading.Tasks.Task FormatDocumentAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                (await GetDTEAsync()).ExecuteCommand(Constants.EDIT_DOCUMENT_COMMAND);
            }
            catch (Exception) //Some documents do not support formatting
            {

            }
        }
    }
}
