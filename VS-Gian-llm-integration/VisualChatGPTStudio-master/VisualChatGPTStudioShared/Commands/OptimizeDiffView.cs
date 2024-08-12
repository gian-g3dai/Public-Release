using Community.VisualStudio.Toolkit;
using Unakin.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using System.Threading.Tasks;
using System.Text;
using Unakin.Options.Commands;
using Unakin;
using UnakinShared.Utils;
using System.Text.RegularExpressions;

namespace Unakin.Commands
{
    /// <summary>
    /// Command to add summary for the entire class.
    /// </summary>
    [Command(PackageIds.OptimizeDiffView)]
    internal sealed class OptimizeDiffView : BaseCommand<OptimizeDiffView>
    {
        /// <summary>
        /// Executes the UNAKIN optimization process for the selected code and shows on a diff view.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (!await AuthHelper.ValidateAPIAsync())
                {
                    return;
                }

                DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
                string selectedText = docView.TextView.Selection.StreamSelectionSpan.GetText();

                if (!await ValidateCodeSelectedAsync(selectedText))
                {
                    await VS.MessageBox.ShowAsync("No Code Selected", "Please select some code to optimize.", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                await VS.StatusBar.ShowProgressAsync(Unakin.Utils.Constants.MESSAGE_WAITING_UNAKIN, 1, 2);

                CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

                string prompt = OptionsCommands.Optimize + selectedText;

                string result = await GetResponseFromWebSocket(prompt);

                result = RemoveBlankLinesFromResult(result.ToString());

                result = result.Replace("```", "");

                // result = Regex.Replace(result, @"\r(?!\n)", "\r\n");
                // Then, replace any LF with CRLF (handles the case where LF is used without preceding CR)
                // result = result.Replace("\n", "\r\n");

                await ShowDiffViewAsync(docView.FilePath, selectedText, result);

                await VS.StatusBar.ShowProgressAsync(Unakin.Utils.Constants.MESSAGE_RECEIVING_UNAKIN, 2, 2);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                if (ex is not OperationCanceledException)
                {
                    await VS.MessageBox.ShowAsync("Error", ex.Message, OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK);
                }
            }
        }
        /// <summary>
        /// Shows a diff view of two strings of code.
        /// </summary>
        private async Task ShowDiffViewAsync(string filePath, string originalCode, string optimizedCode)
        {
            string extension = Path.GetExtension(filePath).TrimStart('.');

            string tempFolder = Path.GetTempPath();
            string tempFilePath1 = Path.Combine(tempFolder, $"Original.{extension}");
            string tempFilePath2 = Path.Combine(tempFolder, $"Optimized.{extension}");

            // Use StreamWriter with explicitly set NewLine property to ensure Windows line endings
            using (var writer = new StreamWriter(tempFilePath1, false, Encoding.UTF8))
            {
                writer.NewLine = "\r\n"; // Set line endings to CRLF
                await writer.WriteAsync(originalCode);
            }

            using (var writer = new StreamWriter(tempFilePath2, false, Encoding.UTF8))
            {
                writer.NewLine = "\r\n"; // Set line endings to CRLF
                await writer.WriteAsync(optimizedCode);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsCommandWindow commandWindow = await VS.Services.GetCommandWindowAsync();

            string command = $"Tools.DiffFiles \"{tempFilePath1}\" \"{tempFilePath2}\"";
            commandWindow.ExecuteCommand(command);
        }


        /// <summary>
        /// Removes blank lines from the result string.
        /// </summary>
        private string RemoveBlankLinesFromResult(string result)
        {
            return string.Join("\n", result.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        /// <summary>
        /// Validates that code is selected. (This is a placeholder, implement your own logic)
        /// </summary>
        private async Task<bool> ValidateCodeSelectedAsync(string code)
        {
            return !string.IsNullOrWhiteSpace(code);
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

            System.Threading.Tasks.Task responseTask = Unakin.Utils.Unakin.CallWebSocketSingleAnswer(UnakinPackage.Instance.OptionsGeneral, prompt, HandleContent, CancellationToken.None, "GPT4-LongContext");

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
    }
}
