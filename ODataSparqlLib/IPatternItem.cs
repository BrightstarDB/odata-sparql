namespace ODataSparqlLib
{
    public interface IPatternItem
    {
        PatternItemKind PatternItemKind { get; }
        string SparqlRepresentation { get; }
    }
}