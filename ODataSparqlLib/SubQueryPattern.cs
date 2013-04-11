namespace ODataSparqlLib
{
    public class SubQueryPattern : BaseTriplePattern
    {
        public SubQueryPattern()
        {
            Query = new SparqlModel();
        }

        public SparqlModel Query { get; set; }

        public override string GetSparqlRepresentation()
        {
            return "{" + Query.GetSparqlRepresentation() + "}";
        }
    }
}
