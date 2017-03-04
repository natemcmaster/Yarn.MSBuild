using Microsoft.DotNet.Cli.Utils;
using Yarn.MSBuild.Tests.Utilities;

namespace FluentAssertions
{
    public static class CommandResultExtensions
    {
        public static CommandResultAssertions Should(this CommandResult commandResult)
        {
            return new CommandResultAssertions(commandResult);
        }
    }
}
