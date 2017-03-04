using System;
using System.Collections.Generic;
using System.IO;
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
            => RunCommand("restore");

        public CommandResult Build(params string[] args)
            => RunCommand("build", args);

        public CommandResult Msbuild(params string[] args)
            => RunCommand("msbuild", args);

        public CommandResult Clean()
            => RunCommand("clean");

        private CommandResult RunCommand(string commandName, params string[] args)
        {
            var cmd = Command
                .CreateDotNet(commandName, args)
                .CaptureStdErr()
                .CaptureStdOut()
                .WorkingDirectory(Root.FullName);

            foreach (var env in _env)
            {
                cmd.EnvironmentVariable(env.Key, env.Value);
            }

            Console.WriteLine($"Running 'dotnet {commandName} {string.Join(" ", args)}' in '{Root.FullName}'");
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
