using Microsoft.Data.Edm;

namespace ODataSparqlLib
{
    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public IEdmTypeReference PropertyType { get; set; }
        public bool IsInverse { get; set; }
    }
}