using System.Collections.Generic;
using System.Text;

namespace ODataSparqlLib
{
    public class GraphPattern
    {
        public List<TriplePattern> TriplePatterns { get; private set; }
        public List<GraphPattern> ChildGraphPatterns { get; private set; } 

        public GraphPattern()
        {
            TriplePatterns = new List<TriplePattern>();
            ChildGraphPatterns = new List<GraphPattern>();
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
            return builder.ToString();
        }
    }
}