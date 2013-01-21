namespace ODataSparqlLib
{
    public class VariablePatternItem : IPatternItem
    {
        public string VariableName { get; set; }
        public PatternItemKind PatternItemKind { get {return PatternItemKind.Variable;} }
        public string SparqlRepresentation { get { return "?" + VariableName; } }
        public VariablePatternItem(string variableName)
        {
            VariableName = variableName;
        }
    }
}