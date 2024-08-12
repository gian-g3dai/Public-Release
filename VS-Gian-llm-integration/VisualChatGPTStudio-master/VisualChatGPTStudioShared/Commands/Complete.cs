﻿using Community.VisualStudio.Toolkit;
using EnvDTE;
using Unakin.Utils;

namespace Unakin.Commands
{
    [Command(PackageIds.Complete)]
    internal sealed class Complete : BaseGenericCommand<Complete>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return string.Empty;
            }

            return TextFormat.FormatForCompleteCommand(OptionsCommands.Complete, docView.FilePath);
        }
    }
}
