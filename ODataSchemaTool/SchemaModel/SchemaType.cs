using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODataSchemaTool.SchemaModel
{
    class SchemaType
    {
        public string Name { get; set; }
        public Uri TypeUri { get; set; }
        public string ResourcePrefix { get; set; }
        public SchemaProperty IdentifierProperty { get; set; }
        public SchemaType DerivedFrom { get; set; }
    }
}
