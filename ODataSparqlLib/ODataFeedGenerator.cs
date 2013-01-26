using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query;

namespace ODataSparqlLib
{
    public class ODataFeedGenerator
    {
        private readonly IODataResponseMessage _request;
        private readonly SparqlMap _map;
        private readonly string _baseUri;
        private readonly ODataMessageWriterSettings _writerSettings;

        public ODataFeedGenerator(IODataResponseMessage requestMessage, SparqlMap entityMap, string baseUri, ODataMessageWriterSettings messageWriterSettings)
        {
            _request = requestMessage;
            _map = entityMap;
            _baseUri = baseUri;
            _writerSettings = messageWriterSettings;
        }

        public void CreateFeedFromGraph(IGraph resultsGraph, string entityType)
        {
            var msgWriter = new ODataMessageWriter(_request, _writerSettings, _map.Model);
            var feedWriter = msgWriter.CreateODataFeedWriter();
            var entries = new List<ODataEntry>();

            var typeUri = _map.GetUriForType(entityType);
            if (!String.IsNullOrEmpty(typeUri))
            {
                var predNode = resultsGraph.CreateUriNode(UriFactory.Create(RdfConstants.RdfType));
                var objNode = resultsGraph.CreateUriNode(UriFactory.Create(typeUri));
                foreach (var instanceTriple in resultsGraph.GetTriplesWithPredicateObject(predNode, objNode))
                {
                    var instanceUri = (instanceTriple.Subject as IUriNode).Uri;
                    entries.Add(CreateODataEntry(resultsGraph, instanceUri.ToString(), entityType));
                }
            }

            var feed = new ODataFeed {Count = entries.Count, Id = _baseUri + _map.GetTypeSet(entityType)};
            feedWriter.WriteStart(feed);
            foreach (var entry in entries)
            {
                feedWriter.WriteStart(entry);
                feedWriter.WriteEnd();
            }
            feedWriter.WriteEnd();
            feedWriter.Flush();
        }

        public void CreateFeedFromResultSet(SparqlResultSet resultSet)
        {
            throw new NotImplementedException();
        }

        public void CreateEntryFromGraph(IGraph resultsGraph, string entryResource, string entryType)
        {
            var msgWriter = new ODataMessageWriter(_request, _writerSettings, _map.Model);
            var entryWriter = msgWriter.CreateODataEntryWriter();
            var entry = CreateODataEntry(resultsGraph, entryResource, entryType);
            entryWriter.WriteStart(entry);
            entryWriter.WriteEnd();
            entryWriter.Flush();
        }

        private ODataEntry CreateODataEntry(IGraph resultsGraph, string entryResource, string entryType)
        {
            var idPrefix = _map.GetResourceUriPrefix(entryType);
            if (!entryResource.StartsWith(idPrefix))
            {
                // Now we have a problem
                throw new Exception("Cannot create entry feed for resource " + entryResource +
                                    ". Resource URI does not start with the expected prefix " + idPrefix);
            }
            var resourceId = entryResource.Substring(idPrefix.Length);
            var odataLink = _baseUri + _map.GetTypeSet(entryType) + "('" + resourceId + "')";
            var entry = new ODataEntry
                {
                    TypeName = entryType,
                    ReadLink = new Uri(odataLink),
                    Id = odataLink
                };
            var subject = resultsGraph.CreateUriNode(UriFactory.Create(entryResource));
            var properties = new List<ODataProperty>();
            foreach (var propertyMapping in _map.GetStructuralPropertyMappings(entryType))
            {
                var predicate = resultsGraph.CreateUriNode(UriFactory.Create(propertyMapping.Uri));
                var match = resultsGraph.GetTriplesWithSubjectPredicate(subject, predicate).FirstOrDefault();
                if (match != null)
                {
                    if (match.Object is LiteralNode)
                    {
                        var newProperty = new ODataProperty
                            {
                                Name = propertyMapping.Name,
                                Value = GetValue(match.Object, propertyMapping.PropertyType)
                            };
                        properties.Add(newProperty);
                    }
                }
            }

            entry.AssociationLinks =
                _map.GetAssociationPropertyMappings(entryType)
                    .Select(m => new ODataAssociationLink {Name = m.Name, Url = new Uri(odataLink + "/" + m.Name)});
            entry.Properties = properties;
            return entry;
        }


