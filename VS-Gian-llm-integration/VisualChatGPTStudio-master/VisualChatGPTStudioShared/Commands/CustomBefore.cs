using Community.VisualStudio.Toolkit;
using Unakin.Commands;

namespace Unakin
{
    [Command(PackageIds.CustomBefore)]
    internal sealed class CustomBefore : BaseGenericCommand<CustomBefore>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.CustomBefore;
        }
    }
}
