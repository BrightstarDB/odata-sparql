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

using System.Linq;
using System.Text;
using Microsoft.Data.Edm.Library;

namespace Microsoft.Data.Edm
{
    /// <summary>
    /// Contains ToTraceString() extension methods.
    /// </summary>
    public static class ToTraceStringExtensionMethods
    {
        /// <summary>
        /// Returns the text representation of the current object.
        /// </summary>
        /// <param name="schemaType">Reference to the calling object.</param>
        /// <returns>The text representation of the current object.</returns>
        public static string ToTraceString(this IEdmSchemaType schemaType)
        {
            return ((IEdmSchemaElement)schemaType).ToTraceString();
        }

        /// <summary>
        /// Returns the text representation of the current object.
        /// </summary>
        /// <param name="schemaElement">Reference to the calling object.</param>
        /// <returns>The text representation of the current object.</returns>
        public static string ToTraceString(this IEdmSchemaElement schemaElement)
        {
            return schemaElement.FullName();
        }

        /// <summary>
        /// Returns the text representation of the current object.
        /// </summary>
        /// <param name="type">Reference to the calling object.</param>
        /// <returns>The text representation of the current object.</returns>
        public static string ToTraceString(this IEdmType type)
        {
            EdmUtil.CheckArgumentNull(type, "type");
            switch (type.TypeKind)
            {
                case EdmTypeKind.Collection:
                    return ((IEdmCollectionType)type).ToTraceString();
                case EdmTypeKind.EntityReference:
                    return ((IEdmEntityReferenceType)type).ToTraceString();
                case EdmTypeKind.Row:
                    return ((IEdmRowType)type).ToTraceString();
                default:
                    var schemaType = type as IEdmSchemaType;
                    return schemaType != null ? schemaType.ToTraceString() : EdmConstants.Value_UnknownType;
            }
        }

