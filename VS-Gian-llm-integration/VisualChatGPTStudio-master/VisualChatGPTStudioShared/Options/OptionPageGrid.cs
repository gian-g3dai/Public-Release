using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unakin.Utils;
using UnakinShared.Utils;

namespace Unakin.Options
{
    /// <summary>
    /// Represents a class that provides a dialog page for displaying general options.
    /// </summary>
    [ComVisible(true)]
    public class OptionPageGridGeneral : DialogPage
    {
        public OptionPageGridGeneral()
        {
            PropertyDescriptorCollection propCol = TypeDescriptor.GetProperties(this.GetType());
            List<string> chatGPTProps = new List<string>() { "Service" , "OpenAIApiKey", "Model", "MaxTokens", "Temperature", "PresencePenalty", "FrequencyPenalty", "TopP", "StopSequences" };
            foreach(PropertyDescriptor prop in propCol)
            {
                if (!chatGPTProps.Contains(prop.Name))
                    continue;

                BrowsableAttribute att = (BrowsableAttribute)prop.Attributes[typeof(BrowsableAttribute)];
                FieldInfo cat = att.GetType().GetField("browsable", BindingFlags.NonPublic | BindingFlags.Instance);
                cat.SetValue(att, Constants.USE_CHATGPT);
            }

        }

        bool useChatGPT = false;

        #region General

        [Category("General")]
        [DisplayName("LLM Provider")]
        [Description("Select how to connect: Unakin or OpenAI API.")]
        [DefaultValue(OpenAIService.UNAKIN)]
        [TypeConverter(typeof(EnumConverter))]
        public OpenAIService Service { get; set; }

        [Category("General")]
        [DisplayName("OpenAI API Key")]
        [Description("Set API Key. For OpenAI API, see \"https://beta.openai.com/account/api-keys\" for more details.")]
        public string OpenAIApiKey { get; set; }

        [Category("General")]
        [DisplayName("User Name")]
        [Description("Set User Name for Server Calls.")]
        [Browsable(true)]
        public string UserName { get; set; }

        private string password;

        [Category("General")]
        [DisplayName("Password")]
        [Description("Set Password for Server Calls.")]
        [PasswordPropertyText(true)]  // This attribute indicates the property is for a password
        [Browsable(true)]
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        [Category("General")]
        [DisplayName("Project Name")]
        [Description("Specify Project Name for semantic search.")]
        [Browsable(true)]
        public string ProjectName { get; set; }

        [Category("General")]
        [DisplayName("Single Response")]
        [Description("If true, the entire response will be displayed at once (less undo history but longer waiting time).")]
        [DefaultValue(false)]
        [Browsable(true)]
        public bool SingleResponse { get; set; } = false;

        private OutputVerbosityOptions outputVerbosity;
        [Category("General")]
        [DisplayName("Output Verbosity")]
        [Description("Output verbosity for logger : Use Minimal for genral use, use Detail and Extensive for tracing.")]
        [DefaultValue(OutputVerbosityOptions.Minimal)]
        [TypeConverter(typeof(EnumConverter))]
        [Browsable(true)]
        public OutputVerbosityOptions OutputVerbosity
        {
            get { return outputVerbosity; }
            set
            {
                outputVerbosity = value;
                UnakinLogger.Options = value;
            }
        }

        [Category("General")]
        [DisplayName("Minify Requests")]
        [Description("If true, all requests to OpenAI will be minified. Ideal to save Tokens.")]
        [DefaultValue(false)]
        [Browsable(true)]
        public bool MinifyRequests { get; set; } = false;

        [Category("General")]
        [DisplayName("Characters To Remove From Requests")]
        [Description("Add characters or words to be removed from all requests made to OpenAI. They must be separated by commas, e.g. a,1,TODO:,{")]
        [DefaultValue("")]
        [Browsable(true)]
        public string CharactersToRemoveFromRequests { get; set; } = string.Empty;

        #endregion General

        #region ChatGPT Model Parameters

        [Category("OpenAI Model Parameters")]
        [DisplayName("Model Language")]
        [Description("See \"https://platform.openai.com/docs/models/overview\" for more details.")]
        [DefaultValue(ModelLanguageEnum.GPT_3_5_Turbo_1106)]
        [TypeConverter(typeof(EnumConverter))]
        public ModelLanguageEnum Model { get; set; } = ModelLanguageEnum.GPT_3_5_Turbo_1106;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Max Tokens")]
        [Description("See \"https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them\" for more details.")]
        [DefaultValue(2048)]
        public int MaxTokens { get; set; } = 4096;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Temperature")]
        [Description("What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 for ones with a well-defined answer.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double Temperature { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Presence Penalty")]
        [Description("The scale of the penalty applied if a token is already present at all. Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double PresencePenalty { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Frequency Penalty")]
        [Description("The scale of the penalty for how often a token is used. Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double FrequencyPenalty { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("top p")]
        [Description("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double TopP { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Stop Sequences")]
        [Description("Up to 4 sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence. Separate different stop strings by a comma e.g. '},;,stop'")]
        [DefaultValue("")]
        public string StopSequences { get; set; } = string.Empty;

        #endregion ChatGPT Model Parameters   

        #region Unakin Model Parameters

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Temperature")]
        [Description("What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 for ones with a well-defined answer.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public double UnakinTemperature { get; set; } = 0;

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Max Token")]
        [Description("This number is fixed for Unakin, do not change it.The maximum number of tokens for the output.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public int UnakinMaxTokens { get; set; } = 4096;

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Run TimeTopP")]
        [Description("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public double UnakinRunTimeTopP { get; set; } = 0.9;

        #endregion Unakin Model Parameters

        #region Advanced Unakin Model Parameters (Anti-Repeat Policy)

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Min Size")]
        [Description("This is the minimum size of the repeated token sequence to be avoided.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public int UnakinMinSize { get; set; } = 2;

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Max Size")]
        [Description("This is the maximum size of the repeated token sequence to be avoided.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public int UnakinMaxSize { get; set; } = 8;

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Min Repeat Portion")]
        [Description("This is the minimum proportion of the output tokens that have to be repeated for it to be considered a repeat.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public double UnakinMinRepSize { get; set; } = 0.5;

        [Category("Unakin Model Parameters")]
        [DisplayName("Unakin Max Repeat Portion")]
        [Description(" This is the number of times to retry if a repeat is detected.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        [Browsable(true)]
        public int UnakinMaxRepSize { get; set; } = 3;

        #endregion Advanced Unakin Model Parameters (Anti-Repeat Policy)

        #region Web Search - Google Search API

        [Category("Web Search")]
        [DisplayName("Search Engine Id")]
        [Description("Search engine Id used for web search")]
        [DefaultValue("")]
        [TypeConverter(typeof(StringConverter))]
        [Browsable(true)]
        public string SearchEngineId { get; set; } = string.Empty;

        [Category("Web Search")]
        [DisplayName("Search Engine API Key")]
        [Description("API key used for web search")]
        [DefaultValue("")]
        [TypeConverter(typeof(StringConverter))]
        [Browsable(true)]
        public string SearchEngineAPIKey { get; set; } = string.Empty;

        #endregion
    }


    /// <summary>
    /// Enum to represent Output Verbosity for logger.
    /// </summary>
    public enum OutputVerbosityOptions
    {
        Minimal,
        Detail,
        Extensive
    }
}
