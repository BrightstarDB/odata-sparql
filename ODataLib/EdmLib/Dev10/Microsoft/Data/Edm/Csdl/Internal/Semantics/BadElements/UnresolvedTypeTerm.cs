//   Copyright 2011 Microsoft Corporation
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Data.Edm.Csdl.Internal.CsdlSemantics
{
    internal class UnresolvedTypeTerm : UnresolvedVocabularyTerm, IEdmEntityType
    {
        public UnresolvedTypeTerm(string qualifiedName)
            : base(qualifiedName)
        {
        }

        public IEnumerable<IEdmStructuralProperty> DeclaredKey
        {
            get { return Enumerable.Empty<IEdmStructuralProperty>(); }
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsOpen
        {
            get { return false; }
        }

        public IEdmStructuredType BaseType
        {
            get { return null; }
        }

        public IEnumerable<IEdmProperty> DeclaredProperties
        {
            get { return Enumerable.Empty<IEdmProperty>(); }
        }

        public EdmTypeKind TypeKind
        {
            get { return EdmTypeKind.Entity; }
        }

        public override EdmSchemaElementKind SchemaElementKind
        {
            get { return EdmSchemaElementKind.TypeDefinition; }
        }

        public override EdmTermKind TermKind
        {
            get { return EdmTermKind.Type; }
        }

        public IEdmProperty FindProperty(string name)
        {
            return null;
        }
    }
}
