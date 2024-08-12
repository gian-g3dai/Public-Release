using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace UnakinShared.Utils.Http
{
    abstract class APIClientBase
    {

        #region CTOR
        protected APIClientBase()
        {
        }
        #endregion


        #region Web Socket API
        protected static async Task<ClientWebSocket> ConnectWebSocketAsync(string value, string userToken, CancellationToken cancellationToken)
        {
            var webSocket = new ClientWebSocket();
            webSocket.Options.SetRequestHeader("Authorization", $"Bearer {userToken}");

            var uri = new Uri(value);

            Logger.Log("Trying to connect with server.");
            await webSocket.ConnectAsync(uri, cancellationToken);

            return webSocket;
        }

        protected static async Task SendWebSocketMessageAsync<TMessage>(ClientWebSocket webSocket, TMessage message, CancellationToken cancellationToken)
        {
            // Serialize
            var messageString = JsonConvert.SerializeObject(message);

            // Send
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(messageString));
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
        }

        protected static async Task<object> ReceiveWebSocketMessageAsync(ClientWebSocket webSocket, CancellationToken cancellationToken )
        {
            return await ReceiveWebSocketMessageAsync<object>(webSocket, cancellationToken);
        }

        protected static async Task<TResult> ReceiveWebSocketMessageAsync<TResult>(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            // Receive
            var buffer = new byte[1024];
            var result = new StringBuilder();

            while (true)
            {
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                result.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));

                if (receiveResult.EndOfMessage)
                    break;
            }

            // Deserialize
            var resultString = result.ToString();

            var resultObject = JsonConvert.DeserializeObject<TResult>(resultString);

            return resultObject;
        }
        #endregion // Web Socket
    }
}
