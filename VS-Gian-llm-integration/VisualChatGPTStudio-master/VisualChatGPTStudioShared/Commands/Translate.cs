using Community.VisualStudio.Toolkit;
using Unakin;
using Unakin.Commands;

namespace UnakinShared.Commands
{
    [Command(PackageIds.Translate)]
    internal sealed class Translate : BaseGenericCommand<Translate>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Replace;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.Translate;
        }
    }
}
