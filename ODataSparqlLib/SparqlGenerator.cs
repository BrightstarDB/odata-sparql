using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Edm;
using Microsoft.Data.OData.Query;

namespace ODataSparqlLib
{
    public class SparqlGenerator
    {
        private readonly SparqlMap _map;
        private SparqlModel _sparqlModel;
        private readonly string _defaultLanguageCode;

        public SparqlModel SparqlQueryModel { get { return _sparqlModel; } }

        public SparqlGenerator(SparqlMap map, string defaultLanguageCode= "")
        {
            _map = map;
            _defaultLanguageCode = defaultLanguageCode;
        }

        public void ProcessQuery(QueryDescriptorQueryNode query)
        {
            _sparqlModel = new SparqlModel();
            switch (query.Query.Kind)
            {
                case QueryNodeKind.EntitySet:
                    ProcessNode(query.Query as EntitySetQueryNode);
                    break;
                case QueryNodeKind.KeyLookup:
                    ProcessRoot(query.Query as KeyLookupQueryNode);
                    break;
                case QueryNodeKind.Filter:
                    var entityType = (query.Query as FilterQueryNode).ItemType;
                    var instances = AssertInstancesVariable(entityType);
                    ProcessFilter(query.Query as FilterQueryNode);
                    _sparqlModel.AddSelectVariable(instances, entityType.FullName(), true);
                    _sparqlModel.IsDescribe = true;
                    break;
                case QueryNodeKind.Top:
                    var top = query.Query as TopQueryNode;
                    ProcessNode(top.Collection);
                    var processedLimit = ProcessNode(top.Amount);
                    _sparqlModel.Limit = Convert.ToInt32(processedLimit);
                    break;
                case QueryNodeKind.Segment:
                    var navigation = query.Query as NavigationPropertyNode;
                    var finalVar = ProcessNode(navigation);
                    _sparqlModel.AddSelectVariable(
                        finalVar.ToString(), 
                        navigation.TypeReference.FullName(),
                        navigation.NavigationProperty.OwnMultiplicity() == EdmMultiplicity.Many);
                    _sparqlModel.IsDescribe = true;
                    break;
                default:
                    throw new NotImplementedException("No processing implemented for " + query.Query.Kind);
            }
        }

        private object ProcessNode(QueryNode queryNode)
        {
            switch (queryNode.Kind)
            {
                case QueryNodeKind.Constant:
                    return ProcessConstant(queryNode as ConstantQueryNode);
                case QueryNodeKind.EntitySet:
                    return ProcessNode(queryNode as EntitySetQueryNode);
                case QueryNodeKind.OrderBy:
                    return ProcessNode(queryNode as OrderByQueryNode);
                case QueryNodeKind.KeyLookup:
                    return ProcessKeyLookup(queryNode as KeyLookupQueryNode);
                default:
                    throw new NotImplementedException("No processing implemented for " + queryNode.Kind);
            }
        }

        private object ProcessKeyLookup(KeyLookupQueryNode keyLookup)
        {
            var keyPropertyValues = keyLookup.KeyPropertyValues.ToList();
            if (keyPropertyValues.Count == 1)
            {
                var kpv = keyPropertyValues[0];
                var rootEntity = kpv.KeyProperty.DeclaringType as IEdmEntityType; // Get the entity that declares the Id property
                if (rootEntity != null)
                {
                    string prefix;
                    if (_map.TryGetIdentifierPrefixForProperty(rootEntity.FullName(), kpv.KeyProperty.Name, out prefix))
                    {
                        object keyValue = ProcessNode(kpv.KeyValue);
                        if (keyValue != null)
                        {
                            return new Uri(prefix + keyValue);
                        }
                    }
                }
            }
            throw new Exception("Could not process key lookup");
        }