        /// <summary>
        /// Returns the text representation of the current object.
        /// </summary>
        /// <param name="type">Reference to the calling object.</param>
        /// <returns>The text representation of the current object.</returns>
        public static string ToTraceString(this IEdmTypeReference type)
        {
            EdmUtil.CheckArgumentNull(type, "type");
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            if (type.Definition != null)
            {
                sb.Append(type.Definition.ToTraceString());
                sb.AppendKeyValue(EdmConstants.FacetName_Nullable, type.IsNullable.ToString());
                if (type.IsPrimitive())
                {
                    sb.AppendFacets(type.AsPrimitive());
                }
            }

            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Returns the text representation of the current object.
        /// </summary>
        /// <param name="property">Reference to the calling object.</param>
        /// <returns>The text representation of the current object.</returns>
        public static string ToTraceString(this IEdmProperty property)
        {
            EdmUtil.CheckArgumentNull(property, "property");
            return (property.Name != null ? property.Name : "") + ":" + (property.Type != null ? property.Type.ToTraceString() : "");
        }

        private static string ToTraceString(this IEdmEntityReferenceType type)
        {
            return EdmTypeKind.EntityReference.ToString() + '(' + (type.EntityType != null ? type.EntityType.ToTraceString() : "") + ')';
        }

        private static string ToTraceString(this IEdmCollectionType type)
        {
            return EdmTypeKind.Collection.ToString() + '(' + (type.ElementType != null ? type.ElementType.ToTraceString() : "") + ')';
        }

        private static string ToTraceString(this IEdmRowType type)
        {
            StringBuilder sb = new StringBuilder(EdmTypeKind.Row.ToString());
            sb.Append('(');
            if (type.Properties().Any())
            {
                IEdmProperty lastProperty = type.Properties().Last();
                foreach (IEdmProperty prop in type.Properties())
                {
                    if (prop != null)
                    {
                        sb.Append(prop.ToTraceString());
                        if (!prop.Equals(lastProperty))
                        {
                            sb.Append(", ");
                        }
                    }
                }
            }

            sb.Append(')');
            return sb.ToString();
        }

        private static void AppendFacets(this StringBuilder sb, IEdmPrimitiveTypeReference type)
        {
            switch (type.PrimitiveKind())
            {
                case EdmPrimitiveTypeKind.Binary:
                    sb.AppendBinaryFacets(type.AsBinary());
                    break;
                case EdmPrimitiveTypeKind.Decimal:
                    sb.AppendDecimalFacets(type.AsDecimal());
                    break;
                case EdmPrimitiveTypeKind.String:
                    sb.AppendStringFacets(type.AsString());
                    break;
                case EdmPrimitiveTypeKind.Time:
                case EdmPrimitiveTypeKind.DateTime:
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    sb.AppendTemporalFacets(type.AsTemporal());
                    break;
                case EdmPrimitiveTypeKind.Geography:
                case EdmPrimitiveTypeKind.GeographyPoint:
                case EdmPrimitiveTypeKind.GeographyLineString:
                case EdmPrimitiveTypeKind.GeographyPolygon:
                case EdmPrimitiveTypeKind.GeographyCollection:
                case EdmPrimitiveTypeKind.GeographyMultiPolygon:
                case EdmPrimitiveTypeKind.GeographyMultiLineString:
                case EdmPrimitiveTypeKind.GeographyMultiPoint:
                case EdmPrimitiveTypeKind.Geometry:
                case EdmPrimitiveTypeKind.GeometryPoint:
                case EdmPrimitiveTypeKind.GeometryLineString:
                case EdmPrimitiveTypeKind.GeometryPolygon:
                case EdmPrimitiveTypeKind.GeometryCollection:
                case EdmPrimitiveTypeKind.GeometryMultiPolygon:
                case EdmPrimitiveTypeKind.GeometryMultiLineString:
                case EdmPrimitiveTypeKind.GeometryMultiPoint:
                    sb.AppendSpatialFacets(type.AsSpatial());
                    break;
            }
        }

        private static void AppendBinaryFacets(this StringBuilder sb, IEdmBinaryTypeReference type)
        {
            sb.AppendKeyValue(EdmConstants.FacetName_FixedLength, type.IsFixedLength.ToString());
            if (type.IsUnbounded || type.MaxLength != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_MaxLength, (type.IsUnbounded) ? EdmConstants.Value_Max : type.MaxLength.ToString());
            }
        }

        private static void AppendStringFacets(this StringBuilder sb, IEdmStringTypeReference type)
        {
            if (type.IsFixedLength != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_FixedLength, type.IsFixedLength.ToString());
            }

            if (type.IsUnbounded == true || type.MaxLength != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_MaxLength, (type.IsUnbounded) ? EdmConstants.Value_Max : type.MaxLength.ToString());
            }

            if (type.IsUnicode != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_Unicode, type.IsUnicode.ToString());
            }

            if (type.Collation != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_Collation, type.Collation.ToString());
            }
        }

        private static void AppendTemporalFacets(this StringBuilder sb, IEdmTemporalTypeReference type)
        {
            if (type.Precision != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_Precision, type.Precision.ToString());
            }
        }

        private static void AppendDecimalFacets(this StringBuilder sb, IEdmDecimalTypeReference type)
        {
            if (type.Precision != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_Precision, type.Precision.ToString());
            }

            if (type.Scale != null)
            {
                sb.AppendKeyValue(EdmConstants.FacetName_Scale, type.Scale.ToString());
            }
        }

        private static void AppendSpatialFacets(this StringBuilder sb, IEdmSpatialTypeReference type)
        {
            sb.AppendKeyValue(EdmConstants.FacetName_Srid, type.SpatialReferenceIdentifier != null ? type.SpatialReferenceIdentifier.ToString() : EdmConstants.Value_SridVariable);
        }

        private static void AppendKeyValue(this StringBuilder sb, string key, string value)
        {
            sb.Append(' ');
            sb.Append(key);
            sb.Append('=');
            sb.Append(value);
        }
    }
}
