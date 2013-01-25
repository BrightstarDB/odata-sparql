﻿using System;
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
        private string _defaultLanguageCode;

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
                    ProcessRoot(query.Query as EntitySetQueryNode);
                    break;
                case QueryNodeKind.KeyLookup:
                    ProcessRoot(query.Query as KeyLookupQueryNode);
                    break;
                case QueryNodeKind.Filter:
                    var entityType = (query.Query as FilterQueryNode).ItemType;
                    var instances = AssertInstancesVariable(entityType);
                    ProcessFilter(query.Query as FilterQueryNode);
                    _sparqlModel.AddSelectVariable(instances, entityType.FullName());
                    _sparqlModel.IsDescribe = true;
                    break;
                default:
                    throw new NotImplementedException("No processing implemented for " + query.Query.Kind);
            }
        }

        private void ProcessFilter(FilterQueryNode filterQuery)
        {
            var filterSparqlExpression = ProcessNode(filterQuery.Expression);
            _sparqlModel.CurrentGraphPattern.AddFilterExpression(filterSparqlExpression.ToString());
        }

        private void ProcessRoot(EntitySetQueryNode entitySet)
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
            _sparqlModel.AddSelectVariable(instancesVariable, entitySet.ItemType.FullName());
            _sparqlModel.IsDescribe = true;
        }

        private void ProcessRoot(KeyLookupQueryNode keyLookup)
        {
            var keyPropertyValues = keyLookup.KeyPropertyValues.ToList();
            if (keyPropertyValues.Count == 1)
            {
                var kpv = keyPropertyValues[0];
                var parentEntity = kpv.KeyProperty.DeclaringType as IEdmEntityType;
                if (parentEntity != null)
                {
                    string prefix;
                    if (_map.TryGetIdentifierPrefixForProperty(parentEntity.FullName(), kpv.KeyProperty.Name, out prefix))
                    {
                        object keyValue = ProcessNode(kpv.KeyValue);
                        if (keyValue != null)
                        {
                            _sparqlModel.SelectEntity(prefix + keyValue, parentEntity.FullName());
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
                    var constNode = queryNode as ConstantQueryNode;
                    return ProcessConstant(constNode.Value);
                case QueryNodeKind.Convert:
                    var convertNode = queryNode as ConvertQueryNode;
                    var sourceValue = ProcessNode(convertNode.Source);
                    if (convertNode.TargetType.IsString())
                    {
                        return sourceValue.ToString();
                    }
                    throw new NotImplementedException("Haven't yet implemented convert to types other than string");
                case QueryNodeKind.BinaryOperator:
                    var binaryOperatorNode = queryNode as BinaryOperatorQueryNode;
                    var left = ProcessNode(binaryOperatorNode.Left);
                    var right = ProcessNode(binaryOperatorNode.Right);
                    return BindOperator(binaryOperatorNode, left, right);
                case QueryNodeKind.PropertyAccess:
                    return ProcessNode(queryNode as PropertyAccessQueryNode);
                default:
                    throw new NotImplementedException("No support for " + queryNode.Kind);
            }
        }

        private object ProcessConstant(object value)
        {
            if (value is string)
            {
                var stringValue =  "'" + value + "'"; // TODO: Need proper escaping in here
                if (!String.IsNullOrEmpty(_defaultLanguageCode)) stringValue = stringValue + "@" + _defaultLanguageCode;
                return stringValue;
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

        private string BindOperator(BinaryOperatorQueryNode binaryOperatorNode, object left, object right)
        {
            switch (binaryOperatorNode.OperatorKind)
            {
                    case BinaryOperatorKind.Equal:
                    return left + " = " + right;
                default:
                    throw new NotImplementedException("No support for " + binaryOperatorNode.OperatorKind);
            }
        }
    }
}
