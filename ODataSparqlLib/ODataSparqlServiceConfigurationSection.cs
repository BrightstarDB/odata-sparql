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
}
