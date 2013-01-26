using System.Configuration;

namespace ODataSparqlLib
{
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