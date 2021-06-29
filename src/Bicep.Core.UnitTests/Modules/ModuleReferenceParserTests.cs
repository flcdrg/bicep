// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Modules;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.Modules
{
    [TestClass]
    public class ModuleReferenceParserTests
    {
        [DataRow("./test.bicep")]
        [DataRow("foo/bar/test.bicep")]
        [DataRow("../bar/test.bicep")]
        [DataTestMethod]
        public void ValidLocalReference_ShouldParse(string value)
        {
            var reference = ModuleReferenceParser.TryParse(value, out var failureBuilder);
            reference.Should().BeOfType<LocalModuleReference>();
            failureBuilder.Should().BeNull();

            var typed = (LocalModuleReference)reference!;
            typed.Path.Should().Be(value);
        }

        [DataRow("nuget:My.Package@1.0", "My.Package", "1.0")]
        [DataRow("nuget:My.Package.Something@1.2.3-preview", "My.Package.Something", "1.2.3-preview")]
        [DataTestMethod]
        public void ValidNugetReference_ShouldParse(string value, string expectedPackageId, string expectedVersion)
        {
            var reference = ModuleReferenceParser.TryParse(value, out var failureBuilder);
            reference.Should().BeOfType<NugetModuleReference>();
            failureBuilder.Should().BeNull();

            var typed = (NugetModuleReference)reference!;
            typed.PackageId.Should().Be(expectedPackageId);
            typed.Version.Should().Be(expectedVersion);
        }

        [DataRow("oci:myacr.azurecr.io/foo/bar:v1.0", "myacr.azurecr.io", "/foo/bar", "v1.0")]
        [DataRow("oci:localhost:5000/foo/bar:v1.0", "localhost:5000", "/foo/bar", "v1.0")]
        [DataTestMethod]
        public void ValidOciReference_ShouldParse(string value, string expectedRegistry, string expectedRepository, string expectedTag)
        {
            var reference = ModuleReferenceParser.TryParse(value, out var failureBuilder);
            reference.Should().BeOfType<OciArtifactModuleReference>();
            failureBuilder.Should().BeNull();

            var typed = (OciArtifactModuleReference)reference!;
            typed.Registry.Should().Be(expectedRegistry);
            typed.Repository.Should().Be(expectedRepository);
            typed.Tag.Should().Be(expectedTag);
        }
    }
}
