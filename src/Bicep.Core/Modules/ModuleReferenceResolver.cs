// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Core.Extensions;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bicep.Core.Modules
{
    public class ModuleReferenceResolver : IModuleReferenceResolver
    {
        private readonly IFileResolver fileResolver;

        private readonly OrasClient orasClient;

        public ModuleReferenceResolver(IFileResolver fileResolver)
        {
            this.fileResolver = fileResolver;
            this.orasClient = new OrasClient(GetArtifactCachePath());
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

        public void DownloadExternalReferences(IEnumerable<ModuleReference> references)
        {
            foreach(var reference in references)
            {
                switch(reference)
                {
                    case OciArtifactModuleReference ociRef:
                        this.PullArtifact(ociRef);
                        break;

                    default:
                        throw new InvalidOperationException($"External references of type {reference.GetType().Name} are not supported.");
                }
            }
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
                    string localArtifactPath = this.orasClient.GetLocalPackagePath(ociRef);
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

        private void PullArtifact(OciArtifactModuleReference reference)
        {
            this.orasClient.Pull(reference);
        }

        private static string GetArtifactCachePath()
        {
            // TODO: Will NOT work if user profile is not loaded on Windows! (Az functions load exes like that)
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(basePath, ".bicep", "artifacts");
        }
    }
}
