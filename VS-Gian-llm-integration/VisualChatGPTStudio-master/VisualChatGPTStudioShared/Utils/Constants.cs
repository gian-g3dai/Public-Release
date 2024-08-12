using System.Windows.Media;

namespace Unakin.Utils
{
    /// <summary>
    /// Contains constants used throughout the application.
    /// </summary>
    public static class Constants
    {
        //Package Constants
        public const bool USE_CHATGPT = true;
        public const string EXTENSION_NAME = "Visual Unakin Studio";

        public const string EDIT_DOCUMENT_COMMAND = "Edit.FormatDocument";
        public const string MESSAGE_SET_USER_NAME = "Please, set the User Name.";
        public const string MESSAGE_SET_PASSWORD = "Please, set the Password.";
        public const string MESSAGE_INVALID_CREDENTIALS = "Incorrect user name or password!.";
        public const string MESSAGE_SET_PROJECT_NAME = "Please, set the Project Name.";
        public const string MESSAGE_SET_SEARCH_ENGINE_ID = "Please, set the search engine Id.";
        public const string MESSAGE_SET_API_KEY = "Please, set the search engine API key.";
        public const string MESSAGE_SET_OPENAIAPI_KEY = "Please, set the OpenAI API Key.";

        public const string MESSAGE_WAITING_UNAKIN = "Response is loading...";
        public const string MESSAGE_RECEIVING_UNAKIN = "Response is loading...";
        public const string MESSAGE_WRITE_REQUEST = "Please write a request.";
        public const string MESSAGE_SET_COMMAND = "Please, set the command for \"{0}\" through the Options.";
        public const string MESSAGE_SELECTDIR = "Pleae select working directory!";
        public const string MESSAGE_NORESPONSE = "No response from server! Please rephrase and try again!";

        internal const string WEBSOCKET_URL = "";
        internal const string CHAT_URL = "";
        internal const string LOGIN_URL = "";

        public const string INCONTEXT_REPLACE = "enclosed between the /*|Start|*/ and /*|End|*/ comments";
        public const string startComment = "/*|Start|*/";
        public const string endComment = "/*|End|*/";
        public const string GENERIC_DELIMETER = "|#|";

        public static Color CHAT_COLOR_B = Color.FromRgb(69, 69, 69);
        public static Color CODE_COLOR_B = Color.FromRgb(40, 40, 40);
        public static Color TAG_COLOR_ME_B = Color.FromRgb(196, 255, 8);
        public static Color TAG_COLOR_UNAKIN_B = Color.FromRgb(255, 166, 255);
        public const string SELF_COMMENT = "You";
        public const string UNAKIN_COMMENT_FIRST = "Unakin";

        /* // Commands veriable*/
        public const string SEMANTICSEARCH_NAME = "Search in Code";
        public const string SEMANTICSEARCH_DESC = "Search in Code";
        public const int SEMANTICSEARCH_ID = 3;
        public const string LOCALWORKFLOW_NAME = "Local Workflow";
        public const string LOCALWORKFLOW_DESC = "Run agents workflow for local files";
        public const int LOCALWORKFLOW_ID = 13;
        public const string PROJECTSUMMARY_NAME = "Project Summary";
        public const string PROJECTSUMMARY_DESC = "Project Summary";
        public const int PROJECTSUMMARY_ID = 14;
        public const string AUTOMATEDTESTING_NAME = "Automated Testing";
        public const string AUTOMATEDTESTING_DESC = "Automated Testing";
        public const int AUTOMATEDTESTING_ID = 15;
        public const int AUTONOMOUSAGENT_ID = 16;
        public const string AUTONOMOUSAGENT_NAME = "Autonomous Agents";
        public const string AUTONOMOUSAGENT_DESC = "Autonomous Agents";
        public const int DATAGEN_ID = 17;
        public const string DATAGEN_NAME = "Data Generation";
        public const string DATAGEN_DESC = "Data Generation";


    }
}
