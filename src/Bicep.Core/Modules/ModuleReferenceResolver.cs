// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Core.Extensions;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Syntax;
using System;
using System.IO;
using System.Linq;

namespace Bicep.Core.Modules
{
    public class ModuleReferenceResolver : IModuleReferenceResolver
    {
        private readonly IFileResolver fileResolver;

        public ModuleReferenceResolver(IFileResolver fileResolver)
        {
            this.fileResolver = fileResolver;
        }

        public ModuleReference? TryGetModuleReference(ModuleDeclarationSyntax moduleDeclarationSyntax, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var moduleReferenceString = SyntaxHelper.TryGetModulePath(moduleDeclarationSyntax, out var getModulePathFailureBuilder);
            if (moduleReferenceString is null)
            {
                failureBuilder = getModulePathFailureBuilder;
                return null;
            }

            return ModuleReferenceParser.TryParse(moduleReferenceString, out failureBuilder);
        }

        public Uri? TryGetModulePath(Uri parentFileUri, ModuleDeclarationSyntax moduleDeclarationSyntax, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var moduleReference = this.TryGetModuleReference(moduleDeclarationSyntax, out var parseFailureBuilder);
            if(moduleReference is null)
            {
                failureBuilder = parseFailureBuilder;
                return null;
            }

            return ResolvePath(parentFileUri, moduleReference, out failureBuilder);
        }

        private Uri? ResolvePath(Uri parentFileUri, ModuleReference moduleReference, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            switch(moduleReference)
            {
                case LocalModuleReference localRef:
                    var localUri = fileResolver.TryResolveModulePath(parentFileUri, localRef.Path);
                    if (localUri is not null)
                    {
                        failureBuilder = null;
                        return localUri;
                    }

                    failureBuilder = x => x.ModulePathCouldNotBeResolved(localRef.Path, parentFileUri.LocalPath);
                    return null;

                case OciArtifactModuleReference ociRef:
                    string localArtifactPath = GetLocalPackagePath(ociRef);
                    if (Uri.TryCreate(localArtifactPath, UriKind.Absolute, out var uri))
                    {
                        failureBuilder = null;
                        return uri;
                    }

                    throw new NotImplementedException($"Local OCI artifact path is malformed: \"{localArtifactPath}\"");

                default:
                    throw new NotImplementedException($"Unexpected module reference type {moduleReference.GetType().Name}.");
            }
        }

        private static string GetLocalPackagePath(OciArtifactModuleReference reference)
        {
            // TODO: Will NOT work if user profile is not loaded on Windows! (Az functions load exes like that)
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var baseDirectories = new[]
            {
                basePath,
                ".bicep",
                "artifacts",
                reference.Registry
            };

            // TODO: Directory convention problematic. /foo/bar:baz and /foo:bar will share directories
            var directories = baseDirectories
                .Concat(reference.Repository.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Append(reference.Tag)
                .ToArray();

            return Path.Combine(baseDirectories);
        }
    }
}
