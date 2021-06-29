// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Bicep.Core.Modules
{
    public class ModuleReferenceComparer : IEqualityComparer<ModuleReference>
    {
        public static readonly ModuleReferenceComparer Instance = new();

        public bool Equals(ModuleReference x, ModuleReference y)
        {
            throw new NotImplementedException();
        }

        public int GetHashCode(ModuleReference obj)
        {
            throw new NotImplementedException();
        }
    }
}
