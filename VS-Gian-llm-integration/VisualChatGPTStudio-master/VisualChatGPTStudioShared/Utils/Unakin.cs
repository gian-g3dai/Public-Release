using Newtonsoft.Json;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unakin.Options;
using UnakinShared.DTO;
using UnakinShared.Utils;

namespace Unakin.Utils
{
    public class Unakin
    {
        /// <summary>
        /// Function to get UNAKIN/ChatGPT response. Used in all the places except Unakin-Chat
        /// </summary>
        /// <param name="OptionsGeneral"></param>
        /// <param name="contentText"></param>
        /// <param name="onContentReceived"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static async Task CallWebSocket(OptionPageGridGeneral OptionsGeneral, string contentText, Action<string> onContentReceived, CancellationToken cancellationToken, string commandType)
        {
            if (OptionsGeneral.Service == UnakinShared.Utils.OpenAIService.UNAKIN)
            {
                string userToken;
                var tokenResponse = await AuthHelper.GetAccessTokenAsync();
                if (tokenResponse.status == HttpStatusCode.OK)
                {
                    userToken = tokenResponse.token;
                }
                else
                {
                    return;
                }

                using (var client = new ClientWebSocket())
                {
                    string url = Constants.CHAT_URL;

                    client.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");

                    await client.ConnectAsync(new Uri(url), cancellationToken);
                    string jsonString = GetUNAKINMessage(OptionsGeneral, commandType, "user", contentText);
                    await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString)), WebSocketMessageType.Text, true, cancellationToken);