        private object ProcessNode(NavigationPropertyNode navigation)
        {
            var source = ProcessNode(navigation.Source);
            if (source != null)
            {
                IPatternItem sourcePatternItem =
                    source is Uri ? (IPatternItem) new UriPatternItem(source.ToString()) : new VariablePatternItem(source.ToString());

                string propertyUri;
                bool isInverse;
                string targetVar = _sparqlModel.NextVariable();
                _map.TryGetUriForNavigationProperty(navigation.NavigationProperty.DeclaringEntityType().FullName(),
                                                    navigation.NavigationProperty.Name,
                                                    out propertyUri,
                                                    out isInverse);
                if (isInverse)
                {
                    // Target is the subject of a triple
                    _sparqlModel.CurrentGraphPattern.Add(
                        new TriplePattern(new VariablePatternItem(targetVar),
                                          new UriPatternItem(propertyUri),
                                          sourcePatternItem));
                }
                else
                {
                    _sparqlModel.CurrentGraphPattern.Add(
                        new TriplePattern(sourcePatternItem,
                            new UriPatternItem(propertyUri),
                            new VariablePatternItem(targetVar)));
                }
                return targetVar;
            }
            throw new Exception("Cannot process navigation node as source could not be processed.");
        }
        private object ProcessNode(OrderByQueryNode orderByQuery)
        {
            // TODO: This currently assumes a single property lookup
            var ret = ProcessNode(orderByQuery.Collection);
            var expression = ProcessNode(orderByQuery.Expression).ToString();
            if (expression.StartsWith("?"))
            {
                _sparqlModel.Ordering = new SparqlVariableOrdering(expression.TrimStart('?'),
                                                                   orderByQuery.Direction == OrderByDirection.Descending);
            }
            else
            {
                throw new Exception("No handling for SPARQL expression ordering yet");
            }
            return ret;
        }

        private void ProcessFilter(FilterQueryNode filterQuery)
        {
            var filterSparqlExpression = ProcessNode(filterQuery.Expression);
            _sparqlModel.CurrentGraphPattern.AddFilterExpression(filterSparqlExpression.ToString());
        }

        private string ProcessNode(EntitySetQueryNode entitySet)
        {
            var entitySetType = _map.GetUriForType(entitySet.EntitySet.ElementType.FullName());
            if (entitySetType == null)
            {
                // Throw exception
            }
            var instancesVariable = _sparqlModel.NextVariable();
            _sparqlModel.RootGraphPattern.Add(
                new TriplePattern(
                    new VariablePatternItem(instancesVariable),
                    new UriPatternItem("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"),
                    new UriPatternItem(entitySetType)
                    ));
            _sparqlModel.AddSelectVariable(instancesVariable, entitySet.ItemType.FullName(), true);
            _sparqlModel.IsDescribe = true;
            return instancesVariable;
        }

        private void ProcessRoot(KeyLookupQueryNode keyLookup)
        {
            var keyPropertyValues = keyLookup.KeyPropertyValues.ToList();
            if (keyPropertyValues.Count == 1)
            {
                var kpv = keyPropertyValues[0];
                var rootEntity = kpv.KeyProperty.DeclaringType as IEdmEntityType; // Get the entity that declares the Id property
                if (rootEntity != null)
                {
                    string prefix;
                    if (_map.TryGetIdentifierPrefixForProperty(rootEntity.FullName(), kpv.KeyProperty.Name, out prefix))
                    {
                        object keyValue = ProcessNode(kpv.KeyValue);
                        if (keyValue != null)
                        {
                            _sparqlModel.SelectEntity(
                                prefix + keyValue, keyLookup.Collection.ItemType.FullName());
                        }
                    }
                }
            }
        }

        private object ProcessNode(SingleValueQueryNode queryNode)
        {
            switch (queryNode.Kind)
            {
                case QueryNodeKind.Constant:
                    return ProcessConstant(queryNode as ConstantQueryNode);
                case QueryNodeKind.Convert:
                    var convertNode = queryNode as ConvertQueryNode;
                    var sourceValue = ProcessNode(convertNode.Source);
                    if (convertNode.TargetType.IsInt32())
                    {
                        return Convert.ToInt32(sourceValue);
                    }
                    if (convertNode.TargetType.IsString())
                    {
                        return sourceValue.ToString();
                    }
                    throw new NotImplementedException("Haven't yet implemented convert to types other than string");
                case QueryNodeKind.BinaryOperator:
                    var binaryOperatorNode = queryNode as BinaryOperatorQueryNode;
                    return BindOperator(binaryOperatorNode);
                case QueryNodeKind.PropertyAccess:
                    return ProcessNode(queryNode as PropertyAccessQueryNode);
                default:
                    throw new NotImplementedException("No support for " + queryNode.Kind);
            }
        }

