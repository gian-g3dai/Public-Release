using Community.VisualStudio.Toolkit;
using Unakin.Commands;

namespace Unakin
{
    [Command(PackageIds.CustomReplace)]
    internal sealed class CustomReplace : BaseGenericCommand<CustomReplace>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Replace;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.CustomReplace;
        }
    }
}
