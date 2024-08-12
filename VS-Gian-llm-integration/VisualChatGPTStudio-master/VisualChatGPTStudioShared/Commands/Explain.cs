using Community.VisualStudio.Toolkit;
using Unakin.Commands;

namespace Unakin
{
    [Command(PackageIds.Explain)]
    internal sealed class Explain : BaseGenericCommand<Explain>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.Explain;
        }
    }
}
