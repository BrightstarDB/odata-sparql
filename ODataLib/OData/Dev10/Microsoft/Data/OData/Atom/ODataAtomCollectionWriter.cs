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

namespace Microsoft.Data.OData.Atom
{
    #region Namespaces
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
#if ODATALIB_ASYNC
    using System.Threading.Tasks;
#endif
    using System.Xml;
    using Microsoft.Data.Edm;
    using o = Microsoft.Data.OData;
    #endregion Namespaces

    /// <summary>
    /// ODataCollectionWriter for the ATOM format.
    /// </summary>
    internal sealed class ODataAtomCollectionWriter : ODataCollectionWriterCore
    {
        /// <summary>The output context to write to.</summary>
        private readonly ODataAtomOutputContext atomOutputContext;

        /// <summary>The collection serializer to use for writing.</summary>
        private readonly ODataAtomCollectionSerializer atomCollectionSerializer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="atomOutputContext">The output context to write to.</param>
        /// <param name="expectedItemType">The type reference of the expected item type or null if no expected item type exists.</param>
        /// <param name="listener">If not null, the writer will notify the implementer of the interface of relevant state changes in the writer.</param>
        internal ODataAtomCollectionWriter(ODataAtomOutputContext atomOutputContext, IEdmTypeReference expectedItemType, IODataReaderWriterListener listener)
            : base(atomOutputContext, expectedItemType, listener)
        {
            DebugUtils.CheckNoExternalCallers();
            Debug.Assert(atomOutputContext != null, "atomOutputContext != null");

            this.atomOutputContext = atomOutputContext;
            this.atomCollectionSerializer = new ODataAtomCollectionSerializer(atomOutputContext);
        }

        /// <summary>
        /// Check if the object has been disposed; called from all public API methods. Throws an ObjectDisposedException if the object
        /// has already been disposed.
        /// </summary>
        protected override void VerifyNotDisposed()
        {
            this.atomOutputContext.VerifyNotDisposed();
        }

        /// <summary>
        /// Flush the output.
        /// </summary>
        protected override void FlushSynchronously()
        {
            this.atomOutputContext.Flush();
        }

#if ODATALIB_ASYNC
        /// <summary>
        /// Flush the output.
        /// </summary>
        /// <returns>Task representing the pending flush operation.</returns>
        protected override Task FlushAsynchronously()
        {
            return this.atomOutputContext.FlushAsync();
        }
#endif

        /// <summary>
        /// Start writing an OData payload.
        /// </summary>
        protected override void StartPayload()
        {
            this.atomCollectionSerializer.WritePayloadStart();
        }

        /// <summary>
        /// Finish writing an OData payload.
        /// </summary>
        protected override void EndPayload()
        {
            // This method is only called if no error has been written so it is safe to
            // call WriteEndDocument() here (since it closes all open elements which we don't want in error state)
            this.atomCollectionSerializer.WritePayloadEnd();
        }

        /// <summary>
        /// Start writing a collection.
        /// </summary>
        /// <param name="collectionStart">The <see cref="ODataCollectionStart"/> representing the collection.</param>
        protected override void StartCollection(ODataCollectionStart collectionStart)
        {
            Debug.Assert(collectionStart != null, "collection != null");

            string collectionName = collectionStart.Name;
            if (collectionName == null)
            {
                // null collection names are not allowed in ATOM
                throw new ODataException(o.Strings.ODataAtomCollectionWriter_CollectionNameMustNotBeNull);
            }

            // Note that we don't perform metadata validation of the name of the collection.
            // This is because there are multiple possibilities (service operation, action, function, top-level property)
            // and without more information we can't know which one to look for.

            // <collectionName>
            this.atomOutputContext.XmlWriter.WriteStartElement(collectionName, this.atomCollectionSerializer.MessageWriterSettings.WriterBehavior.ODataNamespace);

            // xmlns:="ODataNamespace"
            this.atomOutputContext.XmlWriter.WriteAttributeString(
                AtomConstants.XmlnsNamespacePrefix,
                AtomConstants.XmlNamespacesNamespace,
                this.atomCollectionSerializer.MessageWriterSettings.WriterBehavior.ODataNamespace);

            this.atomCollectionSerializer.WriteDefaultNamespaceAttributes(
                ODataAtomSerializer.DefaultNamespaceFlags.ODataMetadata |
                ODataAtomSerializer.DefaultNamespaceFlags.Gml |
                ODataAtomSerializer.DefaultNamespaceFlags.GeoRss);
        }

        /// <summary>
        /// Finish writing a collection.
        /// </summary>
        protected override void EndCollection()
        {
            // </collectionName>
            this.atomOutputContext.XmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Writes a collection item (either primitive or complex)
        /// </summary>
        /// <param name="item">The collection item to write.</param>
        /// <param name="expectedItemType">The expected type of the collection item or null if no expected item type exists.</param>
        protected override void WriteCollectionItem(object item, IEdmTypeReference expectedItemType)
        {
            // <d:element>
            this.atomOutputContext.XmlWriter.WriteStartElement(AtomConstants.ODataCollectionItemElementName, this.atomCollectionSerializer.MessageWriterSettings.WriterBehavior.ODataNamespace);

            if (item == null)
            {
                ValidationUtils.ValidateNullCollectionItem(expectedItemType, this.atomOutputContext.MessageWriterSettings.WriterBehavior);

                // NOTE can't use ODataAtomWriterUtils.WriteNullAttribute because that method assumes the
                //      default 'm' prefix for the metadata namespace.
                this.atomOutputContext.XmlWriter.WriteAttributeString(
                    AtomConstants.ODataNullAttributeName,
                    AtomConstants.ODataMetadataNamespace,
                    AtomConstants.AtomTrueLiteral);
            }
            else
            {
                ODataComplexValue complexValue = item as ODataComplexValue;
                if (complexValue != null)
                {
                    this.atomCollectionSerializer.AssertRecursionDepthIsZero();
                    this.atomCollectionSerializer.WriteComplexValue(
                        complexValue,
                        expectedItemType,
                        false /* isOpenPropertyType */,
                        true  /* isWritingCollection */,
                        null  /* beforePropertiesAction */,
                        null  /* afterPropertiesAction */,
                        this.DuplicatePropertyNamesChecker,
                        this.CollectionValidator,
                        null  /* epmValueCache */,
                        null  /* epmSourcePathSegment */,
                        null  /* projectedProperties */);
                    this.atomCollectionSerializer.AssertRecursionDepthIsZero();
                    this.DuplicatePropertyNamesChecker.Clear();
                }
                else
                {
                    Debug.Assert(!(item is ODataCollectionValue), "!(item is ODataCollectionValue)");
                    Debug.Assert(!(item is ODataStreamReferenceValue), "!(item is ODataStreamReferenceValue)");
                    this.atomCollectionSerializer.WritePrimitiveValue(item, this.CollectionValidator, expectedItemType);
                }
            }

            // </d:element>
            this.atomOutputContext.XmlWriter.WriteEndElement();
        }
    }
}
