using Community.VisualStudio.Toolkit;
using Unakin.Commands;
using Unakin.Utils;

namespace Unakin
{
    [Command(PackageIds.AddSummary)]
    internal sealed class AddSummary : BaseGenericCommand<AddSummary>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            if (string.IsNullOrWhiteSpace(OptionsCommands.AddSummary))
            {
                return string.Empty;
            }

            return OptionsCommands.AddSummary;
        }
    }
}
