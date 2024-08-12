using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Unakin.Utils;
using UnakinShared.Utils.Http;
using System.Net;
using UnakinShared.DTO;

namespace UnakinShared.Utils
{
    internal class SemanticSearchServerHelper:APIClientBase
    {
      

        #region "Properties"
        internal string ProjectName { get; set; }
        string UserToken { get; set; }

        #endregion

        #region Login
        public async Task<Boolean> ConnectAsync()
        {
            
            var tokenResponse= await AuthHelper.GetAccessTokenAsync();
            if (tokenResponse.status == HttpStatusCode.OK)
            {
                this.UserToken = tokenResponse.token;
                return true;
            }
            else
            {
                return false;
            }   
        }
        #endregion // Login

        #region Actions
        public async Task<bool> SendInitialFilesMessageAsync(string workingDir, List<string> filenames, CancellationToken cancellationToken)
        {
            const int CHUNK_SIZE = (1024 * 1024);
            string zipDirectory = string.Empty;
            string zipFilePath = string.Empty;

            if (!CommonUtils.CreateZipFile(workingDir, filenames, out zipDirectory, out zipFilePath)){
                return false;
            }
            var zipFileMeta = CreateMetadataInitialFiles(zipFilePath, CHUNK_SIZE);

            ClientWebSocket socket = null;
            FileStream fileStream = null;

            try
            {
                if (!await this.ConnectAsync())
                {
                    UnakinLogger.LogError("Unable to connect to server!");
                }
                var url = GetWebsocketUrl();
                var userToken = this.UserToken;
                socket = await ConnectWebSocketAsync(url, userToken, cancellationToken);

                await SendWebSocketMessageAsync(socket, zipFileMeta, cancellationToken);
                var protocolDescription = await ReceiveWebSocketMessageAsync(socket, cancellationToken);

                fileStream = File.OpenRead(zipFilePath);
                var buffer = new byte[CHUNK_SIZE];

                for (var i = 0; i < zipFileMeta.n_message_chunks; i++)
                {
                    var bytesRead = fileStream.Read(buffer, 0, CHUNK_SIZE);
                    var base64Data = Convert.ToBase64String(buffer, 0, bytesRead);
                    var byteData = Convert.FromBase64String(base64Data);
                    var hexData = BitConverter.ToString(byteData).Replace("-", "");

                    var messageData = new
                    {
                        user = zipFileMeta.user,
                        project_name = zipFileMeta.project_name,
                        message_bytes = hexData
                    };
                    await SendWebSocketMessageAsync(socket, messageData, cancellationToken);
                }

                // @TODO(mateusdigital): Add error checking...
                var uploadConfirmation = await ReceiveWebSocketMessageAsync(socket, cancellationToken);
                var treeStatusMessage = await ReceiveWebSocketMessageAsync(socket, cancellationToken);

            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while sending file");
                UnakinLogger.HandleException(ex);
                return false;
            }
            finally
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                socket.Dispose();
                fileStream?.Close();
            }


            File.Delete(zipFilePath); // Remove the temporary zip file after use
            return true;
        }

        public async Task<List<string>> SendFilesCreatedUpdateAsync(string workingDir, List<string> createdFiles, CancellationToken cancellationToken)
        {
            const int CHUNK_SIZE = (1024 * 1024);
            string zipDirectory = string.Empty;
            string zipFilePath = string.Empty;

            if (!CommonUtils.CreateZipFile(workingDir, createdFiles, out zipDirectory, out zipFilePath))
            {
                return null;
            }
            var zipFileMeta = CreateMetadataCreatedFiles(zipFilePath, CHUNK_SIZE);

            ClientWebSocket socket = null;
            FileStream fileStream = null;

            try
            {
                if (!await this.ConnectAsync())
                {
                    UnakinLogger.LogError("Unable to connect to server!");
                }
                var url = GetWebsocketUrl();
                var userToken = this.UserToken;
                socket = await ConnectWebSocketAsync(url, userToken, cancellationToken);

                await SendWebSocketMessageAsync(socket, zipFileMeta, cancellationToken);
                var protocolDescription = await ReceiveWebSocketMessageAsync(socket, cancellationToken);

                fileStream = File.OpenRead(zipFilePath);
                var buffer = new byte[CHUNK_SIZE];

                for (var i = 0; i < zipFileMeta.n_message_chunks; i++)
                {
                    var bytesRead = fileStream.Read(buffer, 0, CHUNK_SIZE);
                    var base64Data = Convert.ToBase64String(buffer, 0, bytesRead);
                    var byteData = Convert.FromBase64String(base64Data);
                    var hexData = BitConverter.ToString(byteData).Replace("-", "");

                    var messageData = new
                    {
                        user = zipFileMeta.user,
                        project_name = zipFileMeta.project_name,
                        message_bytes = hexData
                    };
                    await SendWebSocketMessageAsync(socket, messageData, cancellationToken);
                }

                // @TODO(mateusdigital): Add error checking...
                var uploadConfirmation = await ReceiveWebSocketMessageAsync(socket, cancellationToken);
                var treeStatusMessage = await ReceiveWebSocketMessageAsync(socket, cancellationToken);
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while sending file");
                UnakinLogger.HandleException(ex);
                return createdFiles;
            }
            finally
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                socket.Dispose();
                fileStream?.Close();
            }


