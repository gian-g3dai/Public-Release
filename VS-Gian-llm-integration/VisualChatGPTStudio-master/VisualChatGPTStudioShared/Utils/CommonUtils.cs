using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Community.VisualStudio.Toolkit;
using System.Net;
using UnakinShared.Utils;
using EnvDTE;
using System.Threading;

namespace Unakin.Utils
{
    internal static class CommonUtils
    {
        #region "Properties"
        internal static string WorkingDir { get; set; }
        internal static string UserName { get; set; } = string.Empty; 
        internal static string Password { get; set; } = string.Empty; 
        internal static string ProjectName { get; set; } = string.Empty;
        internal static bool IsDirectoryChanged { get; set; } = true;
        internal static GetAccessTokenResponse Token { get; set; } = null;

        #endregion

        /// <summary>
        /// Copies the given text to the clipboard and changes the button image and tooltip.
        /// </summary>
        /// <param name="button">The button to change.</param>
        /// <param name="text">The text to copy.</param>
        public static void Copy(Button button, string text)
        {
            Clipboard.SetText(text);

            Image img = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/Unakin;component/Resources/check.png")) };

            button.Content = img;
            button.ToolTip = "Copied!";

            System.Timers.Timer timer = new System.Timers.Timer(2000) { Enabled = true };

            timer.Elapsed += (s, args) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    img = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/Unakin;component/Resources/copy.png")) };

                    button.Content = img;
                    button.ToolTip = "Copy code";
                }));

                timer.Enabled = false;
                timer.Dispose();
            };
        }

        public static T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent != null && !(parent is T))
                return (T)GetAncestorOfType<T>((FrameworkElement)parent);
            return (T)parent;
        }

        internal static T GetVisualChildInDataTemplate<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChildInDataTemplate<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        internal static bool CreateZipFile(string workingDir, List<string> files, out string zipDirectory,out string zipFilePath)
        {
            zipDirectory = string.Empty;
            zipFilePath = string.Empty;

            try
            {
                var guid = Guid.NewGuid();
                string tmpDir = Path.GetTempPath();
                zipDirectory = tmpDir + guid.ToString();
                zipFilePath = tmpDir + guid.ToString() + ".zip";

                foreach(var file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    CopyFilesRecursively(fileInfo, zipDirectory+ file.Replace(workingDir, string.Empty));
                }

                //CommonUtils.CopyFilesRecursively(WorkingDir, zipDirectory);
                System.IO.Compression.ZipFile.CreateFromDirectory(zipDirectory, zipFilePath);
            }
            catch (Exception ex){
                Logger.Log("Exception: Unable to create zip file");
                Logger.Log("Exception: " + ex.Message);
            }


            return true;
        }

        internal static void DeleteZipFile(string zipDirectory, string zipFilePath)
        {
            try
            {
                if (File.Exists(zipFilePath))
                    File.Delete(zipFilePath);

                if (Directory.Exists(zipDirectory))
                    Directory.Delete(zipDirectory, true);
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: Unable to delete zip file");
                Logger.Log("Exception: " + ex.Message);
            }
        }

        internal static void CopyFilesRecursively(FileInfo sourceFile,string targetFilePath)
        {
            if (!sourceFile.FullName.EndsWith(".cs"))
                return;
            var dir = (new FileInfo(targetFilePath)).Directory;
            if (!Directory.Exists(dir.FullName))
                (new FileInfo(targetFilePath)).Directory.Create();
            File.Copy(sourceFile.FullName, targetFilePath);

        }

        internal static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            string[] systemFolders = new string[] { "bin", "obj" };
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                if (newPath.EndsWith(".cs"))
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        internal static string MakeRelativePth(string workingDirectory, string fileName)
        {
            if (fileName.StartsWith(fileName))
                return fileName.Replace(workingDirectory + "\\", string.Empty);

            return fileName;
        }

        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }

        public static async Task ShowErrorAsync(string message)
        {
            await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, message, buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
        }

        public static async Task HasTimedOutAsync(System.Threading.Tasks.Task task, DateTime lastRecived, int inActivityTimerSec = 2)
        {
            TimeSpan inactivityTimeout = TimeSpan.FromSeconds(inActivityTimerSec);
            while (!task.IsCompleted)
            {
                if (DateTime.Now - lastRecived > inactivityTimeout)
                {
                    break;
                }
                await System.Threading.Tasks.Task.Delay(500);
            }
        }
    }

    public static class ListExtensions
    {
        public static void RemoveLast<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
            }
            else
            {
                throw new InvalidOperationException("List is empty.");
            }
        }
    }

    /// <summary>
    /// A helper class for working with enums in C#.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Gets the string value associated with the specified enum item.
        /// </summary>
        /// <param name="enumItem">The enum item.</param>
        /// <returns>The string value associated with the enum item.</returns>
        public static string GetStringValue(this Enum enumItem)
        {
            string enumStringValue = string.Empty;
            Type type = enumItem.GetType();

            FieldInfo objFieldInfo = type.GetField(enumItem.ToString());

            if (objFieldInfo != null) // Check if objFieldInfo is not null
            {
                EnumStringValue[] enumStringValues = objFieldInfo.GetCustomAttributes(typeof(EnumStringValue), false) as EnumStringValue[];

                if (enumStringValues != null && enumStringValues.Length > 0) // Check if enumStringValues is not null and has elements
                {
                    enumStringValue = enumStringValues[0].Value;
                }
            }

            return enumStringValue;
        }

    }

    /// <summary>
    /// Represents an attribute that can be used to associate a string value with an enum value.
    /// </summary>
    public class EnumStringValue : Attribute
    {
        private readonly string value;

        /// <summary>
        /// Initializes a new instance of the EnumStringValue class with the specified value.
        /// </summary>
        /// <param name="value">The value of the EnumStringValue.</param>
        public EnumStringValue(string value)
        {
            this.value = value;
        }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        /// <returns>The value of the property.</returns>
        public string Value
        {
            get
            {
                return value;
            }
        }
    }

    #region Types
    internal class GetAccessTokenResponse
    {
        public HttpStatusCode status;
        public string token;
        public string errorMessage;
    }
    #endregion
}
