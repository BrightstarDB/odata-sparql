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

namespace Microsoft.Data.Edm.Csdl.Internal.Parsing.Ast
{
    internal class CsdlIsTypeExpression : CsdlExpressionBase
    {
        private readonly CsdlTypeReference type;
        private readonly CsdlExpressionBase operand;

        public CsdlIsTypeExpression(CsdlTypeReference type, CsdlExpressionBase operand, CsdlLocation location)
            : base(location)
        {
            this.type = type;
            this.operand = operand;
        }

        public override Expressions.EdmExpressionKind ExpressionKind
        {
            get { return Expressions.EdmExpressionKind.IsType; }
        }

        public CsdlTypeReference Type
        {
            get { return this.type; }
        }

        public CsdlExpressionBase Operand
        {
            get { return this.operand; }
        }
    }
}
