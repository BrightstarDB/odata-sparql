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
using System.Globalization;
using Microsoft.Data.OData.Query;

namespace Microsoft.Data.OData
{
    #region Namespaces
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    #endregion Namespaces

    /// <summary>
    /// Uri utility methods.
    /// </summary>
    internal static class UriUtils
    {
        /// <summary>
        /// Returns an absolute URI constructed from the specified base URI and a relative URI
        /// </summary>
        /// <param name="baseUri">The base URI to use.</param>
        /// <param name="relativeUri">The relative URI to use.</param>
        /// <returns>The absolute URI as a result of combining the base URI with the relative URI.</returns>
        internal static Uri UriToAbsoluteUri(Uri baseUri, Uri relativeUri)
        {
            DebugUtils.CheckNoExternalCallers();
            Debug.Assert(baseUri != null, "baseUri != null");
            Debug.Assert(baseUri.IsAbsoluteUri, "baseUri is not absolute.");
            Debug.Assert(relativeUri != null, "relativeUri != null");
            Debug.Assert(!relativeUri.IsAbsoluteUri, "relativeUri is not relative.");

            return new Uri(baseUri, relativeUri);
        }

        /// <summary>
        /// A method to ensure that the original string of a relative URI is escaped.
        /// </summary>
        /// <param name="uri">The relative <see cref="System.Uri"/> to escape.</param>
        /// <returns>A relative URI instance with guaranteed escaped original string.</returns>
        [SuppressMessage("DataWeb.Usage", "AC0010", Justification = "Usage of OriginalString is safe in this context")]
        internal static Uri EnsureEscapedRelativeUri(Uri uri)
        {
            DebugUtils.CheckNoExternalCallers();
            Debug.Assert(uri != null && !uri.IsAbsoluteUri, "uri != null && !uri.IsAbsoluteUri");

            string escapedRelativeUri = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            if (string.CompareOrdinal(uri.OriginalString, escapedRelativeUri) == 0)
            {
                return uri;
            }

            return new Uri(escapedRelativeUri, UriKind.Relative);
        }

        /// <summary>
        /// Determines whether the <paramref name="baseUri"/> Uri instance is a 
        /// base of the specified Uri instance. 
        /// </summary>
        /// <param name="baseUri">The candidate base URI.</param>
        /// <param name="uri">The specified Uri instance to test.</param>
        /// <returns>true if the baseUri Uri instance is a base of uri; otherwise false.</returns>
        internal static bool UriInvariantInsensitiveIsBaseOf(Uri baseUri, Uri uri)
        {
            DebugUtils.CheckNoExternalCallers();
            Debug.Assert(baseUri != null, "baseUri != null");
            Debug.Assert(uri != null, "uri != null");

            Uri upperCurrent = CreateBaseComparableUri(baseUri);
            Uri upperUri = CreateBaseComparableUri(uri);

            return IsBaseOf(upperCurrent, upperUri);
        }

        /// <summary>
        /// Parses query options from a specified URI into a dictionary.
        /// </summary>
        /// <param name="uri">The uri to get the query options from.</param>
        /// <returns>The parsed query options.</returns>
        /// <remarks>This method returns <see cref="List{T}"/> with all the query options.
        /// Note that it is valid to include multiple query options with the same name.</remarks>
        internal static List<QueryOptionQueryToken> ParseQueryOptions(Uri uri)
        {
            DebugUtils.CheckNoExternalCallers();
            Debug.Assert(uri != null, "uri != null");

            List<QueryOptionQueryToken> queryOptions = new List<QueryOptionQueryToken>();

            string queryString = uri.Query.Replace('+', ' ');
            int length;
            if (queryString != null)
            {
                if (queryString.Length > 0 && queryString[0] == '?')
                {
                    queryString = queryString.Substring(1);
                }

                length = queryString.Length;
            }
            else
            {
                length = 0;
            }

            for (int i = 0; i < length; i++)
            {
                int startIndex = i;
                int equalSignIndex = -1;
                while (i < length)
                {
                    char ch = queryString[i];
                    if (ch == '=')
                    {
                        if (equalSignIndex < 0)
                        {
                            equalSignIndex = i;
                        }
                    }
                    else if (ch == '&')
                    {
                        break;
                    }

                    i++;
                }

                string queryOptionsName = null;
                string queryOptionValue = null;
                if (equalSignIndex >= 0)
                {
                    queryOptionsName = queryString.Substring(startIndex, equalSignIndex - startIndex);
                    queryOptionValue = queryString.Substring(equalSignIndex + 1, (i - equalSignIndex) - 1);
                }
                else
                {
                    queryOptionValue = queryString.Substring(startIndex, i - startIndex);
                }

                queryOptionsName = queryOptionsName == null ? null : Uri.UnescapeDataString(queryOptionsName).Trim();
                queryOptionValue = queryOptionValue == null ? null : Uri.UnescapeDataString(queryOptionValue).Trim();

                queryOptions.Add(new QueryOptionQueryToken(queryOptionsName, queryOptionValue));

                if ((i == (length - 1)) && (queryString[i] == '&'))
                {
                    queryOptions.Add(new QueryOptionQueryToken(null, string.Empty));
                }
            }

            return queryOptions;
        }


        /// <summary>Creates a URI suitable for host-agnostic comparison purposes.</summary>
        /// <param name="uri">URI to compare.</param>
        /// <returns>URI suitable for comparison.</returns>
        private static Uri CreateBaseComparableUri(Uri uri)
        {
            Debug.Assert(uri != null, "uri != null");

            uri = new Uri(UriUtilsCommon.UriToString(uri).ToUpper(CultureInfo.InvariantCulture), UriKind.RelativeOrAbsolute);

            UriBuilder builder = new UriBuilder(uri);
            builder.Host = "h";
            builder.Port = 80;
            builder.Scheme = "http";
            return builder.Uri;
        }

        /// <summary>
        /// Check whether the <paramref name="baseUri"/> Uri is the base of the <paramref name="uri"/> Uri.
        /// </summary>
        /// <param name="baseUri">The candidate base Uri.</param>
        /// <param name="uri">The Uri to check.</param>
        /// <returns>True if the <paramref name="baseUri"/> is the base of the <paramref name="uri"/> Uri.</returns>
        private static bool IsBaseOf(Uri baseUri, Uri uri)
        {
            return baseUri.IsBaseOf(uri);
        }
    }
}
