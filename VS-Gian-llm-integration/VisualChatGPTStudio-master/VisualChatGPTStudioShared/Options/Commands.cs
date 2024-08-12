﻿namespace Unakin.Options.Commands
{
    /// <summary>
    /// Represents a class that contains various commands.
    /// </summary>
    public class Commands
    {
        public string ProjectName { get; set; }
        public string Complete { get; set; }
        public string AddTests { get; set; }
        public string FindBugs { get; set; }
        public string Optimize { get; set; }
        public string Explain { get; set; }
        public string AddSummary { get; set; }
        
        //public string AddCommentsForLine { get; set; }
        //public string AddCommentsForLines { get; set; }

        public string AddComments { get; set; }
        public string Translate { get; set; }
        public string CustomBefore { get; set; }
        public string CustomAfter { get; set; }
        public string CustomReplace { get; set; }
    }
}