using EnvDTE80;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EnvDTE;
using EnvDTE80;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Task = System.Threading.Tasks.Task;


namespace Unakin.ToolWindows
{
    /// <summary>
    /// Helper class for the terminal windows
    /// </summary>
    public class TerminalWindowHelper
    {
        /// <summary>
        /// Copies the given text to the clipboard and changes the button image and tooltip.
        /// </summary>
        /// <param name="button">The button to change.</param>
        /// <param name="text">The text to copy.</param>
        public static void Copy(Button button, string text)
        {
            Clipboard.SetText(text);

            //Image img = new() { Source = new BitmapImage(new Uri("pack://application:,,,/Unakin;component/Resources/check.png")) };

            //button.Content = img;
            button.ToolTip = "Copied!";

            System.Timers.Timer timer = new(2000) { Enabled = true };

            timer.Elapsed += (s, args) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    //img = new() { Source = new BitmapImage(new Uri("pack://application:,,,/Unakin;component/Resources/copy.png")) };

                    //button.Content = img;
                    button.ToolTip = "Copy code";
                }));

                timer.Enabled = false;
                timer.Dispose();
            };
        }

        public static async Task ReplaceSelectedTextInActiveDocumentAsync(string newText)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsTextManager textManager = (IVsTextManager)await ServiceProvider.GetGlobalServiceAsync(typeof(SVsTextManager));
            textManager.GetActiveView(1, null, out IVsTextView activeView);

            if (activeView is IVsUserData userData)
            {
                Guid guidIWpfTextViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidIWpfTextViewHost, out object textViewHostObj);

                IWpfTextViewHost textViewHost = textViewHostObj as IWpfTextViewHost;
                if (textViewHost != null)
                {
                    ITextSnapshot snapshot = textViewHost.TextView.TextSnapshot;
                    ITextSelection selection = textViewHost.TextView.Selection;

                    if (!selection.IsEmpty)
                    {
                        SnapshotSpan span = new SnapshotSpan(snapshot, selection.SelectedSpans[0].Start, selection.SelectedSpans[0].Length);
                        ITextEdit edit = span.Snapshot.TextBuffer.CreateEdit();

                        // Determine the base indentation of the line where the replacement starts
                        string currentLineText = span.Start.GetContainingLine().GetText();
                        string leadingWhitespace = currentLineText.Substring(0, currentLineText.Length - currentLineText.TrimStart().Length);

                        // Add an extra tab to the base indentation
                        //string extraTab = "  "; // Assuming a tab is equivalent to 4 spaces; adjust as necessary
                        //string adjustedIndentation = leadingWhitespace + extraTab;
                        string adjustedIndentation = leadingWhitespace;

                        // Apply the adjusted indentation to each line of the newText
                        var newTextLines = newText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        for (int i = 0; i < newTextLines.Length; i++)
                        {
                            if (i == 0)
                            {
                                // Optionally, skip adding extra indentation to the first line if it's already positioned correctly
                                //newTextLines[i] = adjustedIndentation + newTextLines[i].TrimStart();
                                newTextLines[i] = newTextLines[i].TrimStart();
                            }
                            else
                            {
                                newTextLines[i] = adjustedIndentation + newTextLines[i];
                            }
                        }
                        string indentedNewText = string.Join(Environment.NewLine, newTextLines);

                        edit.Replace(span, indentedNewText);
                        edit.Apply();
                    }
                }
            }
        }




    }
}
