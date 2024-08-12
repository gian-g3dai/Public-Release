using Community.VisualStudio.Toolkit;
using Unakin.Commands;

namespace Unakin
{
    [Command(PackageIds.AddTests)]
    internal sealed class AddTests : BaseGenericCommand<AddTests>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.AddTests;
        }
    }
}
