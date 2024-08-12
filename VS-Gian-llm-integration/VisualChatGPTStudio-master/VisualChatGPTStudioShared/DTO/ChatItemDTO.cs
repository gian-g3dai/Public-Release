using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using System.Windows.Interop;
using System;
using System.Linq;
using System.Windows.Forms;
using Unakin.Utils;

namespace UnakinShared.DTO
{
    /// <summary>
    /// Represents a ChatItemDTO object which contains information about a chat item.
    /// </summary>
    public class ChatItemDTO
    {
        public ChatItemDTO() { }

        public int Index { get; set; }
        public int Id { get; set; }
        public int ChatType { get; set; } //1-Chat, 2-Agents
        public string ImageSource { get; set; }
        public TextDocument Document { get; set; }
        public string Message { get;  set; }
        public bool IsFirstSegment { get; set; }
        public string Syntax { get; set; }
        public Brush BackgroundColor { get; set; }
        public Thickness Margins { get; set; }
        public Visibility ButtonCopyVisibility { get; set; }
        public Visibility ButtonInsertVisibility { get; set; }
        public Visibility ButtonZoomVisibility { get; set; }


        public string FirstTag{ get; set; }
        public Visibility FirstTagVisiblity { get; set; }
        public string SecondTag { get; set; }
        public Visibility SecondTagVisiblity { get; set; }
        public SolidColorBrush TagColor { get; set; } 


        public string ActionImageSource { get;  set; }
        public Visibility ActionButtonVisiblity { get; set; }
        public string ActionButtonTooltip { get;  set; }
        public string ActionButtonTag { get; set; }


        public ScrollBarVisibility ShowHorizontalScrollBar { get; set; }
        public Visibility IsHeaderVisible { get; set; } = Visibility.Collapsed;
        public Visibility IsItemVisible { get; set; } = Visibility.Visible;
        public int AgentSequence { get; set; } = 0;



        public bool IsDirty = true;

        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Constructor for ChatItemDTO class.
        /// </summary>
        /// <param name="author">Author of the message.</param>
        /// <param name="message">Message content.</param>
        /// <param name="firstSegment">Indicates if the message is the first segment of the conversation.</param>
        /// <param name="index">Index of the message.</param>
        public ChatItemDTO(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null)
        {
            if (author == AuthorEnum.ChatGPTCode)
            {
                message = message.TrimStart().TrimEnd();
            }
            Document = new TextDocument(message);
            Message = message;


            if (author == AuthorEnum.Me)
            {
                ImageSource = "pack://application:,,,/Unakin;component/Resources/user.png";
                BackgroundColor = new SolidColorBrush(Color.FromRgb(69, 69, 69));
                Syntax = string.Empty;
                Margins = new Thickness(0, 5, 5, 5);

                ButtonCopyVisibility = Visibility.Collapsed;
                ButtonInsertVisibility = Visibility.Collapsed;
                ButtonZoomVisibility = Visibility.Collapsed;

                ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
                ActionImageSource = "pack://application:,,,/Unakin;component/Resources/edit.png";
                ActionButtonVisiblity = Visibility.Visible;
                ActionButtonTooltip = "Edit Prompt";
                ActionButtonTag = "P|" + index.ToString();
                IsFirstSegment = true;
            }
            else if (author == AuthorEnum.ChatGPT)
            {
                string defaultChatGptImage = string.IsNullOrEmpty(imagePath) ? "pack://application:,,,/Unakin;component/Resources/unakin.png" : imagePath;
                ImageSource = firstSegment ? defaultChatGptImage : string.Empty;
                BackgroundColor = new SolidColorBrush(Color.FromRgb(69, 69, 69));
                Syntax = string.Empty;
                Margins = new Thickness(0, -2.5, 5, -2.5);

                ButtonCopyVisibility = Visibility.Collapsed;
                ButtonInsertVisibility = Visibility.Collapsed;
                ButtonZoomVisibility = Visibility.Collapsed;

                ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
                ActionImageSource = "pack://application:,,,/Unakin;component/Resources/refresh.png";
                ActionButtonVisiblity = Visibility.Collapsed;
                ActionButtonTooltip = "Refresh Response";
                ActionButtonTag = "R|" + index.ToString();
                IsFirstSegment = firstSegment;
            }
            else if (author == AuthorEnum.ChatGPTCode)
            {
                string defaultChatGptImage = string.IsNullOrEmpty(imagePath) ? "pack://application:,,,/Unakin;component/Resources/unakin.png" : imagePath;
                ImageSource = firstSegment ? defaultChatGptImage : string.Empty;
                BackgroundColor = new SolidColorBrush(Color.FromRgb(40, 42, 54));
                Syntax = TextFormat.DetectCodeLanguage(message);
                Margins = new Thickness(0, -2.5, 0, -2.5);

                ButtonCopyVisibility = Visibility.Collapsed;
                ButtonInsertVisibility = Visibility.Collapsed;
                ButtonZoomVisibility = Visibility.Collapsed;

                ShowHorizontalScrollBar = ScrollBarVisibility.Auto;
                ActionImageSource = "pack://application:,,,/Unakin;component/Resources/refresh.png";
                ActionButtonVisiblity = Visibility.Collapsed;
                ActionButtonTag = "C|" + index.ToString();
                IsFirstSegment = firstSegment;
            }
            IsDirty = isDirty;
            Index = index;

            CreationTime = DateTime.Now;
        }

        public void UpdateDoccument()
        {
            Document.Text = Message;
            this.IsDirty= true; 
        }
    }

    public enum AuthorEnum
    {
        Me,
        ChatGPT,
        ChatGPTCode
    }
}
