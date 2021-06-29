// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Syntax;
using System;
using System.Collections.Generic;

namespace Bicep.Core.Modules
{
    public interface IModuleReferenceResolver
    {
        ModuleReference? TryGetModuleReference(ModuleDeclarationSyntax moduleDeclarationSyntax, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);

        void DownloadExternalReferences(IEnumerable<ModuleReference> references);

        Uri? TryGetModulePath(Uri parentFileUri, ModuleDeclarationSyntax moduleDeclarationSyntax, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);
    }
}
