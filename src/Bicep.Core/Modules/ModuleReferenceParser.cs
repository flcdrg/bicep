// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bicep.Core.Modules
{
    public static class ModuleReferenceParser
    {
        private delegate ModuleReference? ModuleReferenceParserDelegate(string rawValue, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder);

        private static readonly ImmutableDictionary<string, ModuleReferenceParserDelegate> schemeParsers = new Dictionary<string, ModuleReferenceParserDelegate>()
        {
            { "nuget", NugetModuleReference.TryParse},
            { "oci", OciArtifactModuleReference.TryParse }
        }.ToImmutableDictionary(StringComparer.Ordinal);

        public static ModuleReference? TryParse(string moduleReferenceString, out DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder)
        {
            var parts = moduleReferenceString.Split(':', 2, System.StringSplitOptions.None);
            switch (parts.Length)
            {
                case 1:
                    // local path reference
                    return LocalModuleReference.TryParse(parts[0], out failureBuilder);

                case 2:
                    var scheme = parts[0];

                    if (schemeParsers.TryGetValue(scheme, out var parserFunc))
                    {
                        // the scheme is recognized
                        var rawValue = parts[1];
                        return parserFunc(rawValue, out failureBuilder);
                    }

                    // unknown scheme
                    failureBuilder = x => x.UnknownModuleReferenceScheme(scheme, schemeParsers.Keys);
                    return null;

                default:
                    // empty string
                    failureBuilder = x => x.ModulePathHasNotBeenSpecified();
                    return null;
            }
        }
    }
}
