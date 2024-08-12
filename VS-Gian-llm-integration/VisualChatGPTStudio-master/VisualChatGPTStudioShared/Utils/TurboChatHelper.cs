using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Unakin.Options;
using UnakinShared.DTO;
using UnakinShared.Enums;
using UnakinShared.Helpers.Interfaces;

namespace Unakin.Utils
{
    /// <summary>
    /// This static class provides helper methods for the TurboChat.
    /// </summary>
    public static class TurboChatHelper
    {
        public static List<ChatTurboResponseSegment> GetChatTurboResponseSegments(string response)
        {
            const string DIVIDER = "```";

            Regex regex = new($@"({DIVIDER}([\s\S]*?){DIVIDER})");

            MatchCollection matches = regex.Matches(response);

            List<ChatTurboResponseSegment> substrings = new();

            //Get all substrings from the separation with the character ```
            string[] allSubstrings = response.Split(new string[] { DIVIDER }, StringSplitOptions.None);

            int indexFirstLine;

            // Identify the initial and final position of each substring that appears between the characters ```
            foreach (Match match in matches)
            {
                int start = match.Index;
                int end = start + match.Length;

                indexFirstLine = match.Value.IndexOf('\n');

                substrings.Add(new ChatTurboResponseSegment
                {
                    IsCode = true,
                    Content = Environment.NewLine + match.Value.Substring(indexFirstLine + 1).Replace(DIVIDER, string.Empty) + Environment.NewLine,
                    Start = start,
                    End = end
                });
            }

            bool matched;

            // Identify the initial and final position of each substring that does not appear between special characters ``` 
            for (int i = 0; i < allSubstrings.Length; i++)
            {
                matched = false;

                foreach (Match match in matches)
                {
                    if (match.Value.Contains(allSubstrings[i]))
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    continue;
                }

                int start = response.IndexOf(allSubstrings[i]);
                int end = start + allSubstrings[i].Length;

                substrings.Add(new ChatTurboResponseSegment
                {
                    IsCode = false,
                    Content = allSubstrings[i].Trim(),
                    Start = start,
                    End = end
                });
            }

            // Order the list of substrings by their starting position.
            return substrings.OrderBy(s => s.Start).ToList();
        }

        internal static Tuple<ChatItemDTO, AuthorEnum> SaprateCode(List<ChatItemDTO> chatItems, ChatItemDTO chatItem, AuthorEnum aurthor, IChatHelper chatHelper = null)
        {
            

            string[] lines = chatItem.Message.Split(new char[] { '\r', '\n' }, StringSplitOptions.None);
            if (lines.Length >= 2)
            {
                //chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                string line = null;
                bool isChanged = false;
                if (chatItem.Message.Contains("```"))
                {
                    

                    isChanged = true;
                    if (lines[lines.Length - 2].Contains("```"))
                    {
                        line = lines[lines.Length - 2];
                        

                    }

                    else if (lines.Length >= 3 && lines[lines.Length - 3].Contains("```"))
                    {
                        line = lines[lines.Length - 3];
                        

                    }
                    else if (lines.Length >= 3 && lines[lines.Length - 1].Contains("```"))
                    {
                        line = lines[lines.Length - 1];
                       

                    }
                    
                    chatItem.Message = chatItem.Message.Replace("```", string.Empty);
                    

                    if (aurthor == AuthorEnum.ChatGPTCode && String.IsNullOrEmpty(chatItem.Syntax))
                    {
                        chatItem.Syntax = TextFormat.DetectCodeLanguage(chatItem.Message);
                        chatItem.ActionButtonTag.Replace("R|", "C|");
                        
                    }
                    
                }


                if (isChanged && !String.IsNullOrEmpty(line))
                {

                    chatItem.ActionButtonVisiblity = Visibility.Collapsed;

                    string remaining = string.Empty;
                    if (!lines[lines.Length - 1].Contains("```") && !string.IsNullOrEmpty(remaining = lines[lines.Length - 1]))
                    {
                        remaining = lines[lines.Length - 1];
                        chatItem.Message = chatItem.Message.Replace(remaining, string.Empty);
                        


                    }

                    

                    chatItem.Message = chatItem.Message.Replace(line, string.Empty);
                    //chatItem.Message = chatItem.Message.TrimStart().TrimEnd();
                    //chatItem.Message = chatItem.Message.Replace("```", string.Empty);
                    chatItem.Message = chatItem.Message.Replace("```", string.Empty).Trim();
                    chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                    chatItem.UpdateDoccument();

                   


                    aurthor = aurthor == AuthorEnum.ChatGPT ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;
                    if (chatHelper == null)
                    {
                        chatItem = new ChatItemDTO(aurthor, remaining, false, chatItems.Count);
                        chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                    }
                    else
                    {
                        chatItem = chatHelper.AddChatItem(aurthor, remaining, false, chatItems.Count);
                        chatItem.ActionButtonVisiblity = Visibility.Collapsed;
                        
                    }
                    if ((chatHelper == null) || (chatHelper != null && chatHelper.ChatMaster.ChatType == 1))
                    {
                       // chatItem.ActionButtonVisiblity = Visibility.Visible;
                        
                    }

                    //chatItem.ActionButtonVisiblity = Visibility.Visible;
                    chatItem.ActionButtonTooltip = "Refresh Response";
                    if (aurthor == AuthorEnum.ChatGPT)
                        chatItem.ActionButtonTag = "R|" + chatItems.Count;
                    else if(aurthor == AuthorEnum.ChatGPTCode)
                        chatItem.ActionButtonTag = "C|" + chatItems.Count;
                    chatItem.Message = chatItem.Message.Replace("```", string.Empty);

                    chatItems.Add(chatItem);
                }
                chatItem.ActionButtonVisiblity = Visibility.Visible;
            }
            return new Tuple<ChatItemDTO, AuthorEnum>(chatItem, aurthor);
        }

