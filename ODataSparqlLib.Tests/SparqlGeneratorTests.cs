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
            _dbpediaMap = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/", NameMapping.Unchanged);
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

        [TestMethod]
        public void TestTop()
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Films?$top=2"),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            var sparql = generator.SparqlQueryModel;
            Assert.IsNotNull(sparql);
            Assert.IsTrue(sparql.IsDescribe);
            Assert.AreEqual(2, sparql.Limit);
        }

        [TestMethod]
        public void TestOrderByProperty()
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Places?$orderby=PopulationTotal&$top=20"),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            var sparql = generator.SparqlQueryModel;
            Assert.IsNotNull(sparql);
            Assert.IsTrue(sparql.IsDescribe);
            Assert.AreEqual(20, sparql.Limit);
            Assert.IsNotNull(sparql.Ordering);
            Assert.IsTrue(sparql.Ordering.IsSimple);
            Assert.AreEqual("v2", sparql.Ordering.Variable);
            Assert.IsNull(sparql.Ordering.ThenBy);
        }

        [TestMethod]
        public void TestGtOperator()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal gt 1000000");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
        }

        private SparqlModel ProcessQuery(string odataQuery)
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri(odataQuery),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            return generator.SparqlQueryModel;
        }
    }
}
