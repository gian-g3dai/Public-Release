using System;
using System.Collections.Generic;
using System.Text;
using Unakin.Options;
using Unakin.Utils;

namespace UnakinShared.Utils
{
    internal static class UnakinLogger
    {
        internal static OutputVerbosityOptions Options;
        internal static void LogInfo(string message)
        {
            if (Options == OutputVerbosityOptions.Extensive)
                Logger.Log(message);
        }
        internal static void LogWarning(string message)
        {
            if (Options == OutputVerbosityOptions.Detail || Options == OutputVerbosityOptions.Extensive)
                Logger.Log(String.Concat("Warning :", message));
            
        }
        internal static void LogError(string message)
        {
            Logger.Log(String.Concat("Error :", message));
        }
        internal static void HandleException(Exception exception)
        {
            Logger.Log(String.Concat("Exception :", exception.Message));
            Logger.Log("Getting exception stack for previous exception");
            CommonUtils.Token = null;
        }

        public static string GetaAllMessages(this Exception exp)
        {
            string message = string.Empty;
            Exception innerException = exp;

            do
            {
                message = message + (string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                Logger.Log(String.Concat("Inner Exception :", message));
                innerException = innerException.InnerException;
            }
            while (innerException != null);

            return message;
        }
    }
}