        private object ProcessConstant(ConstantQueryNode constNode)
        {
            return constNode == null ? null : constNode.Value;
        }

        private string MakeSparqlConstant(object value, string languageCode = null)
        {
            if (value is string)
            {
                var stringConstant = "'" + value + "'";
                if (languageCode == null) languageCode = _defaultLanguageCode;
                if (languageCode != null) stringConstant += "@" + languageCode;
                return stringConstant;
            }
            throw new NotImplementedException("No SPARQL conversion defined for constant value of type " + value.GetType());
        }
        private object ProcessNode(PropertyAccessQueryNode propertyAccessQueryNode)
        {
            var srcVariable = AssertInstancesVariable(propertyAccessQueryNode.Source.TypeReference);
            var propertyUri = _map.GetUriForProperty(propertyAccessQueryNode.Source.TypeReference.FullName(),
                                                     propertyAccessQueryNode.Property.Name);
            return "?" + AssertPropertyAccessVariable(srcVariable, propertyUri);
        }

        private string AssertInstancesVariable(IEdmTypeReference typeReference)
        {
            var typeUri = _map.GetUriForType(typeReference.FullName());
            var existingVar = _sparqlModel.CurrentGraphPattern.TriplePatterns
                        .Where(
                            p => p.Subject is VariablePatternItem
                                 && p.Object is UriPatternItem && p.Predicate is UriPatternItem &&
                                 (p.Predicate as UriPatternItem).Uri.Equals(RdfConstants.RdfType) &&
                                 (p.Object as UriPatternItem).Uri.Equals(typeUri))
                        .Select(p => (p.Subject as VariablePatternItem).VariableName)
                        .FirstOrDefault();
            if (existingVar != null)
            {
                return existingVar;
            }
            var instancesVar = _sparqlModel.NextVariable();
            _sparqlModel.CurrentGraphPattern.TriplePatterns.Add(
                new TriplePattern(
                    new VariablePatternItem(instancesVar),
                    new UriPatternItem(RdfConstants.RdfType),
                    new UriPatternItem(typeUri)
                    ));
            return instancesVar;
        }

        private string AssertPropertyAccessVariable(string srcVariable, string propertyTypeUri)
        {
            var existing = _sparqlModel.CurrentGraphPattern.TriplePatterns
                                       .Where(
                                           p => p.Subject is VariablePatternItem &&
                                                p.Predicate is UriPatternItem &&
                                                p.Object is VariablePatternItem &&
                                                (p.Subject as VariablePatternItem).VariableName.Equals(srcVariable) &&
                                                (p.Predicate as UriPatternItem).Uri.Equals(propertyTypeUri)
                )
                                       .Select(p => (p.Object as VariablePatternItem).VariableName).FirstOrDefault();
            if (existing != null) return existing;
            var propertyVar = _sparqlModel.NextVariable();
            _sparqlModel.CurrentGraphPattern.TriplePatterns.Add(
                new TriplePattern(new VariablePatternItem(srcVariable),
                                  new UriPatternItem(propertyTypeUri),
                                  new VariablePatternItem(propertyVar)));
            return propertyVar;
        }

        private string BindOperator(BinaryOperatorQueryNode binaryOperatorNode)
        {
            var left = binaryOperatorNode.Left is ConstantQueryNode
                           ? MakeSparqlConstant((binaryOperatorNode.Left as ConstantQueryNode).Value)
                           : ProcessNode(binaryOperatorNode.Left);
            var right = binaryOperatorNode.Right is ConstantQueryNode
                            ? MakeSparqlConstant((binaryOperatorNode.Right as ConstantQueryNode).Value)
                            : ProcessNode(binaryOperatorNode.Right);
            switch (binaryOperatorNode.OperatorKind)
            {
                case BinaryOperatorKind.Equal:
                    return left + " = " + right;
                case BinaryOperatorKind.GreaterThan:
                    return left + " > " + right;
                default:
                    throw new NotImplementedException("No support for " + binaryOperatorNode.OperatorKind);
            }
        }
    }
}
