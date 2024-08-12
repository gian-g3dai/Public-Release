using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;
using System.Windows.Shapes;
using System.Windows;
using System;

namespace Unakin.Options
{
    /// <summary>
    /// Represents a class that provides a dialog page for displaying commands options.
    /// </summary>
    [ComVisible(true)]
    public class OptionPageGridCommands : DialogPage
    {
       internal static Dictionary<int, Tuple<string,string,bool>> Commands = new Dictionary<int, Tuple<string, string,bool>>()
        {
            { Unakin.Utils.Constants.SEMANTICSEARCH_ID,new Tuple<string,string,bool>(Unakin.Utils.Constants.SEMANTICSEARCH_NAME,Unakin.Utils.Constants.SEMANTICSEARCH_DESC,true)},
            { 4,new Tuple<string,string,bool>("Complete","Complete the following script",false) },
            { 5,new Tuple<string,string,bool>("Add Tests","Create unit tests for the following script",false) },
            { 6,new Tuple<string,string,bool>("Find Bugs" ,"Find Bugs in the following script", false)},
            { 7,new Tuple<string,string,bool>("Optimize","Optimize the following script",false) },
            { 8,new Tuple<string,string,bool>("Explain","Explain the following script",false) },
            { 9,new Tuple < string, string ,bool>("Add Summary", "Add a summary. Only write a comment as C# summary format-like for the following script",false) },
            //{ 10,new Tuple < string, string,bool >("Add Comments", "Add comments to the script",false) },
            //{ 11,new Tuple < string, string,bool >("Add Comments for multiple lines", "Rewrite the code with comments. Add comment char for each comment line",false) },
            { 11,new Tuple < string, string,bool >("Add Comments", "Add Comments to the following script",false) },
           { 12,new Tuple < string, string ,bool>("Translate", "Translate to English the following script",false) },
           { Unakin.Utils.Constants.LOCALWORKFLOW_ID,new Tuple < string, string,bool >(Unakin.Utils.Constants.LOCALWORKFLOW_NAME, Unakin.Utils.Constants.LOCALWORKFLOW_DESC,true) },
           { Unakin.Utils.Constants.PROJECTSUMMARY_ID,new Tuple < string, string,bool >(Unakin.Utils.Constants.PROJECTSUMMARY_NAME, Unakin.Utils.Constants.PROJECTSUMMARY_DESC,true) },
           { Unakin.Utils.Constants.AUTOMATEDTESTING_ID,new Tuple < string, string,bool >(Unakin.Utils.Constants.AUTOMATEDTESTING_NAME, Unakin.Utils.Constants.AUTOMATEDTESTING_NAME,true) },
           { Unakin.Utils.Constants.AUTONOMOUSAGENT_ID,new Tuple < string, string,bool >(Unakin.Utils.Constants.AUTONOMOUSAGENT_NAME, Unakin.Utils.Constants.AUTONOMOUSAGENT_NAME,false) },
           { Unakin.Utils.Constants.DATAGEN_ID,new Tuple < string, string,bool >(Unakin.Utils.Constants.DATAGEN_NAME, Unakin.Utils.Constants.DATAGEN_NAME,true) }
        };

        [Category("Commands")]
        [DisplayName("Semantic Search")]
        [Description("Set the \"Semantic Search\" command")]
        [DefaultValue("Semantic Search")]
        public string SemanticSearch { get; set; } = Commands[3].Item2;

        [Category("Commands")]
        [DisplayName("Complete")]
        [Description("Set the \"Complete\" command")]
        [DefaultValue("Complete the following script")]
        public string Complete { get; set; } = Commands[4].Item2;

        [Category("Commands")]
        [DisplayName("Add Tests")]
        [Description("Set the \"Add Tests\" command")]
        [DefaultValue("Create unit tests for the following script")]
        public string AddTests { get; set; } = Commands[5].Item2;

        [Category("Commands")]
        [DisplayName("Find Bugs")]
        [Description("Set the \"Find Bugs\" command")]
        [DefaultValue("Find Bugs in the following script")]
        public string FindBugs { get; set; } = Commands[6].Item2;

