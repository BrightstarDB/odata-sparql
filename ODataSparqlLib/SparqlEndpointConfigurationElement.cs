using System.Configuration;

namespace ODataSparqlLib
{
    public class SparqlEndpointConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string) this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("metadata", IsRequired = true)]
        public string Metadata
        {
            get { return (string) this["metadata"]; }
            set { this["metadata"] = value; }
        }

        [ConfigurationProperty("address", IsRequired = true)]
        public string Address
        {
            get { return (string) this["address"]; }
            set { this["address"] = value; }
        }

        [ConfigurationProperty("defaultNamespace")]
        public string DefaultNamespace
        {
            get { return (string) this["defaultNamespace"]; }
            set { this["defaultNamespace"] = value; }
        }

        [ConfigurationProperty("nameMapping", DefaultValue = NameMapping.Unchanged)]
        public NameMapping NameMapping
        {
            get { return (NameMapping) this["nameMapping"]; }
            set { this["nameMapping"] = value; }
        }

        [ConfigurationProperty("defaultPropertyNamespace")]
        public string DefaultPropertyNamespace
        {
            get { return (string) this["defaultPropertyNamespace"]; }
            set { this["defaultPropertyNamespace"] = value; }
        }

        [ConfigurationProperty("propertyNameMapping", DefaultValue = NameMapping.Unchanged)]
        public NameMapping? PropertyNameMapping
        {
            get { return (NameMapping?) this["propertyNameMapping"]; }
            set { this["propertyNameMapping"] = value; }
        }

        [ConfigurationProperty("defaultLanguage")]
        public string DefaultLanguage
        {
            get { return (string) this["defaultLanguage"]; }
            set { this["defaultLanguage"] = value; }
        }

        [ConfigurationProperty("defaultGraphUri")]
        public string DefaultGraphUri
        {
            get { return (string) this["defaultGraphUri"]; }
            set { this["defaultGraphUri"] = value; }
        }

        [ConfigurationProperty("maxPageSize", DefaultValue = SparqlGenerator.DefaultMaxPageSize)]
        public int MaxPageSize
        {
            get { return (int) this["maxPageSize"]; }
            set { this["maxPageSize"] = value; }
        }
    }
}