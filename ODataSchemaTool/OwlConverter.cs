using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODataSchemaTool.SchemaModel;
using VDS.RDF;
using VDS.RDF.Ontology;
using VDS.RDF.Parsing;

namespace ODataSchemaTool
{
    internal class OwlConverter
    {
        private readonly Args _args;
        private readonly List<string> _warnings;
        private readonly List<string> _errors;

        private OntologyGraph _ontology;
        private Schema _schema;

        public OwlConverter(Args converterArgs)
        {
            _args = converterArgs;
            _warnings = new List<string>();
            _errors = new List<string>();
        }

        public bool HasErrors
        {
            get { return _errors.Any(); }
        }

        public bool HasWarnings
        {
            get { return _warnings.Any(); }
        }

        public IEnumerable<string> WarningMessages
        {
            get { return _warnings; }
        }

        public IEnumerable<string> ErrorMessages
        {
            get { return _errors; }
        }

        public void Execute()
        {
            if (TryLoadOntologyGraph())
            {
                _schema = new Schema();
                ProcessClasses();
            }
        }

        private bool TryLoadOntologyGraph()
        {
            bool parsedSuccessfully = true;
            _ontology = new OntologyGraph();
            foreach (var f in _args.Inputs)
            {
                try
                {
                    FileLoader.Load(_ontology, f);
                }
                catch (Exception ex)
                {
                    LogError("Error loading ontology from file '{0}'. Cause: {1}", f, ex.Message);
                    parsedSuccessfully = false;
                }
            }
            return parsedSuccessfully;
        }

        private void ProcessClasses()
        {
            foreach (var topClass in _ontology.AllClasses.Where(c => c.IsTopClass))
            {
                ProcessClass(topClass, null);
            }
        }

        private void ProcessClass(OntologyClass ontologyClass, SchemaType parentType)
        {
            if (IncludeInOutput(ontologyClass))
            {
                var schemaType = new SchemaType();
                var resource = ontologyClass.Resource as IUriNode;
                schemaType.Name = MakeTypeName(resource);
                schemaType.TypeUri = resource.Uri;
                if (parentType == null)
                {
                    // Needs to define an Id property
                    schemaType.IdentifierProperty = new SchemaProperty
                        {
                            Name = _args.IdentifierPropertyName,
                            DeclaredType = typeof (string),
                        };
                }
                else
                {
                    schemaType.DerivedFrom = parentType;
                }
                foreach (var derivedClass in ontologyClass.DirectSubClasses)
                {
                    ProcessClass(derivedClass, schemaType);
                }
            }
        }

        private string MakeTypeName(IUriNode resource)
        {
            var baseName = String.IsNullOrEmpty(resource.Uri.Fragment)
                               ? resource.Uri.Segments.Last()
                               : resource.Uri.Fragment;
            if (!_schema.Types.Any(t => t.Name.Equals(baseName)))
            {
                return baseName;
            }
            var suffix = 1;
            string name;
            do
            {
                name = baseName + suffix;
                suffix++;
            } while (_schema.Types.Any(t => t.Name.Equals(baseName)));
            return name;
        }

        /// <summary>
        /// Determines if this ontology class is to be included in the generated schema
        /// </summary>
        /// <param name="ontologyClass"></param>
        /// <returns></returns>
        private bool IncludeInOutput(OntologyClass ontologyClass)
        {
            return ontologyClass.Resource is IUriNode;
        }

        #region Logging errors and warnings
        private void LogError(string fmt, params object[] args)
        {
            _errors.Add(string.Format(fmt, args));
        }

        private void LogWarning(string fmt, params object[] args)
        {
            _warnings.Add(String.Format(fmt, args));
        }
        #endregion
    }
}
