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
        public IEdmModel Model { get { return Map.Model; } }
        public SparqlRemoteEndpoint SparqlEndpoint { get; private set; }
        public ODataMessageWriterSettings BaseODataWriterSettings { get; private set; }

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
            var configPath = _server.MapPath(endpoint.Metadata);
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Cannot find service metadata file");
            }
            Map = new SparqlMap(configPath, endpoint.DefaultNamespace,
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