        [Category("Commands")]
        [DisplayName("Optimize")]
        [Description("Set the \"Optimize\" command")]
        [DefaultValue("Optimize the following script")]
        public string Optimize { get; set; } = Commands[7].Item2;

        [Category("Commands")]
        [DisplayName("Explain")]
        [Description("Set the \"Explain\" command")]
        [DefaultValue("Explain the following script")]
        public string Explain { get; set; } = Commands[8].Item2;

        [Category("Commands")]
        [DisplayName("Add Summary")]
        [Description("Set the \"Add Summary\" command")]
        [DefaultValue("Add a summary. Only write a comment as C# summary format-like for the following script")]
        public string AddSummary { get; set; } = Commands[9].Item2;

        /*
        
        [Category("Commands")]
        [DisplayName("Add Comments")]
        [Description("Set the \"Add Comments\" command when one line was selected")]
        [DefaultValue("Add comments to the script")]
        public string AddCommentsForLine { get; set; } = Commands[10].Item2;

        [Category("Commands")]
        [DisplayName("Add Comments for multiple lines")]
        [Description("Set the \"Add Comments\" command when multiple lines was selected")]
        [DefaultValue("Rewrite the code with comments. Add comment char for each comment line")]
        public string AddCommentsForLines { get; set; } = Commands[11].Item2;

        */

        [Category("Commands")]
        [DisplayName("Add Comments")]
        [Description("Set the \"Add Comments\" command when multiple lines was selected")]
        [DefaultValue("Add Comments to the following script")]
        public string AddComments { get; set; } = Commands[11].Item2;

        [Category("Commands")]
        [DisplayName("Translate")]
        [Description("Set the \"Translate\" command")]
        [DefaultValue("Translate to English the following script")]
        public string Translate { get; set; } = Commands[12].Item2;

        [Category("Commands")]
        [DisplayName("Custom command Before")]
        [Description("Define a custom command that will insert the response before the selected text")]
        [DefaultValue("")]
        public string CustomBefore { get; set; }

        [Category("Commands")]
        [DisplayName("Custom command After")]
        [Description("Define a custom command that will insert the response after the selected text")]
        [DefaultValue("")]
        public string CustomAfter { get; set; }

        [Category("Commands")]
        [DisplayName("Custom command Replace")]
        [Description("Define a custom command that will replace the selected text with the response")]
        [DefaultValue("")]
        public string CustomReplace { get; set; }

        [Category("Commands")]
        [DisplayName(Unakin.Utils.Constants.PROJECTSUMMARY_DESC)]
        [Description("Get Project Summary")]
        [DefaultValue(Unakin.Utils.Constants.PROJECTSUMMARY_NAME)]
        public string ProjectSummary { get; set; } = Commands[Unakin.Utils.Constants.PROJECTSUMMARY_ID].Item2;

        [Category("Commands")]
        [DisplayName(Unakin.Utils.Constants.AUTOMATEDTESTING_DESC)]
        [Description("Get Project Summary")]
        [DefaultValue(Unakin.Utils.Constants.AUTOMATEDTESTING_NAME)]
        public string AutomatedTesting { get; set; } = Commands[Unakin.Utils.Constants.AUTOMATEDTESTING_ID].Item2;

        [Category("Commands")]
        [DisplayName(Unakin.Utils.Constants.AUTONOMOUSAGENT_DESC)]
        [Description("Autonomous Agents")]
        [DefaultValue(Unakin.Utils.Constants.AUTONOMOUSAGENT_NAME)]
        public string AutonomousAgent { get; set; } = Commands[Unakin.Utils.Constants.AUTONOMOUSAGENT_ID].Item2;

        [Category("Commands")]
        [DisplayName(Unakin.Utils.Constants.DATAGEN_DESC)]
        [Description("Get Project Summary")]
        [DefaultValue(Unakin.Utils.Constants.DATAGEN_NAME)]
        public string DataGeneration { get; set; } = Commands[Unakin.Utils.Constants.DATAGEN_ID].Item2;
    }
}
