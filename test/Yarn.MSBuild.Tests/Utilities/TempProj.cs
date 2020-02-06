// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Xunit.Abstractions;

namespace Yarn.MSBuild.Tests.Utilities
{
    public class TempProj : TempDir
    {
        private readonly IReadOnlyDictionary<string, string> _env;
        private readonly ITestOutputHelper _output;
        private bool _cleanup;

        public TempProj(IReadOnlyDictionary<string, string> env, string root, ITestOutputHelper output)
         : base(root)
        {
            _env = env;
            _output = output;
        }

        public CommandResult Build(params string[] args)
            => RunCommand(new[] { "-restore", "-t:Build", "-clp:Summary" }.Concat(args).ToArray());

        public CommandResult Msbuild(params string[] args)
            => RunCommand(args);

        private CommandResult RunCommand(params string[] args)
        {
            Command cmd;
            string commandName;
#if NETCOREAPP3_1
            commandName = "dotnet msbuild";
            cmd = Command.CreateDotNet("msbuild", args);
#elif NET472
            commandName = GetVisualStudioMsbuild();
            cmd = Command.Create(commandName, args);
#else
#error Target frameworks need to be updated
#endif
            cmd
                .CaptureStdErr()
                .CaptureStdOut()
                .OnOutputLine(l => _output.WriteLine(l))
                .OnErrorLine(l => _output.WriteLine(l))
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

        private string GetVisualStudioMsbuild()
        {
            var msbuildInVs = Path.Combine(VisualStudioHelper.InstallationPath, "MSBuild", "15.0", "Bin", "msbuild.exe");
            return File.Exists(msbuildInVs)
                ? msbuildInVs
                : "msbuild";
        }

        public override void Dispose()
        {
            if (_cleanup)
            {
                base.Dispose();
            }
        }
    }
}
