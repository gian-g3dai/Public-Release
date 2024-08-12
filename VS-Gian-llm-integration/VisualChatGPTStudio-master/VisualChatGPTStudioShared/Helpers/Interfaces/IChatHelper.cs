using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnakinShared.DTO;
using UnakinShared.Models;
using UnakinShared.Utils;

namespace UnakinShared.Helpers.Interfaces
{
    internal interface IChatHelper
    {
        ChatMaster ChatMaster { get; set; }
        List<ChatDetail> ChatDetails { get; set; }
        List<ChatItemDTO> ChatItems { get; set; }
        Conversation Chat { get; set; }
        Action<Boolean> OnRequestComplete { get; set; }
        Action RefreshChatItem { get; set; }
        void AppendUserInput(OpenAIService llm, string prompt);
        ChatItemDTO AddChatItem(AuthorEnum author, string message, bool firstSegment, int index, bool isDirty = true, string imagePath = null);
        void RefreshChat(OpenAIService llm);
        Task<string> GetUnakinResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken);
        Task<string> GetChatGPTResponseAsync(string prompt, AgentDTO agent, CancellationToken cancellationToken);
        System.Threading.Tasks.Task SaveChatAsync();
        System.Threading.Tasks.Task DeleteChatAsync(int index);
        System.Threading.Tasks.Task LoadChatAsync(OpenAIService llm, int index);
    }
}
