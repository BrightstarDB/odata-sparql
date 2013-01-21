using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Validation;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VDS.RDF.Query;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    [DeploymentItem("dbpedia.metadata")]
    public class EndToEndTests
    {
        private readonly IEdmModel _dbpediaModel;
        private readonly SparqlMap _dbpediaMap;
        private SparqlRemoteEndpoint _sparqlEndpoint;
        private string _odataBase;

        public EndToEndTests()
        {
            using (var edmxStream = File.OpenRead("dbpedia.metadata"))
            {
                IEnumerable<EdmError> errors;
                Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out _dbpediaModel,
                                                            out errors);
            }
            _dbpediaMap = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/");
            _sparqlEndpoint = new SparqlRemoteEndpoint(new Uri("http://dbpedia.org/sparql"),
                                                       "http://dbpedia.org")
                {
                    Timeout = 60000,
                    RdfAcceptHeader = "application/rdf+xml"
                };
            _odataBase = "http://example.org/odata/";
        }

        [TestMethod]
        public void TestRetrieveSingleFilm()
        {
            const string odataQuery = "http://example.org/odata/Films('Un_Chien_Andalou')";
            var parsedQuery = QueryDescriptorQueryNode.ParseUri(
                new Uri(odataQuery), new Uri(_odataBase), _dbpediaModel);
            var sparqlGenerator = new SparqlGenerator(_dbpediaMap);
            sparqlGenerator.ProcessQuery(parsedQuery);
            var mockMessage = new Mock<IODataResponseMessage>();
            var outputStream = new MemoryStream();
            mockMessage.Setup(m => m.GetStream()).Returns(outputStream);
                var feedGenerator = new ODataFeedGenerator(mockMessage.Object, _dbpediaMap, _odataBase);
            sparqlGenerator.SparqlQueryModel.Execute(_sparqlEndpoint, feedGenerator);
            outputStream.Seek(0, SeekOrigin.Begin);
            var validator= new XPathValidator(outputStream);
            validator.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            validator.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            validator.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            validator.AssertRoot("atom:entry");
            validator.AssertXPathValue("/atom:entry/atom:id", "http://example.org/odata/Films('Un_Chien_Andalou')");
            validator.AssertXPathValue("/atom:entry/atom:title", "");
            validator.AssertXPathValue("/atom:entry/atom:content/m:properties/d:Name", "Un Chien Andalou");
            validator.AssertXPathValue("/atom:entry/atom:content/m:properties/d:Runtime", "960.0");
        }

    }

    class XPathValidator
    {
        private readonly XmlDocument _doc;
        private readonly XmlNamespaceManager _nsMgr;
        private readonly XPathNavigator _nav;

        public XPathValidator(Stream xmlStream)
        {
            _doc = new XmlDocument();
            _doc.Load(xmlStream);
            _nsMgr = new XmlNamespaceManager(_doc.NameTable);
            _nav = _doc.CreateNavigator();
        }

        public void AddNamespace(string prefix, string uri)
        {
            _nsMgr.AddNamespace(prefix, uri);
        }

        public void AssertRoot(string qname)
        {
            Assert.IsNotNull(_doc.SelectSingleNode("/" + qname, _nsMgr),
                "Cannot find root node with name {0}", qname);
        }

        public void AssertXPathValue(string xpath, string expectedValue)
        {
            var result = _nav.Select(xpath, _nsMgr);
            while (result.MoveNext())
            {
                if (expectedValue.Equals(result.Current.Value))
                {
                    return;
                }
            }
            Assert.Fail("Evaluation of path '{0}' did not return a node with a value that matched the expected value '{1}'", xpath, expectedValue);
        }
    }
}
