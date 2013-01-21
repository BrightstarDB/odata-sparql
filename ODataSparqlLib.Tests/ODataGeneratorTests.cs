using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Validation;
using Microsoft.Data.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VDS.RDF;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    [DeploymentItem("dbpedia.metadata")]
    public class ODataGeneratorTests
    {
        private readonly IEdmModel _dbpediaModel;
        private readonly SparqlMap _dbpediaMap;

        public ODataGeneratorTests()
        {
            using (var edmxStream = File.OpenRead("dbpedia.metadata"))
            {
                IEnumerable<EdmError> errors;
                Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out _dbpediaModel,
                                                            out errors);
            }
            _dbpediaMap = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/");
        }

        [TestMethod]
        public void TestCreateEntryFeedForSingleItem()
        {
            var testGraph = new Graph {BaseUri = new Uri("http://dbpedia.org/resource/")};
            var film = testGraph.CreateUriNode(UriFactory.Create("http://dbpedia.org/resource/Un_Chien_Andalou"));
            testGraph.Assert(film,
                             testGraph.CreateUriNode(UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
                             testGraph.CreateUriNode(UriFactory.Create("http://dbpedia.org/ontology/Film")));
            testGraph.Assert(film,
                             testGraph.CreateUriNode(UriFactory.Create("http://xmlns.com/foaf/0.1/name")),
                             testGraph.CreateLiteralNode("Un Chien Andalou"));
            var mock = new Mock<IODataResponseMessage>();
            var mockStream = new MemoryStream();
            //mock.Setup(m => m.Url).Returns(new Uri("http://example.org/odata/Films('Un_Chien_Andalou')"));
            //mock.Setup(m => m.Method).Returns("GET");
            mock.Setup(m => m.GetStream()).Returns(mockStream);
            var generator = new ODataFeedGenerator(mock.Object, _dbpediaMap, "http://example.org/odata/");
            generator.CreateEntryFromGraph(testGraph, film.Uri.ToString(), "DBPedia.Film");
            mockStream.Seek(0, SeekOrigin.Begin);
            var streamXml = XDocument.Load(mockStream);
            Assert.IsNotNull(streamXml);
            Assert.IsNotNull(streamXml.Root);
            Assert.AreEqual(XName.Get("entry", "http://www.w3.org/2005/Atom"), streamXml.Root.Name);
            Console.WriteLine(streamXml.ToString());
            XNamespace atom = "http://www.w3.org/2005/Atom";
            Assert.AreEqual("http://example.org/odata/Films('Un_Chien_Andalou')",
                (string)streamXml.Root.Element(atom+"id"));
        }
    }
}
