using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODataSchemaTool.SchemaModel
{
    class Schema
    {
        private readonly List<SchemaType> _types;
 
        public IEnumerable<SchemaType> Types { get { return _types; } }

        public Schema()
        {
            _types = new List<SchemaType>();
        }

        public void AddType(SchemaType t)
        {
            _types.Add(t);
        }
    }
}
