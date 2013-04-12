using System;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using VDS.RDF.Query;

namespace ODataSparqlLib
{
    internal class ODataSparqlServiceSettings
    {
        public string Name { get; private set; }
        private readonly HttpServerUtility _server;
        public SparqlMap Map { get; private set; }

        /// <summary>
        /// Get the path to the file that provides the EDM for the endpoint
        /// </summary>
        public string MetadataPath { get; private set; }

        /// <summary>
        /// Get the data model for the OData endpoint
        /// </summary>
        public IEdmModel Model { get { return Map.Model; } }

        /// <summary>
        /// Get the SPARQL endpoint to be queried
        /// </summary>
        public SparqlRemoteEndpoint SparqlEndpoint { get; private set; }

        /// <summary>
        /// Get the options for the OData writer
        /// </summary>
        public ODataMessageWriterSettings BaseODataWriterSettings { get; private set; }

        /// <summary>
        /// Get the maximum number of entries to retrieve from the SPARQL endpoint per request.
        /// </summary>
        public int MaxPageSize { get; private set; }

        /// <summary>
        /// Get the language code to apply when filtering string values
        /// </summary>
        public string DefaultLanguageCode { get; private set; }

        public ODataSparqlServiceSettings(string name, HttpServerUtility server)
        {
            Name = name;
            _server = server;
            var serviceConfiguration =
                System.Configuration.ConfigurationManager.GetSection("odataSparql") as
                ODataSparqlServiceConfigurationSection;
            ConfigureWriterSettings(serviceConfiguration);
            var endpoint = FindEndpointConfiguration(name, serviceConfiguration);
            ReadEndpointMetadata(endpoint);
            MaxPageSize = endpoint.MaxPageSize;
            DefaultLanguageCode = endpoint.DefaultLanguage;
            SparqlEndpoint = new SparqlRemoteEndpoint(new Uri(endpoint.Address), endpoint.DefaultGraphUri)
            {
                Timeout = 60000,
                RdfAcceptHeader = "application/rdf+xml"
            };
        }

        private void ConfigureWriterSettings(ODataSparqlServiceConfigurationSection serviceConfiguration)
        {
            BaseODataWriterSettings = new ODataMessageWriterSettings
                {
                    Indent = serviceConfiguration.OutputConfiguration.Indent,
                    Version = ODataVersion.V3
                };
        }

        private void ReadEndpointMetadata(SparqlEndpointConfigurationElement endpoint)
        {
            MetadataPath = _server.MapPath(endpoint.Metadata);
            if (!File.Exists(MetadataPath))
            {
                throw new FileNotFoundException("Cannot find service metadata file");
            }
            Map = new SparqlMap(MetadataPath, endpoint.DefaultNamespace,
                endpoint.NameMapping,
                endpoint.DefaultPropertyNamespace,
                endpoint.PropertyNameMapping);
        }

        private static SparqlEndpointConfigurationElement FindEndpointConfiguration(string name,
                                                                                    ODataSparqlServiceConfigurationSection
                                                                                        serviceConfiguration)
        {
            var endpoint =
                serviceConfiguration.Endpoints.OfType<SparqlEndpointConfigurationElement>()
                                    .FirstOrDefault(ep => ep.Name.Equals(name));
            if (endpoint == null)
            {
                throw new Exception("No configuration found for SPARQL service named " + name);
            }
            return endpoint;
        }
    }
}