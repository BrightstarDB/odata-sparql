using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Validation;
using Microsoft.Data.OData.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    [DeploymentItem("dbpedia.metadata")]
    public class SparqlGeneratorTests
    {
        private readonly IEdmModel _dbpediaModel;
        private SparqlMap _dbpediaMap;
        public SparqlGeneratorTests()
        {
            using (var edmxStream = System.IO.File.OpenRead("dbpedia.metadata"))
            {
                IEnumerable<EdmError> errors;
                Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out _dbpediaModel,
                                                            out errors);
            }
            _dbpediaMap = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/");
        }

        [TestMethod]
        public void TestEntitySetAccess()
        {
            var dbpediaGenerator= new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                            new Uri("http://example.org/odata/Films"),
                            new Uri("http://example.org/odata/"), _dbpediaModel); 
            dbpediaGenerator.ProcessQuery(queryDescriptor);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel.RootGraphPattern);
            Assert.AreEqual(1, dbpediaGenerator.SparqlQueryModel.RootGraphPattern.TriplePatterns.Count);
            var tp = dbpediaGenerator.SparqlQueryModel.RootGraphPattern.TriplePatterns[0];
            Assert.AreEqual("?v1 <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://dbpedia.org/ontology/Film>",
                tp.GetSparqlRepresentation());
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel.SelectVariables);
            Assert.AreEqual(1, dbpediaGenerator.SparqlQueryModel.SelectVariables.Count);
            Assert.IsTrue(dbpediaGenerator.SparqlQueryModel.SelectVariables.Contains("v1"));
            Assert.IsTrue(dbpediaGenerator.SparqlQueryModel.IsDescribe);
            Assert.AreEqual("DBPedia.Film", dbpediaGenerator.SparqlQueryModel.GetEntityType("v1"));
        }

        [TestMethod]
        public void TestInstanceAccess()
        {
            var dbpediaGenerator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                            new Uri("http://example.org/odata/Films('Un_Chien_Andalou')"),
                            new Uri("http://example.org/odata/"), _dbpediaModel);
            dbpediaGenerator.ProcessQuery(queryDescriptor);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel.DescribeResource);
            Assert.AreEqual("http://dbpedia.org/resource/Un_Chien_Andalou", dbpediaGenerator.SparqlQueryModel.DescribeResource);
            Assert.AreEqual("DBPedia.Film", dbpediaGenerator.SparqlQueryModel.GetEntityType("http://dbpedia.org/resource/Un_Chien_Andalou"));
        }
    }
}
