using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ODataSparqlLib
{
    public class GraphPattern
    {
        public List<TriplePattern> TriplePatterns { get; private set; }
        public List<GraphPattern> ChildGraphPatterns { get; private set; }
        public List<string> FilterExpressions { get; private set; } 

        public GraphPattern()
        {
            TriplePatterns = new List<TriplePattern>();
            ChildGraphPatterns = new List<GraphPattern>();
            FilterExpressions = new List<string>();
        }

        public void Add(TriplePattern triplePattern)
        {
            TriplePatterns.Add(triplePattern);
        }

        public string GetSparqlRepresentation()
        {
            var builder = new StringBuilder();
            foreach (var triplePattern in TriplePatterns)
            {
                builder.Append(triplePattern.GetSparqlRepresentation());
                builder.Append(". ");
            }
            if (FilterExpressions.Count > 0)
            {
                builder.Append("FILTER ( ");
                if (FilterExpressions.Count == 1)
                {
                    builder.Append(FilterExpressions[0]);
                }
                else
                {
                    builder.Append("(");
                    builder.Append(FilterExpressions[0]);
                    builder.Append(")");
                    foreach (var expr in FilterExpressions.Skip(1))
                    {
                        builder.Append(" && (");
                        builder.Append(expr);
                        builder.Append(")");
                    }
                }
                builder.Append(")");
            }
            return builder.ToString();
        }

        public void AddFilterExpression(string filterExpression)
        {
            FilterExpressions.Add(filterExpression);
        }
    }
}