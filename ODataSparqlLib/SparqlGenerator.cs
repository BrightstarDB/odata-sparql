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

        public SparqlModel SparqlQueryModel { get { return _sparqlModel; } }

        public SparqlGenerator(SparqlMap map)
        {
            _map = map;
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
                default:
                    throw new NotImplementedException("No processing implemented for " + query.Query.Kind);
            }
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
                    return constNode.Value;
                case QueryNodeKind.Convert:
                    var convertNode = queryNode as ConvertQueryNode;
                    var sourceValue = ProcessNode(convertNode.Source);
                    if (convertNode.TargetType.IsString())
                    {
                        return sourceValue.ToString();
                    }
                    throw new NotImplementedException("Haven't yet implemented convert to types other than string");
                default:
                    throw new NotImplementedException("No support for " + queryNode.Kind);
            }
        }
    }
}
