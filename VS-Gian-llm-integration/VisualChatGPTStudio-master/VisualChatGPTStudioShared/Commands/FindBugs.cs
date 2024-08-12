using Community.VisualStudio.Toolkit;
using Unakin.Commands;

namespace Unakin
{
    [Command(PackageIds.FindBugs)]
    internal sealed class FindBugs : BaseGenericCommand<FindBugs>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.FindBugs;
        }
    }
}
