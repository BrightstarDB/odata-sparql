using System;

namespace ODataSparqlLib
{
    public class TriplePattern
    {
        public IPatternItem Subject { get; set; }
        public IPatternItem Predicate { get; set; }
        public IPatternItem Object { get; set; }

        public TriplePattern(IPatternItem subject, IPatternItem predicate, IPatternItem @object)
        {
            Subject = subject;
            Predicate = predicate;
            Object = @object;
        }

        public string GetSparqlRepresentation()
        {
            return String.Format("{0} {1} {2}", Subject.SparqlRepresentation, Predicate.SparqlRepresentation,
                                 Object.SparqlRepresentation);
        }
    }
}