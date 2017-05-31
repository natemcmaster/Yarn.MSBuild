using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Yarn.MSBuild.Tests.Utilities
{
    public class TempProj : TempDir
    {
        private readonly IReadOnlyDictionary<string, string> _env;
        private bool _cleanup;

        public TempProj(IReadOnlyDictionary<string, string> env, string root)
         : base(root)
        {
            _env = env;
        }

        public CommandResult Restore()
            => RunCommand("/t:Restore");

        public CommandResult Build(params string[] args)
            => RunCommand(new[] { "/t:Build" }.Concat(args).ToArray());

        public CommandResult Msbuild(params string[] args)
            => RunCommand(args);

        private CommandResult RunCommand(params string[] args)
        {
            Command cmd;
            string commandName;
#if NETCOREAPP1_1
            commandName = "dotnet msbuild";
            cmd = Command.CreateDotNet("msbuild", args);
#elif NET461
            commandName = "msbuild";
            cmd = Command.Create("msbuild", args);
#else
#error Target frameworks need to be updated
#endif
            cmd
                .CaptureStdErr()
                .CaptureStdOut()
                .WorkingDirectory(Root.FullName);

            foreach (var env in _env)
            {
                cmd.EnvironmentVariable(env.Key, env.Value);
            }

            Console.WriteLine($"Running '{commandName} {string.Join(" ", args)}' in '{Root.FullName}'");
            return cmd.Execute();
        }

        public void Done()
            => _cleanup = true;

        public override void Dispose()
        {
            if (_cleanup)
            {
                base.Dispose();
            }
        }
    }
}
