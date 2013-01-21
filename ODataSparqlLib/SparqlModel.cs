using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF.Query;

namespace ODataSparqlLib
{
    public class SparqlModel
    {
        public GraphPattern RootGraphPattern { get; private set; }
        public List<string> SelectVariables { get; private set; }
        public Dictionary<string, string> VariableType { get; private set; } 
        public bool IsDescribe { get; set; }

        public string DescribeResource { get; private set; }

        public int? Limit { get; set; }

        private int _variableCounter = 1;

        public SparqlModel()
        {
            RootGraphPattern = new GraphPattern();
            SelectVariables = new List<string>();
            VariableType = new Dictionary<string, string>();
        }

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
                return String.Format("DESCRIBE <{0}>", DescribeResource);
            }
            StringBuilder queryBuilder= new StringBuilder();
            if (SelectVariables.Count > 0)
            {
                queryBuilder.Append(IsDescribe ? "DESCRIBE " : "SELECT ");
                foreach (var sv in SelectVariables)
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
                var query = this.GetSparqlRepresentation();
                var resultsGraph = endpoint.QueryWithResultGraph(query);
                if (!String.IsNullOrEmpty(DescribeResource))
                {
                    // Create ODATA entry payload for single resource
                    handler.CreateEntryFromGraph(resultsGraph, DescribeResource, GetEntityType(DescribeResource));
                }
                else
                {
                    handler.CreateFeedFromGraph(resultsGraph, VariableType.Values);
                }
            }
            else
            {
                var resultSet = endpoint.QueryWithResultSet(this.GetSparqlRepresentation());
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
}
