namespace ODataSparqlLib
{
    public class UriPatternItem : IPatternItem
    {
        public string Uri { get; set; }
        public PatternItemKind PatternItemKind{get{return PatternItemKind.Uri;}}
        public string SparqlRepresentation { get { return "<" + Uri + ">"; } }
        public UriPatternItem(string uri)
        {
            Uri = uri;
        }
    }
}