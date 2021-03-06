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

using Microsoft.Data.Edm.Expressions;

namespace Microsoft.Data.Edm.Library.Expressions
{
    /// <summary>
    /// Represents an EDM type assertion expression.
    /// </summary>
    public class EdmAssertTypeExpression : EdmElement, IEdmAssertTypeExpression
    {
        private readonly IEdmExpression operand;
        private readonly IEdmTypeReference type;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdmAssertTypeExpression"/> class.
        /// </summary>
        /// <param name="operand">Expression for which the type is asserted.</param>
        /// <param name="type">Type to assert.</param>
        public EdmAssertTypeExpression(IEdmExpression operand, IEdmTypeReference type)
        {
            EdmUtil.CheckArgumentNull(operand, "operand");
            EdmUtil.CheckArgumentNull(type, "type");

            this.operand = operand;
            this.type = type;
        }

        /// <summary>
        /// Gets the expression for which the type is asserted.
        /// </summary>
        public IEdmExpression Operand
        {
            get { return this.operand; }
        }

        /// <summary>
        /// Gets the asserted type.
        /// </summary>
        public IEdmTypeReference Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets the kind of this expression.
        /// </summary>
        public EdmExpressionKind ExpressionKind
        {
            get { return EdmExpressionKind.AssertType; }
        }
    }
}