        internal static ChatType GetChatType(string command)
        {
            var Commands = OptionPageGridCommands.Commands.Where(x=> command.Trim().StartsWith(x.Value.Item2));
            if (Commands == null || Commands.Count() == 0)
                return ChatType.Chat;

            var commandTypeInt =  Commands.First().Key;

            if (commandTypeInt>=4 && commandTypeInt<=12)
                return ChatType.IDE;
            else if (commandTypeInt == 3)
                return ChatType.SemanticSearch;
            else if (commandTypeInt == Constants.PROJECTSUMMARY_ID)
                return ChatType.ProjectSummary;
            else if (commandTypeInt == Constants.AUTOMATEDTESTING_ID)
                return ChatType.AutomatedTesting;
            else if (commandTypeInt == Constants.AUTONOMOUSAGENT_ID)
                return ChatType.AutonomousAgent;
            else if (commandTypeInt == Constants.DATAGEN_ID)
                return ChatType.DataGeneration;
            else
                return ChatType.Chat;
        }

        internal static int GetChatNum(string command)
        {

            var Commands = OptionPageGridCommands.Commands.Where(x => command.Trim().StartsWith(x.Value.Item2));
            if (Commands == null || Commands.Count() == 0)
                return 1;

            return Commands.First().Key;

        }




        internal static ChatType GetChatType(int commandTypeInt)
        {
            if (commandTypeInt == 1 )
                return ChatType.Chat;
            else if (commandTypeInt == 2)
                return ChatType.Agents;
            else if (commandTypeInt == 3)
                return ChatType.SemanticSearch;
            else if (commandTypeInt >= 4 && commandTypeInt <= 12)
                return ChatType.IDE;
            else if (commandTypeInt == Constants.PROJECTSUMMARY_ID)
                return ChatType.ProjectSummary;
            else if (commandTypeInt == Constants.AUTOMATEDTESTING_ID)
                return ChatType.AutomatedTesting;
            else if (commandTypeInt == Constants.DATAGEN_ID)
                return ChatType.DataGeneration;
            else
                return ChatType.Chat;
        }
        internal static string GetChatName(string command)
        {
            var Commands = OptionPageGridCommands.Commands.Where(x => command.Trim().StartsWith(new string(x.Value.Item2.Take(30).ToArray())) );
            if (Commands == null || Commands.Count() == 0)
                return "Chat";

            var commandType = Commands.First().Value;
            return commandType.Item1;
        }
        internal static string GetCommandName(int ChatType)
        {
            Tuple<string, string,bool> command= new Tuple<string,string, bool> ("Chat","Chat", false);
            OptionPageGridCommands.Commands.TryGetValue(ChatType, out command);
            return command.Item2;

        }
    }

    /// <summary>
    /// This class provides methods for segmenting a ChatTurbo response into its component parts.
    /// </summary>
    public class ChatTurboResponseSegment
    {
        public bool IsCode { get; set; }

        public string Content { get; set; }

        public int Start { get; set; }

        public int End { get; set; }
    }
}
