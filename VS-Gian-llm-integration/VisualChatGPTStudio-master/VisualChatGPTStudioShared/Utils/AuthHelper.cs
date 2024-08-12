using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unakin.Utils;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Unakin;
using System.Threading;

namespace UnakinShared.Utils
{
    internal class AuthHelper
    { 
        //TODO - Only request token if previous token is expired.
        public static async Task<GetAccessTokenResponse> GetAccessTokenAsync()
        {
            if (! await ValidateAPIAsync())
            {
                UnakinLogger.LogError("Invalid username or password key in config!");
                return new GetAccessTokenResponse { status = HttpStatusCode.Forbidden,errorMessage="Invalid Credentials!", token = null};
            }

            if (CommonUtils.Token != null)
                return CommonUtils.Token;

            // Setup client
            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", CommonUtils.UserName),
                new KeyValuePair<string, string>("password", CommonUtils.Password),
            });

            // Make Request
            var httpResponse = await client.PostAsync(Constants.LOGIN_URL, content);

            // Check if is valid response
            var accessTokenResponse = new GetAccessTokenResponse();

            accessTokenResponse.status = httpResponse.StatusCode;
            accessTokenResponse.errorMessage = httpResponse.ReasonPhrase;

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, Constants.MESSAGE_INVALID_CREDENTIALS, Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL, buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    UnakinLogger.LogError(Constants.MESSAGE_INVALID_CREDENTIALS);
                }
                return accessTokenResponse;
            }
            

            // Get the token
            var responseBody = await httpResponse.Content.ReadAsStringAsync();
            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);
            var userToken = jsonResponse.access_token;

            accessTokenResponse.token = userToken;
            CommonUtils.Token = accessTokenResponse;
            return accessTokenResponse;
        }

        public static async Task<bool> ValidateAPIAsync(bool showError = true)
        {
            var optionGeneral = UnakinPackage.Instance.OptionsGeneral;
            
            if (optionGeneral.Service == UnakinShared.Utils.OpenAIService.UNAKIN)
            {
                if (string.IsNullOrWhiteSpace(optionGeneral.UserName))
                {
                    if (showError)
                        await CommonUtils.ShowErrorAsync(Constants.MESSAGE_SET_USER_NAME);
                    UnakinPackage.Instance.ShowOptionPage(typeof(Unakin.Options.OptionPageGridGeneral));
                    return false;
                }
                else
                {
                    CommonUtils.UserName = optionGeneral.UserName;
                }
                if (string.IsNullOrWhiteSpace(optionGeneral.Password))
                {
                    if (showError)
                        await CommonUtils.ShowErrorAsync(Constants.MESSAGE_SET_PASSWORD);
                    UnakinPackage.Instance.ShowOptionPage(typeof(Unakin.Options.OptionPageGridGeneral));
                    return false;
                }
                else
                {
                    CommonUtils.Password = optionGeneral.Password;
                }
            }
            else if (optionGeneral.Service == UnakinShared.Utils.OpenAIService.OpenAI)
            {
                if (string.IsNullOrWhiteSpace(optionGeneral.OpenAIApiKey))
                {
                    if (showError)
                        await CommonUtils.ShowErrorAsync(Constants.MESSAGE_SET_OPENAIAPI_KEY);
                    UnakinPackage.Instance.ShowOptionPage(typeof(Unakin.Options.OptionPageGridGeneral));
                    return false;
                }
            }

            return true;

        }
    }
}
