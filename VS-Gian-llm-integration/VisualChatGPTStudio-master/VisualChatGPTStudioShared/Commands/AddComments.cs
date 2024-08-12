﻿using Community.VisualStudio.Toolkit;
using System.Runtime.InteropServices.WindowsRuntime;
using Unakin.Commands;
using Unakin.Options.Commands;

namespace Unakin
{
    [Command(PackageIds.AddComments)]
    internal sealed class AddComments : BaseGenericCommand<AddComments>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            /*
            if (CodeContainsMultipleLines(selectedText))
            {
                return CommandType.Replace;
            }
            */

            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            /*
            if (CodeContainsMultipleLines(selectedText))
            {
                return OptionsCommands.AddCommentsForLines;
            }
            */

            //return OptionsCommands.AddCommentsForLine;

            return OptionsCommands.AddComments;

        }
            

        private bool CodeContainsMultipleLines(string code)
        {
            return code.Contains("\r\n") || code.Contains("\n") || code.Contains("\r");
        }
    }
}