                    try
                    {
                        bool endOfStream = false;
                        while (!endOfStream)
                        {
                            byte[] buffer = new byte[4096];
                            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                            string responseFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);

                            // Deserialize the JSON fragment
                            dynamic jsonFragment = JsonConvert.DeserializeObject<dynamic>(responseFragment);
                            if (jsonFragment?.result?.content != null)
                            {
                                onContentReceived(jsonFragment.result.content.ToString());
                            }

                            // Check for 'end_of_stream' in the current fragment
                            if (responseFragment.Contains("'end_of_stream'"))
                            {
                                endOfStream = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnakinLogger.LogError("Error while processing request");
                        UnakinLogger.HandleException(ex);
                    }
                }
            }
            else if (OptionsGeneral.Service == UnakinShared.Utils.OpenAIService.OpenAI)
            {
                try
                {
                    var result = await ChatGPT.GetResponseAsync(OptionsGeneral, "Automated Testing", contentText, new string[] { }, cancellationToken);
                    if (result != null)
                    {
                        onContentReceived(result);
                    }
                }
                catch (Exception ex)
                {
                    UnakinLogger.LogError("Error while processing request");
                    UnakinLogger.HandleException(ex);
                }
            }
        }

        public static async Task CallWebSocketSingleAnswer(OptionPageGridGeneral OptionsGeneral, string contentText, Action<string> onContentReceived, CancellationToken cancellationToken, string commandType)
        {
                string userToken;
                var tokenResponse = await AuthHelper.GetAccessTokenAsync();
                if (tokenResponse.status == HttpStatusCode.OK)
                {
                    userToken = tokenResponse.token;
                }
                else
                {
                    return;
                }

                using (var client = new ClientWebSocket())
                {
                    string url = "";

                    client.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");

                    await client.ConnectAsync(new Uri(url), cancellationToken);
                    string jsonString = GetUNAKINMessageSingleAnswer(OptionsGeneral, commandType, "user", contentText);
                    await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString)), WebSocketMessageType.Text, true, cancellationToken);

                    try
                    {
                        bool endOfStream = false;
                        while (!endOfStream)
                        {
                            byte[] buffer = new byte[4096];
                            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                            string responseFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);

                            // Deserialize the JSON fragment
                            dynamic jsonFragment = JsonConvert.DeserializeObject<dynamic>(responseFragment);
                            if (jsonFragment?.result?.content != null)
                            {
                                onContentReceived(jsonFragment.result.content.ToString());
                            }

                            // Check for 'end_of_stream' in the current fragment
                            if (responseFragment.Contains("'end_of_stream'"))
                            {
                                endOfStream = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnakinLogger.LogError("Error while processing request");
                        UnakinLogger.HandleException(ex);
                    }
                }
        }

        public static async Task CallWebSocketAutonomousAgents(OptionPageGridGeneral OptionsGeneral, string contentText, Action<string> onContentReceived, CancellationToken cancellationToken, string commandType)
        {
            string userToken;
            var tokenResponse = await AuthHelper.GetAccessTokenAsync();
            if (tokenResponse.status == HttpStatusCode.OK)
            {
                userToken = tokenResponse.token;
            }
            else
            {
                return;
            }

            using (var client = new ClientWebSocket())
            {
                string url = "";

                client.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");

                await client.ConnectAsync(new Uri(url), cancellationToken);
                string jsonString = GetUNAKINMessageSingleAnswerAutonomousAgents(OptionsGeneral, commandType, "user", contentText);
                await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString)), WebSocketMessageType.Text, true, cancellationToken);

                try
                {
                    bool endOfStream = false;
                    while (!endOfStream)
                    {
                        byte[] buffer = new byte[4096];
                        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        string responseFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Deserialize the JSON fragment
                        dynamic jsonFragment = JsonConvert.DeserializeObject<dynamic>(responseFragment);
                        if (jsonFragment?.result?.content != null)
                        {
                            onContentReceived(jsonFragment.result.content.ToString());
                        }

                        // Check for 'end_of_stream' in the current fragment
                        if (responseFragment.Contains("'end_of_stream'"))
                        {
                            endOfStream = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnakinLogger.LogError("Error while processing request");
                    UnakinLogger.HandleException(ex);
                }
            }
        }
        /// <summary>
        /// Function to get UNAKIN/ChatGPT response. Used in Unakin-Chat
        /// </summary>
        /// <param name="OptionsGeneral"></param>
        /// <param name="MessagesContext"></param>
        /// <param name="onContentReceived"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static async Task GetUnakinChatResponseAsync(OptionPageGridGeneral OptionsGeneral, List<ChatItemDTO> chatItems, Action<string> onContentReceived, CancellationToken cancellationToken, string commandType)
        {
            string userToken;
            if (!await AuthHelper.ValidateAPIAsync())
            {
                return;
            }
            var tokenResponse = await AuthHelper.GetAccessTokenAsync();
            if (tokenResponse.status == HttpStatusCode.OK)
            {
                userToken = tokenResponse.token;
            }
            else
            {
                return;
            }

            using (var client = new ClientWebSocket())
            {
                var messagesContext = GetContext(chatItems);
                string url = Constants.CHAT_URL;
                client.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");
                await client.ConnectAsync(new Uri(url), cancellationToken);

                var completionRequest = new
                {
                    messages = messagesContext,
                    model = "default",
                    hyperparameters = new
                    {
                        temperature = OptionsGeneral.UnakinTemperature,
                        max_gen_len = OptionsGeneral.UnakinMaxTokens,
                        runtime_top_p = OptionsGeneral.UnakinRunTimeTopP
                    },
                    anti_repeat_policy = new
                    {
                        min_size = OptionsGeneral.UnakinMinSize,
                        max_size = OptionsGeneral.UnakinMaxSize,
                        min_repeat_proportion = OptionsGeneral.UnakinMinRepSize,
                        repeat_retries = OptionsGeneral.UnakinMaxRepSize
                    },
                    meta = new
                    {
                        command_type = commandType // added command_type from method parameter
                    }
                };

                string jsonString = JsonConvert.SerializeObject(completionRequest);
                await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString)), WebSocketMessageType.Text, true, cancellationToken);

                try
                {
                    bool endOfStream = false;
                    while (!endOfStream)
                    {
                        byte[] buffer = new byte[4096];
                        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        string responseFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Deserialize the JSON fragment
                        dynamic jsonFragment = JsonConvert.DeserializeObject<dynamic>(responseFragment);
                        if (jsonFragment?.result?.content != null)
                        {
                            onContentReceived(jsonFragment.result.content.ToString());
                        }

                        // Check for 'end_of_stream' in the current fragment
                        if (responseFragment.Contains("'end_of_stream'"))
                        {
                            endOfStream = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnakinLogger.LogError("Error while processing request");
                    UnakinLogger.HandleException(ex);
                }
            }
        }

        public static async Task GetChatGPTChatResponseAsync(OptionPageGridGeneral OptionsGeneral, List<ChatItemDTO> chatItems, Conversation chat, bool regenerateChat, Action<List<ChatTurboResponseSegment>> onContentReceived, CancellationToken cancellationToken)
        {
            if (!await AuthHelper.ValidateAPIAsync())
            {
                return;
            }

            if (regenerateChat)
                chat = ChatGPT.RegerateChat(OptionsGeneral, chatItems, chat);

            string response = await SendRequestAsync(chat, cancellationToken);
            List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments(response);
            onContentReceived(segments);
        }

        public static async Task GetUnakinSingleChatResponseAsync(OptionPageGridGeneral OptionsGeneral, List<ChatItemDTO> chatItems, Action<string> onContentReceived, CancellationToken cancellationToken, string commandType)
        {
            string userToken;
            if (!await AuthHelper.ValidateAPIAsync())
            {
                return;
            }
            var tokenResponse = await AuthHelper.GetAccessTokenAsync();
            if (tokenResponse.status == HttpStatusCode.OK)
            {
                userToken = tokenResponse.token;
            }
            else
            {
                return;
            }

            using (var client = new ClientWebSocket())
            {
                var messagesContext = GetContext(chatItems);
                string url = "";
                client.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");
                await client.ConnectAsync(new Uri(url), cancellationToken);

                var completionRequest = new
                {
                    messages = messagesContext,
                    model = "big-expensive",
                    hyperparameters = new
                    {
                        temperature = OptionsGeneral.UnakinTemperature,
                        max_gen_len = OptionsGeneral.UnakinMaxTokens,
                        runtime_top_p = OptionsGeneral.UnakinRunTimeTopP
                    },
                    anti_repeat_policy = new
                    {
                        min_size = OptionsGeneral.UnakinMinSize,
                        max_size = OptionsGeneral.UnakinMaxSize,
                        min_repeat_proportion = OptionsGeneral.UnakinMinRepSize,
                        repeat_retries = OptionsGeneral.UnakinMaxRepSize
                    },
                    meta = new
                    {
                        command_type = commandType // added command_type from method parameter
                    }
                };

                string jsonString = JsonConvert.SerializeObject(completionRequest);
                await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString)), WebSocketMessageType.Text, true, cancellationToken);

                try
                {
                    bool endOfStream = false;
                    while (!endOfStream)
                    {
                        byte[] buffer = new byte[4096];
                        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        string responseFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Deserialize the JSON fragment
                        dynamic jsonFragment = JsonConvert.DeserializeObject<dynamic>(responseFragment);
                        if (jsonFragment?.result?.content != null)
                        {
                            onContentReceived(jsonFragment.result.content.ToString());
                        }

                        // Check for 'end_of_stream' in the current fragment
                        if (responseFragment.Contains("'end_of_stream'"))
                        {
                            endOfStream = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnakinLogger.LogError("Error while processing request");
                    UnakinLogger.HandleException(ex);
                }
            }
        }
        public static async Task<bool> IsServerUpAsync()
        {
            if (!await AuthHelper.ValidateAPIAsync(false))
                return false;

            string userToken;
            var optionGenral = UnakinPackage.Instance.OptionsGeneral;
            try
            {
                var task = "Ping";
                if (UnakinPackage.Instance.OptionsGeneral.Service == UnakinShared.Utils.OpenAIService.UNAKIN)
                {
                    var tokenResponse = await AuthHelper.GetAccessTokenAsync();
                    if (tokenResponse.status == HttpStatusCode.OK)
                    {
                        userToken = tokenResponse.token;
                    }
                    else
                    {
                        return false;
                    }
                    using (var client = new ClientWebSocket())
                    {
                        string url = Constants.CHAT_URL;

                        client.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");

                        await client.ConnectAsync(new Uri(url), CancellationToken.None);

                        var jsonString = GetUNAKINMessage(optionGenral, "ping", "user", task);
                        await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonString)), WebSocketMessageType.Text, true, CancellationToken.None);
                        bool endOfStream = false;

                        while (!endOfStream)
                        {
                            byte[] buffer = new byte[4096];
                            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            string responseFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);

                            // Deserialize the JSON fragment
                            dynamic jsonFragment = JsonConvert.DeserializeObject<dynamic>(responseFragment);
                            if (jsonFragment?.result?.content != null)
                            {
                                return true;
                            }

                            // Check for 'end_of_stream' in the current fragment
                            if (responseFragment.Contains("'end_of_stream'"))
                            {
                                endOfStream = true;
                            }
                        }
                    }
                }
                else if (UnakinPackage.Instance.OptionsGeneral.Service == UnakinShared.Utils.OpenAIService.OpenAI)
                {
                    Conversation chat = ChatGPT.CreateConversation(optionGenral, task);
                    var result = await SendRequestAsync(chat, CancellationToken.None);
                    if (result != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while processing request");
                UnakinLogger.HandleException(ex);
                return false;
            }
            return false;
        }

        private static string GetUNAKINMessage(OptionPageGridGeneral optionsGeneral, string commandType,string role, string prompt)
        {
            var completionRequest = new
            {
                messages = new[] { new { role = role, content = prompt } },
                model = "default",
                hyperparameters = new
                {
                    temperature = optionsGeneral.UnakinTemperature,
                    max_gen_len = optionsGeneral.UnakinMaxTokens,
                    runtime_top_p = optionsGeneral.UnakinRunTimeTopP
                },
                anti_repeat_policy = new
                {
                    min_size = optionsGeneral.UnakinMinSize,
                    max_size = optionsGeneral.UnakinMaxSize,
                    min_repeat_proportion = optionsGeneral.UnakinMinRepSize,
                    repeat_retries = optionsGeneral.UnakinMaxRepSize
                },
                meta = new
                {
                    command_type = commandType // added command_type from method parameter
                }
            };

            return JsonConvert.SerializeObject(completionRequest);
        }

        private static string GetUNAKINMessageSingleAnswer(OptionPageGridGeneral optionsGeneral, string commandType, string role, string prompt)
        {
            var completionRequest = new
            {
                messages = new[] { new { role = role, content = prompt } },
                model = "big-very-expensive",
                hyperparameters = new
                {
                    temperature = optionsGeneral.UnakinTemperature,
                    max_gen_len = optionsGeneral.UnakinMaxTokens,
                    runtime_top_p = optionsGeneral.UnakinRunTimeTopP
                },
                anti_repeat_policy = new
                {
                    min_size = optionsGeneral.UnakinMinSize,
                    max_size = optionsGeneral.UnakinMaxSize,
                    min_repeat_proportion = optionsGeneral.UnakinMinRepSize,
                    repeat_retries = optionsGeneral.UnakinMaxRepSize
                },
                meta = new
                {
                    command_type = commandType // added command_type from method parameter
                }
            };

            return JsonConvert.SerializeObject(completionRequest);
        }

        private static string GetUNAKINMessageSingleAnswerAutonomousAgents(OptionPageGridGeneral optionsGeneral, string commandType, string role, string prompt)
        {
            var completionRequest = new
            {
                messages = new[] { new { role = role, content = prompt } },
                model = "big-expensive",
                hyperparameters = new
                {
                    temperature = optionsGeneral.UnakinTemperature,
                    max_gen_len = optionsGeneral.UnakinMaxTokens,
                    runtime_top_p = optionsGeneral.UnakinRunTimeTopP
                },
                anti_repeat_policy = new
                {
                    min_size = optionsGeneral.UnakinMinSize,
                    max_size = optionsGeneral.UnakinMaxSize,
                    min_repeat_proportion = optionsGeneral.UnakinMinRepSize,
                    repeat_retries = optionsGeneral.UnakinMaxRepSize
                },
                meta = new
                {
                    command_type = commandType // added command_type from method parameter
                }
            };

            return JsonConvert.SerializeObject(completionRequest);
        }

        private static List<ChatMessage> GetContext(List<ChatItemDTO> chatItems)
        {
            var chatMessages = new List<ChatMessage>();
            var previousTag = string.Empty;
            foreach (var chatItem in chatItems)
            {
                ChatMessage msg;
                if (chatItem.ActionButtonTag.StartsWith("P"))
                {
                    if (previousTag.StartsWith("P"))
                    {
                        chatMessages[chatMessages.Count - 1].content += Environment.NewLine + chatItem.Document.Text;
                        continue;
                    }
                    else
                    {
                        msg = new ChatMessage { role = "user", content = chatItem.Document.Text };
                    }                  
                }                
                else
                    msg = new ChatMessage { role = "server", content = chatItem.Document.Text };

                chatMessages.Add(msg);
                previousTag = chatItem.ActionButtonTag;
            }
            return chatMessages;
        }

        /// <summary>
        /// Sends a request to the chatbot asynchronously and waits for a response.
        /// </summary>
        /// <returns>The response from the chatbot.</returns>
        private static async Task<string> SendRequestAsync(Conversation chat, CancellationToken cancellationToken)
        {
            Task<string> task = chat.GetResponseFromChatbotAsync();
            await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return await task;
        }

    }




    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