        private static object GetValue(INode valueNode, IEdmTypeReference propertyType)
        {
            switch (propertyType.Definition.TypeKind)
            {
                case EdmTypeKind.Primitive:
                    try
                    {
                        return GetPrimitiveValue(valueNode.AsValuedNode(), propertyType.Definition as IEdmPrimitiveType,
                                                 propertyType.IsNullable);
                    }
                    catch (RdfException)
                    {
                        // Caught an error during typed value conversion so try a conversion on just the string value
                        if (valueNode is ILiteralNode)
                        {
                            var literalValue = (valueNode as ILiteralNode).Value;
                            return GetPrimitiveValue(literalValue, propertyType.Definition as IEdmPrimitiveType,
                                                     propertyType.IsNullable);
                        }
                        throw;
                    }
                default:
                    throw new NotImplementedException("No support for conversion to property type kind " +
                                                      propertyType.Definition.TypeKind);
            }
        }

        private static object GetPrimitiveValue(IValuedNode valuedNode, IEdmPrimitiveType targetType, bool asNullable)
        {
            // TODO: Some sort of cast to nullable when necessary
                switch (targetType.PrimitiveKind)
                {
                    case EdmPrimitiveTypeKind.Boolean:
                        return valuedNode.AsBoolean();
                    case EdmPrimitiveTypeKind.Byte:
                        return (byte) valuedNode.AsInteger();
                    case EdmPrimitiveTypeKind.DateTime:
                        return valuedNode.AsDateTime();
                    case EdmPrimitiveTypeKind.Decimal:
                        return valuedNode.AsDecimal();
                    case EdmPrimitiveTypeKind.Double:
                        return valuedNode.AsDouble();
                    case EdmPrimitiveTypeKind.Int16:
                        return (Int16) valuedNode.AsInteger();
                    case EdmPrimitiveTypeKind.Int32:
                        return (Int32) valuedNode.AsInteger();
                    case EdmPrimitiveTypeKind.Int64:
                        return valuedNode.AsInteger();
                    case EdmPrimitiveTypeKind.String:
                        return valuedNode.AsString();
                    default:
                        throw new NotSupportedException(
                            String.Format("Support for primitive type {0} has not been implemented yet",
                                          targetType.PrimitiveKind));
                }
        }

        private static object GetPrimitiveValue(string value, IEdmPrimitiveType targetType, bool asNullable)
        {
            // TODO: Some sort of cast to nullable when necessary
            switch (targetType.PrimitiveKind)
            {
                case EdmPrimitiveTypeKind.Boolean:
                    return Convert.ToBoolean(value);
                case EdmPrimitiveTypeKind.Byte:
                    return Convert.ToByte(value);
                case EdmPrimitiveTypeKind.DateTime:
                    return Convert.ToDateTime(value);
                case EdmPrimitiveTypeKind.Decimal:
                    return Convert.ToDecimal(value);
                case EdmPrimitiveTypeKind.Double:
                    return Convert.ToDouble(value);
                case EdmPrimitiveTypeKind.Int16:
                    return Convert.ToInt16(value);
                case EdmPrimitiveTypeKind.Int32:
                    return Convert.ToInt32(value);
                case EdmPrimitiveTypeKind.Int64:
                    return Convert.ToInt64(value);
                case EdmPrimitiveTypeKind.String:
                    return value;
                default:
                    throw new NotSupportedException(
                        String.Format("Support for primitive type {0} has not been implemented yet",
                                      targetType.PrimitiveKind));
            }
        }
    }
}
