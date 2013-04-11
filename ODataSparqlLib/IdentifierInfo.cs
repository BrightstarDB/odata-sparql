using Microsoft.Data.Edm;

namespace ODataSparqlLib
{
    public class IdentifierInfo
    {
        public string Name { get; set; }
        public IEdmTypeReference PropertyType { get; set; }
        public string IdentifierPrefix { get; set; }
    }
}