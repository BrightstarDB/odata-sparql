using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Query;

namespace ODataSparqlLib
{
    public class SparqlModel
    {
        private int _variableCounter = 1;

        public SparqlModel()
        {
            RootGraphPattern = new GraphPattern();
            SelectVariables = new List<string>();
            VariableType = new Dictionary<string, string>();
            CurrentGraphPattern = RootGraphPattern;
        }

        public GraphPattern RootGraphPattern { get; private set; }
        public List<string> SelectVariables { get; private set; }
        public Dictionary<string, string> VariableType { get; private set; }
        public bool IsDescribe { get; set; }
        public string DescribeResource { get; private set; }
        public int? Limit { get; set; }
        public ISparqlOrdering Ordering { get; set; }
        public GraphPattern CurrentGraphPattern { get; private set; }

        public string NextVariable()
        {
            return "v" + (_variableCounter++);
        }


        public void AddSelectVariable(string variableName, string entityType)
        {
            if (!SelectVariables.Contains(variableName))
            {
                SelectVariables.Add(variableName);
                VariableType[variableName] = entityType;
            }
        }

        public string GetSparqlRepresentation()
        {
            if (!String.IsNullOrEmpty(DescribeResource))
            {
                return String.Format("CONSTRUCT {{ <{0}> ?p ?o }} WHERE {{ <{0}> ?p ?o }}", DescribeResource);
            }
            var queryBuilder = new StringBuilder();
            if (SelectVariables.Count > 0)
            {
                if (IsDescribe)
                {
                    queryBuilder.Append("CONSTRUCT {");
                    foreach (var sv in SelectVariables)
                    {
                        queryBuilder.AppendFormat("?{0} ?{0}_p ?{0}_o . ", sv);
                        RootGraphPattern.Add(new TriplePattern(new VariablePatternItem(sv), new VariablePatternItem(sv+"_p"), new VariablePatternItem(sv+"_o")  ));
                    }
                    queryBuilder.Append("} ");
                }
                else
                {
                    queryBuilder.Append("SELECT ");
                    foreach (string sv in SelectVariables)
                    {
                        queryBuilder.AppendFormat("?{0} ", sv);
                    }
                }
            }
            else
            {
                queryBuilder.Append("SELECT * ");
            }
            queryBuilder.Append("WHERE { ");
            queryBuilder.Append(RootGraphPattern.GetSparqlRepresentation());
            queryBuilder.Append("} ");

            if (Ordering != null)
            {
                queryBuilder.Append("ORDER BY ");
                queryBuilder.Append(Ordering.GetSparqlRepresentation());
                queryBuilder.Append(" ");
            }

            if (Limit.HasValue)
            {
                queryBuilder.AppendFormat("LIMIT {0} ", Limit);
            }
            return queryBuilder.ToString();
        }

        public void Execute(SparqlRemoteEndpoint endpoint, ODataFeedGenerator handler)
        {
            if (IsDescribe)
            {
                string query = GetSparqlRepresentation();
                IGraph resultsGraph = endpoint.QueryWithResultGraph(query);
                if (!String.IsNullOrEmpty(DescribeResource))
                {
                    // Create ODATA entry payload for single resource
                    handler.CreateEntryFromGraph(resultsGraph, DescribeResource, GetEntityType(DescribeResource));
                }
                else
                {
                    if (IsDescribe)
                    {
                        if (SelectVariables.Count > 1)
                        {
                            throw new Exception(
                                "Cannot create an entity feed from a SPARQL query with multiple DESCRIBE bindings");
                        }
                        handler.CreateFeedFromGraph(resultsGraph, VariableType[SelectVariables[0]], this);
                    }
                }
            }
            else
            {
                SparqlResultSet resultSet = endpoint.QueryWithResultSet(GetSparqlRepresentation());
                handler.CreateFeedFromResultSet(resultSet);
            }
        }

        public void SelectEntity(string entityResource, string entityType)
        {
            DescribeResource = entityResource;
            IsDescribe = true;
            VariableType[entityResource] = entityType;
        }

        public string GetEntityType(string variableOrResource)
        {
            return VariableType[variableOrResource];
        }
    }

    public interface ISparqlOrdering
    {
        bool IsSimple { get; }
        ISparqlOrdering ThenBy { get; set; }
        string Expression { get; }
        string Variable { get; }
        bool IsDescending { get; }
        string GetSparqlRepresentation();
    }

    public class SparqlVariableOrdering : ISparqlOrdering
    {
        public SparqlVariableOrdering(string variable, bool descending)
        {
            Variable = variable;
            Expression = null;
            IsDescending = descending;
        }

        public bool IsDescending { get; private set; }
        public bool IsSimple { get { return ThenBy == null || ThenBy.IsSimple; } }
        public ISparqlOrdering ThenBy { get; set; }
        public string Expression { get; private set; }
        public string Variable { get; private set; }

        public string GetSparqlRepresentation()
        {
            if (IsDescending)
            {
                return "DESC(?" + Variable + ")";
            }
            return "?" + Variable;
        }
    }

    public class SparqlExpressionOrdering : ISparqlOrdering
    {
        public bool IsSimple { get; private set; }
        public ISparqlOrdering ThenBy { get; set; }
        public string Expression { get; private set; }
        public string Variable { get; private set; }
        public bool IsDescending { get; private set; }
        public string GetSparqlRepresentation()
        {
            throw new NotImplementedException();
        }
    }
}