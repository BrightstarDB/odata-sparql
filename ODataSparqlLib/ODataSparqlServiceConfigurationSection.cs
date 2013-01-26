using System.Configuration;

namespace ODataSparqlLib
{
    public class ODataSparqlServiceConfigurationSection : ConfigurationSection
    {
        public ODataSparqlServiceConfigurationSection()
        {
            Endpoints = new SparqlEndpointConfigurationCollection();
        }

        [ConfigurationProperty("endpoints", IsRequired = true)]
        public SparqlEndpointConfigurationCollection Endpoints
        {
            get { return this["endpoints"] as SparqlEndpointConfigurationCollection; }
            set { this["endpoints"] = value; }
        }

        [ConfigurationProperty("output", IsRequired = true)]
        public OutputConfigurationElement OutputConfiguration
        {
            get { return this["output"] as OutputConfigurationElement; }
            set { this["output"] = value; }
        }
    }

    public class OutputConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Controls whether the output of the service is pretty-printed or not.
        /// </summary>
        [ConfigurationProperty("indent", DefaultValue = false)]
        public bool Indent
        {
            get { return (bool) this["indent"]; }
            set { this["indent"] = value; }
        }


    }
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

        [ConfigurationProperty("defaultMapping")]
        public string DefaultMapping
        {
            get { return (string) this["defaultMapping"]; }
            set { this["defaultMapping"] = value; }
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
    }

    public class SparqlEndpointConfigurationCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SparqlEndpointConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var config = element as SparqlEndpointConfigurationElement;
            return config.Name;
        }
    }
}
