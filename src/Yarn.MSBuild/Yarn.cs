// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Yarn.MSBuild
{
    /// <summary>
    /// A task for invoking the bundled version of yarn
    /// </summary>
    public class Yarn : ToolTask
    {
        private const string YarnExeName = "yarn";

        /// <summary>
        /// Command line arguments to be passed to yarn
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The current working directory for the yarn process
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Override the path to the yarn executable. When empty, the task binds the version bundled in with this task.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path to the nodejs executable. If not specified, this task will expect node to be in PATH.
        /// </summary>
        public string NodeJsExecutablePath { get; set; }

        /// <summary>
        /// Ignore the exit code of yarn.
        /// </summary>
        public bool IgnoreExitCode { get; set; }

        protected override string ToolName => YarnExeName;

        protected override string GenerateCommandLineCommands() => Command;

        protected override ProcessStartInfo GetProcessStartInfo(string pathToTool, string commandLineCommands, string responseFileSwitch)
        {
            var startInfo = base.GetProcessStartInfo(pathToTool, commandLineCommands, responseFileSwitch);
            startInfo.Environment["PATH"] = GetPath();
            return startInfo;
        }

        protected override bool HandleTaskExecutionErrors()
        {
            return IgnoreExitCode
                ? true
                : base.HandleTaskExecutionErrors();
        }

        protected override string GetWorkingDirectory()
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                return Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(WorkingDirectory))
            {
                Log.LogError("WorkingDirectory does not exist: '{0}", WorkingDirectory);
                return null;
            }

            return WorkingDirectory;
        }

        protected override string GenerateFullPathToTool()
        {
            if (string.IsNullOrEmpty(ExecutablePath))
            {
                var exe = FindBundledYarn();

                if (!File.Exists(exe))
                {
                    Log.LogMessage("Failed to find the version of yarn bundled in this package. [{0}]", exe);
                    return YarnExeName;
                }

                return exe;
            }
            else if (!File.Exists(ExecutablePath))
            {
                Log.LogWarning(
                    "The file path set on ExecutablePath does not exist: '{0}'. " +
                    "Falling back to using the system PATH.", ExecutablePath);
                return YarnExeName;
            }
            else
            {
                return ExecutablePath;
            }
        }

        private string GetPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(NodeJsExecutablePath))
            {
                if (!Path.IsPathRooted(NodeJsExecutablePath))
                {
                    Log.LogWarning(nameof(NodeJsExecutablePath) + " is not an absolute path, so its value is being ignored. Pass in an absolute path to nodejs instead.");
                }
                else
                {
                    // First check if this path is a directory. If not, assume it was a filepath and get the directory containing it
                    var nodeDir = Directory.Exists(NodeJsExecutablePath)
                            ? NodeJsExecutablePath
                            : Path.GetDirectoryName(NodeJsExecutablePath);

                    // prepend the node directory so it is found first in the system lookup for nodejs
                    path = nodeDir + Path.PathSeparator + path;
                    Log.LogMessage(MessageImportance.Low, "Adding {0} to the system PATH", nodeDir);
                }
            }
            return path;
        }

        private string FindBundledYarn()
        {
            var assembly = typeof(Yarn).GetTypeInfo().Assembly.Location;
            var nugetRoot = new FileInfo(assembly) // Yarn.MSBuild.dll
                .Directory // tfm
                .Parent // tools
                .Parent.FullName; // nuget package

            var yarn = Path.Combine(nugetRoot, "dist", "bin", "yarn");
            if (IsWindows())
            {
                yarn += ".cmd";
            }
            return yarn;
        }

        private static bool IsWindows()
        {
#if NET46
            // This means the task is running on MSBuild.exe, which is usually only Windows.
            return Type.GetType("Mono.Runtime", throwOnError: false) == null;
#elif NETSTANDARD1_5
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
#error Target framework needs to be updated
#endif
        }
    }
}
