using System;
using System.Collections.Generic;
using System.Linq;
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
            VariableType = new Dictionary<string, SparqlVariableInfo>();
            CurrentGraphPattern = RootGraphPattern;
        }

        public GraphPattern RootGraphPattern { get; private set; }
        public List<string> SelectVariables { get; private set; }
        public Dictionary<string, SparqlVariableInfo> VariableType { get; private set; }
        public bool IsDescribe { get; set; }
        public string DescribeResource { get; private set; }
        public int? Offset { get; set; }

        /// <summary>
        /// Get / set the limit value originally provided in the OData query
        /// </summary>
        public int? OriginalLimit { get; set; }

        /// <summary>
        /// Get / set the actual limit used in the SPARQL query after server-side page limit has been applied
        /// </summary>
        public int? Limit { get; set; }
        public ISparqlOrdering Ordering { get; set; }
        public GraphPattern CurrentGraphPattern { get; private set; }

        public string NextVariable()
        {
            return "v" + (_variableCounter++);
        }


        public void AddSelectVariable(string variableName, string entityType, bool isCollection)
        {
            if (!SelectVariables.Contains(variableName))
            {
                SelectVariables.Add(variableName);
                VariableType[variableName] = new SparqlVariableInfo{EntityType = entityType, IsCollection = isCollection};
            }
        }

        public string GetSparqlRepresentation()
        {
            if (!String.IsNullOrEmpty(DescribeResource))
            {
                return String.Format("CONSTRUCT {{ <{0}> ?p ?o }} WHERE {{ <{0}> ?p ?o }}", DescribeResource);
            }
            var queryBuilder = new StringBuilder();
            bool inSubquery = false;
            if (SelectVariables.Count > 0)
            {
                if (IsDescribe)
                {
                    queryBuilder.Append("CONSTRUCT {");
                    foreach (var sv in SelectVariables)
                    {
                        queryBuilder.AppendFormat("?{0} ?{0}_p ?{0}_o . ", sv);
                        queryBuilder.AppendFormat(
                            "?{0} <http://brightstardb.com/odata-sparql/variable-binding> \"{0}\"", sv);
                    }
                    queryBuilder.Append("} WHERE { ");
                    foreach (var sv in SelectVariables.Distinct())
                    {
                        queryBuilder.AppendFormat("?{0} ?{0}_p ?{0}_o . ", sv);
                    }
                    queryBuilder.Append("{");
                    inSubquery = true;
                }
                queryBuilder.Append("SELECT ");
                foreach (string sv in SelectVariables)
                {
                    queryBuilder.AppendFormat("?{0} ", sv);
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
            if (inSubquery)
            {
                queryBuilder.Append("} }");
            }
            return queryBuilder.ToString();
        }

        public string GetSparqlCountQuery()
        {
            if (SelectVariables.Count == 1)
            {
                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT COUNT(?" + SelectVariables[0] + ") WHERE {");
                queryBuilder.Append(RootGraphPattern.GetSparqlRepresentation());
                queryBuilder.Append("}");
                return queryBuilder.ToString();
            }
            throw new NotImplementedException("Not implemented support for doing a count on a multi-variable query yet");
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
                        string countQuery = GetSparqlCountQuery();
                        var countResults = endpoint.QueryWithResultSet(countQuery);
                        var firstResult = countResults.Results.FirstOrDefault();
                        int resultsCount = 0;
                        if (firstResult != null)
                        {
                            resultsCount = Int32.Parse((firstResult[0] as ILiteralNode).Value);
                        }
                        var selectVarInfo = VariableType[SelectVariables[0]];
                        if (selectVarInfo.IsCollection)
                        {
                            handler.CreateFeedFromGraph(resultsGraph, selectVarInfo.EntityType, resultsCount, this);
                        }
                        else
                        {
                            handler.CreateEntryFromGraphWithVariable(resultsGraph, SelectVariables[0], selectVarInfo.EntityType);
                        }
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
            VariableType[entityResource] = new SparqlVariableInfo {EntityType = entityType, IsCollection = false};
        }

        public string GetEntityType(string variableOrResource)
        {
            return VariableType[variableOrResource].EntityType;
        }

        public class SparqlVariableInfo
        {
            public string EntityType { get; set; }
            public bool IsCollection { get; set; } 
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