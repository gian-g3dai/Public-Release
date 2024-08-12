using Community.VisualStudio.Toolkit;
using Unakin.Commands;

namespace Unakin
{
    [Command(PackageIds.CustomAfter)]
    internal sealed class CustomAfter : BaseGenericCommand<CustomAfter>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.CustomAfter;
        }
    }
}
