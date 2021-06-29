// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Bicep.Core.Modules
{
    public class OrasClient
    {
        private readonly string artifactCachePath;

        public OrasClient(string artifactCachePath)
        {
            this.artifactCachePath = artifactCachePath;
        }

        public void Pull(OciArtifactModuleReference reference)
        {
            string localArtifactPath = GetLocalPackagePath(reference);

            // ensure that the directory exists
            Directory.CreateDirectory(localArtifactPath);

            using var process = new Process
            {
                // TODO: What about spaces? Do we need escaping?
                StartInfo = new("oras", $"pull {reference.ArtifactId} -a")
                {
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    LoadUserProfile = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,

                    // ORAS uses the CWD to download artifacts
                    WorkingDirectory = localArtifactPath
                }
            };

            var output = new StringBuilder();
            process.OutputDataReceived += (sender, e) => output.AppendLine(e.Data);

            var error = new StringBuilder();
            process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);

            process.Start();
            process.WaitForExit();

            if(process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Pull failed\nStdOut: {output}\nStdErr: {error}");
            }
        }

        public string GetLocalPackagePath(OciArtifactModuleReference reference)
        {
            var baseDirectories = new[]
            {
                this.artifactCachePath,
                reference.Registry
            };

            // TODO: Directory convention problematic. /foo/bar:baz and /foo:bar will share directories
            var directories = baseDirectories
                .Concat(reference.Repository.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Append(reference.Tag)
                .ToArray();

            return Path.Combine(directories);
        }
    }
}