            File.Delete(zipFilePath); // Remove the temporary zip file after use
            return new List<string>();
        }

        public async Task<List<string>> SendFilesChangedUpdateAsync(string workingDir, List<string> changedFiles, CancellationToken cancellationToken)
        {
            const int CHUNK_SIZE = (1024 * 1024);
            string zipDirectory = string.Empty;
            string zipFilePath = string.Empty;

            if (!CommonUtils.CreateZipFile(workingDir, changedFiles, out zipDirectory, out zipFilePath))
            {
                return null;
            }
            var zipFileMeta = CreateMetadataCreatedFiles(zipFilePath, CHUNK_SIZE);

            ClientWebSocket socket = null;
            FileStream fileStream = null;

            try
            {
                if (!await this.ConnectAsync())
                {
                    UnakinLogger.LogError("Unable to connect to server!");
                }
                var url = GetWebsocketUrl();                   
                var userToken = this.UserToken;
                socket = await ConnectWebSocketAsync(url, userToken, cancellationToken);

                await SendWebSocketMessageAsync(socket, zipFileMeta, cancellationToken);
                var protocolDescription = await ReceiveWebSocketMessageAsync(socket, cancellationToken);

                fileStream = File.OpenRead(zipFilePath);
                var buffer = new byte[CHUNK_SIZE];

                for (var i = 0; i < zipFileMeta.n_message_chunks; i++)
                {
                    var bytesRead = fileStream.Read(buffer, 0, CHUNK_SIZE);
                    var base64Data = Convert.ToBase64String(buffer, 0, bytesRead);
                    var byteData = Convert.FromBase64String(base64Data);
                    var hexData = BitConverter.ToString(byteData).Replace("-", "");

                    var messageData = new
                    {
                        user = zipFileMeta.user,
                        project_name = zipFileMeta.project_name,
                        message_bytes = hexData
                    };
                    await SendWebSocketMessageAsync(socket, messageData, cancellationToken);
                }

                // @TODO(mateusdigital): Add error checking...
                var uploadConfirmation = await ReceiveWebSocketMessageAsync(socket,cancellationToken);
                var treeStatusMessage = await ReceiveWebSocketMessageAsync(socket, cancellationToken);
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while sending file");
                UnakinLogger.HandleException(ex);
                return changedFiles;
            }
            finally
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                socket.Dispose();
                fileStream?.Close();
            }


            File.Delete(zipFilePath); // Remove the temporary zip file after use
            return new List<string>();
        }

        public async Task<List<string>> SendFilesDeletedUpdateAsync(string workingDir, List<string> deletedFilesParam, CancellationToken cancellationToken)
        {
            ClientWebSocket socket = null;
            List<string> deletedFiles = new List<string>(deletedFilesParam);
            try
            {
                if (!await this.ConnectAsync())
                {
                    UnakinLogger.LogError("Unable to connect to server!");
                }
                
                var url = GetWebsocketUrl();
                var userToken = this.UserToken;
                socket = await ConnectWebSocketAsync(url, userToken,cancellationToken);

                for (var i = deletedFiles.Count - 1; i >= 0; --i)
                {
                    var filename = deletedFiles[i];
                    var relativeFilename = CommonUtils.MakeRelativePth(workingDir, filename);
                    var meta = CreateMetadataDelete(relativeFilename);

                    await SendWebSocketMessageAsync(socket, meta, cancellationToken);

                    var deleteConfirmation = await ReceiveWebSocketMessageAsync(socket, cancellationToken);
                    UnakinLogger.LogInfo("deleted file " + deleteConfirmation);

                    var treeStatusMessage = await ReceiveWebSocketMessageAsync(socket, cancellationToken);

                    deletedFiles.RemoveLast();
                }
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while deleting file");
                UnakinLogger.HandleException(ex);
                return deletedFilesParam;
            }
            finally
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                socket.Dispose();
            }

            return deletedFiles;
        }

        public async Task<ResponseDTO> SendSearchCodeBlocksMessageAsync(string promptContents, CancellationToken cancellationToken)
        {
            string substringToRemove = "//Search in Code:";

            if (promptContents.StartsWith(substringToRemove))
            {
                promptContents = promptContents.Substring(substringToRemove.Length).TrimStart();
            }

            ClientWebSocket socket = null;
            try
            {
                var url = GetWebsocketUrl();
                var userToken = this.UserToken;
                socket = await ConnectWebSocketAsync(url, userToken,cancellationToken);

                var searchMessage = new
                {
                    project_name = CommonUtils.ProjectName,
                    query = promptContents,
                    top_k = 10,
                    search_key = "encoding",
                    method = "linear"
                };
                await SendWebSocketMessageAsync(socket, searchMessage, cancellationToken);


                var searchResponse = await ReceiveWebSocketMessageAsync<ResponseDTO>(socket, cancellationToken);
                return searchResponse;
            }
            catch (Exception ex)
            {
                UnakinLogger.LogError("Error while processing request");
                UnakinLogger.HandleException(ex);
            }
            finally
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                socket.Dispose();
            }

            return new ResponseDTO();
        }
        #endregion // Actions

        #region Metadata Helpers
        private MetaDataInitialFiles CreateMetadataInitialFiles(string filePath, int chunkSize)
        {
            // Compute MD5
            using var fileStream = File.OpenRead(filePath);
            using var md5 = MD5.Create();

            var fileSize = (int)(new FileInfo(filePath)).Length;
            var fileChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

            var buffer = new byte[chunkSize];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, chunkSize)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            md5.TransformFinalBlock(buffer, 0, 0);
            var md5_string = BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();

            var meta = new MetaDataInitialFiles
            {
                archive_type = "zip",
                project_name = CommonUtils.ProjectName,
                user = CommonUtils.UserName,
                project_prefix = "",
                write_mode = "init",
                md5 = md5_string,
                message_size = fileSize,
                n_message_chunks = fileChunks
            };
            return meta;
        }

        private MetaDataInitialFiles CreateMetadataCreatedFiles(string filePath, int chunkSize)
        {
            // Compute MD5
            using var fileStream = File.OpenRead(filePath);
            using var md5 = MD5.Create();

            var fileSize = (int)(new FileInfo(filePath)).Length;
            var fileChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

            var buffer = new byte[chunkSize];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, chunkSize)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            md5.TransformFinalBlock(buffer, 0, 0);
            var md5_string = BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();

            var meta = new MetaDataInitialFiles
            {
                archive_type = "zip",
                project_name = this.ProjectName,
                user = CommonUtils.UserName,
                project_prefix = "",
                write_mode = "append",
                md5 = md5_string,
                message_size = fileSize,
                n_message_chunks = fileChunks
            };
            return meta;
        }

        private MetaDataDelete CreateMetadataDelete(string filePath)
        {
            var meta = new MetaDataDelete
            {
                project_name = this.ProjectName,
                user = CommonUtils.UserName,
                delete_path = filePath,
                is_prefix = false
            };
            return meta;
        }
        #endregion // Helpers

        #region URL Helpers
        private string GetWebsocketUrl()
        {
            var final = String.Format(Constants.WEBSOCKET_URL, CommonUtils.UserName, CommonUtils.Password);
            return final;
        }
        #endregion

    }

    internal class MetaDataInitialFiles
    {
        public string archive_type;
        public string project_name;
        public string user;
        public string project_prefix;
        public string write_mode;
        public int n_message_chunks;
        public string md5;
        public int message_size;
    }

    internal class MetaDataDelete
    {
        public string project_name;
        public string user;
        public string delete_path;
        public bool is_prefix;
    }
}
